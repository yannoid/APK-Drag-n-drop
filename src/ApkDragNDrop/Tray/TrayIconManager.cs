using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace ApkDragNDrop.Tray;

public class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Window _mainWindow;

    public TrayIconManager(Window mainWindow, Action openSettings, Action requestExit)
    {
        _mainWindow = mainWindow;

        var icon = LoadTrayIcon();

        _notifyIcon = new NotifyIcon
        {
            Icon = icon,
            Visible = true,
            Text = "APK Drag'n'drop",
        };

        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Afficher", null, (_, _) => ShowMainWindow());
        menu.Items.Add("Paramètres", null, (_, _) => openSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quitter", null, (_, _) => requestExit());
        _notifyIcon.ContextMenuStrip = menu;
    }

    private static System.Drawing.Icon LoadTrayIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Assets/app.ico");
            var resourceStream = System.Windows.Application.GetResourceStream(uri)
                ?? throw new FileNotFoundException("Assets/app.ico");
            using var stream = resourceStream.Stream;
            return new System.Drawing.Icon(stream);
        }
        catch
        {
            return System.Drawing.SystemIcons.Application;
        }
    }

    public void ShowMainWindow()
    {
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    public void ShowBalloon(string title, string text) =>
        _notifyIcon.ShowBalloonTip(3000, title, text, ToolTipIcon.Info);

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
