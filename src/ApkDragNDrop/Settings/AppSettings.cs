namespace ApkDragNDrop.Settings;

public enum CloseBehavior
{
    MinimizeToTray = 0,
    ExitApplication = 1,
}

public class AppSettings
{
    public bool LaunchAtWindowsStartup { get; set; }
    public CloseBehavior OnWindowClose { get; set; } = CloseBehavior.MinimizeToTray;
}
