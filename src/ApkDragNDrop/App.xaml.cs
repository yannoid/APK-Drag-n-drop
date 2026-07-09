using System.Windows;
using ApkDragNDrop.Settings;
using Application = System.Windows.Application;

namespace ApkDragNDrop;

public partial class App : Application
{
    public AppSettings CurrentSettings { get; private set; } = new();
    private readonly SettingsService _settingsService = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        CurrentSettings = _settingsService.Load();

        var mainWindow = new MainWindow();
        bool startMinimized = e.Args.Contains("--minimized");

        if (startMinimized)
            mainWindow.Hide();
        else
            mainWindow.Show();
    }

    public void SaveSettings() => _settingsService.Save(CurrentSettings);
}
