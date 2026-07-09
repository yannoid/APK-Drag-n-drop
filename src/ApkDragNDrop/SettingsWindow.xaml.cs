using System.Windows;
using System.Windows.Input;
using ApkDragNDrop.Settings;
using Application = System.Windows.Application;

namespace ApkDragNDrop;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private bool _isLoading = true;

    public SettingsWindow()
    {
        InitializeComponent();

        var settings = ((App)Application.Current).CurrentSettings;
        ChkLaunchAtStartup.IsChecked = StartupRegistryHelper.IsEnabled();
        RadioTray.IsChecked = settings.OnWindowClose == CloseBehavior.MinimizeToTray;
        RadioExit.IsChecked = settings.OnWindowClose == CloseBehavior.ExitApplication;

        _isLoading = false;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void OnSettingChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
            return;

        var app = (App)Application.Current;

        var wantStartup = ChkLaunchAtStartup.IsChecked == true;
        if (wantStartup) StartupRegistryHelper.Enable();
        else StartupRegistryHelper.Disable();

        app.CurrentSettings.LaunchAtWindowsStartup = wantStartup;
        app.CurrentSettings.OnWindowClose = RadioExit.IsChecked == true
            ? CloseBehavior.ExitApplication
            : CloseBehavior.MinimizeToTray;

        app.SaveSettings();
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
