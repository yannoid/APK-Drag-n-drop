using System.IO;

namespace ApkDragNDrop.Services;

public static class AdbPathResolver
{
    private static string? _cached;

    public static string ResolveAdbPath()
    {
        if (_cached is not null)
            return _cached;

        var bundled = Path.Combine(AppContext.BaseDirectory, "adb", "adb.exe");
        if (File.Exists(bundled))
            return _cached = bundled;

        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(dir.Trim(), "adb.exe");
            if (File.Exists(candidate))
                return _cached = candidate;
        }

        throw new AdbNotFoundException();
    }

    public static void Invalidate() => _cached = null;
}
