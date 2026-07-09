using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ApkDragNDrop.Models;
using ApkDragNDrop.Services;
using ApkDragNDrop.Settings;
using ApkDragNDrop.Tray;
using Microsoft.Win32;
using Application = System.Windows.Application;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace ApkDragNDrop;

public partial class MainWindow : Window
{
    private readonly AdbService _adbService = new();
    private readonly InstallQueueManager _queueManager;
    private readonly ObservableCollection<DeviceInfo> _devices = new();
    private readonly ObservableCollection<ApkQueueItem> _apkQueue = new();

    private TrayIconManager? _trayIconManager;
    private SettingsWindow? _settingsWindow;
    private bool _isExitRequested;

    public MainWindow()
    {
        InitializeComponent();

        _queueManager = new InstallQueueManager(_adbService);

        DevicesItemsControl.ItemsSource = _devices;
        ApkQueueItemsControl.ItemsSource = _apkQueue;
        JobsItemsControl.ItemsSource = _queueManager.Jobs;

        _apkQueue.CollectionChanged += (_, _) => UpdateSendButtonState();
        _queueManager.Jobs.CollectionChanged += (_, _) => JobsPanel.Visibility =
            _queueManager.Jobs.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        Loaded += async (_, _) =>
        {
            _trayIconManager = new TrayIconManager(this, OpenSettings, ForceExit);
            await RefreshDevicesAsync();
        };
    }

    // ----- Title bar -----
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    // ----- Close / tray behavior -----
    protected override void OnClosing(CancelEventArgs e)
    {
        var app = (App)Application.Current;

        if (!_isExitRequested && app.CurrentSettings.OnWindowClose == CloseBehavior.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
            _trayIconManager?.ShowBalloon("APK Drag'n'drop", "L'application continue de s'exécuter dans la barre des tâches.");
            return;
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _trayIconManager?.Dispose();
        base.OnClosed(e);
        Application.Current.Shutdown();
    }

    private void ForceExit()
    {
        _isExitRequested = true;
        Close();
    }

    private void OpenSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow { Owner = this };
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    // ----- Devices -----
    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await RefreshDevicesAsync();

    private async Task RefreshDevicesAsync()
    {
        RefreshButton.IsEnabled = false;
        var spin = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(700));
        RefreshRotation.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, spin);

        try
        {
            var fetched = await _adbService.GetDevicesAsync(CancellationToken.None);

            var fetchedSerials = fetched.Select(d => d.Serial).ToHashSet();
            for (int i = _devices.Count - 1; i >= 0; i--)
            {
                if (!fetchedSerials.Contains(_devices[i].Serial))
                    _devices.RemoveAt(i);
            }

            foreach (var device in fetched)
            {
                var existing = _devices.FirstOrDefault(d => d.Serial == device.Serial);
                if (existing is not null)
                {
                    existing.State = device.State;
                    existing.Model = device.Model;
                    existing.Product = device.Product;
                }
                else
                {
                    device.PropertyChanged += (_, args) =>
                    {
                        if (args.PropertyName == nameof(DeviceInfo.IsSelected))
                        {
                            UpdateDeviceSummary();
                            UpdateSendButtonState();
                        }
                    };
                    _devices.Add(device);
                }
            }

            UpdateDeviceSummary();
            UpdateSendButtonState();
        }
        catch (AdbNotFoundException)
        {
            DeviceSummaryText.Text = "adb.exe introuvable";
        }
        catch (Exception ex)
        {
            DeviceSummaryText.Text = $"Erreur : {ex.Message}";
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void UpdateDeviceSummary()
    {
        var selectedCount = _devices.Count(d => d.IsSelected);
        DeviceSummaryText.Text = selectedCount switch
        {
            0 => "Aucun appareil",
            1 => "1 appareil sélectionné",
            _ => $"{selectedCount} appareils sélectionnés",
        };
    }

    // ----- Drop zone -----
    private void DropZone_DragEnter(object sender, DragEventArgs e) => UpdateDragEffect(e);

    private void DropZone_DragOver(object sender, DragEventArgs e) => UpdateDragEffect(e);

    private void DropZone_DragLeave(object sender, DragEventArgs e)
    {
    }

    private static void UpdateDragEffect(DragEventArgs e)
    {
        var hasApk = e.Data.GetDataPresent(DataFormats.FileDrop) &&
                     ((string[])e.Data.GetData(DataFormats.FileDrop)!).Any(
                         f => f.EndsWith(".apk", StringComparison.OrdinalIgnoreCase));
        e.Effects = hasApk ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        AddApkFiles(files);
    }

    private void DropZone_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Fichiers APK (*.apk)|*.apk",
            Multiselect = true,
        };

        if (dialog.ShowDialog(this) == true)
            AddApkFiles(dialog.FileNames);
    }

    private void AddApkFiles(IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            if (!file.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                continue;
            if (_apkQueue.Any(a => string.Equals(a.Path, file, StringComparison.OrdinalIgnoreCase)))
                continue;

            _apkQueue.Add(new ApkQueueItem { Path = file });
        }
    }

    private void RemoveApk_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ApkQueueItem item })
            _apkQueue.Remove(item);
    }

    // ----- Send -----
    private void UpdateSendButtonState()
    {
        var deviceCount = _devices.Count(d => d.IsSelected);
        var apkCount = _apkQueue.Count;

        SendButton.IsEnabled = deviceCount > 0 && apkCount > 0;
        SendButtonText.Text = deviceCount > 0 && apkCount > 0
            ? $"Envoyer ({deviceCount} appareil{(deviceCount > 1 ? "s" : "")} · {apkCount} APK)"
            : "Envoyer";
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedDevices = _devices.Where(d => d.IsSelected).ToList();
        var apkPaths = _apkQueue.Select(a => a.Path).ToList();

        if (selectedDevices.Count == 0 || apkPaths.Count == 0)
            return;

        _queueManager.Enqueue(apkPaths, selectedDevices);
        _apkQueue.Clear();
    }
}
