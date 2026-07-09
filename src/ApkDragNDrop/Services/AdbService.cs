using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using ApkDragNDrop.Models;

namespace ApkDragNDrop.Services;

public record AdbCommandResult(int ExitCode, string StdOut, string StdErr);

public class AdbService
{
    private static readonly TimeSpan InstallTimeout = TimeSpan.FromSeconds(120);
    private static readonly Regex DeviceLineRegex =
        new(@"^(?<serial>\S+)\s+(?<state>device|offline|unauthorized|no permissions)\b(?<rest>.*)$",
            RegexOptions.Compiled);
    private static readonly Regex KeyValueRegex = new(@"(?<key>\w+):(?<value>\S+)", RegexOptions.Compiled);
    private static readonly Regex FailureRegex =
        new(@"Failure\s*\[(?<code>[A-Z0-9_]+)(?::\s*(?<detail>[^\]]*))?\]", RegexOptions.Compiled);

    public async Task<IReadOnlyList<DeviceInfo>> GetDevicesAsync(CancellationToken ct)
    {
        var result = await RunProcessAsync(new[] { "devices", "-l" }, TimeSpan.FromSeconds(15), ct);
        return ParseDevicesOutput(result.StdOut);
    }

    public static IReadOnlyList<DeviceInfo> ParseDevicesOutput(string stdout)
    {
        var devices = new List<DeviceInfo>();
        var lines = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase))
                continue;

            var match = DeviceLineRegex.Match(line);
            if (!match.Success)
                continue;

            var serial = match.Groups["serial"].Value;
            var state = match.Groups["state"].Value switch
            {
                "device" => DeviceState.Device,
                "offline" => DeviceState.Offline,
                "unauthorized" => DeviceState.Unauthorized,
                "no permissions" => DeviceState.NoPermissions,
                _ => DeviceState.Unknown,
            };

            string? model = null, product = null;
            foreach (Match kv in KeyValueRegex.Matches(match.Groups["rest"].Value))
            {
                var key = kv.Groups["key"].Value;
                var value = kv.Groups["value"].Value;
                if (key == "model") model = value;
                else if (key == "product") product = value;
            }

            devices.Add(new DeviceInfo { Serial = serial, State = state, Model = model, Product = product });
        }

        return devices;
    }

    public async Task<(JobStatus Status, string? Message)> InstallApkAsync(string serial, string apkPath, CancellationToken ct)
    {
        AdbCommandResult result;
        try
        {
            result = await RunProcessAsync(new[] { "-s", serial, "install", "-r", apkPath }, InstallTimeout, ct);
        }
        catch (OperationCanceledException)
        {
            return (JobStatus.Cancelled, null);
        }

        var combined = result.StdOut + "\n" + result.StdErr;

        if (combined.Contains("device offline", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("device not found", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("device unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            return (JobStatus.DeviceUnavailable, "L'appareil s'est déconnecté ou est indisponible.");
        }

        if (result.ExitCode == 0 && combined.Contains("Success", StringComparison.Ordinal))
            return (JobStatus.Success, null);

        var failureMatch = FailureRegex.Match(combined);
        if (failureMatch.Success)
        {
            var code = failureMatch.Groups["code"].Value;
            return (JobStatus.Failed, DescribeFailureCode(code));
        }

        var trimmedError = result.StdErr.Trim();
        if (trimmedError.Length > 500)
            trimmedError = trimmedError[..500] + "...";

        return (JobStatus.Failed, trimmedError.Length > 0 ? trimmedError : "Échec de l'installation (raison inconnue).");
    }

    private static string DescribeFailureCode(string code) => code switch
    {
        "INSTALL_FAILED_INSUFFICIENT_STORAGE" => "Stockage insuffisant sur l'appareil",
        "INSTALL_FAILED_VERSION_DOWNGRADE" => "Version installée plus récente (downgrade refusé)",
        "INSTALL_FAILED_UPDATE_INCOMPATIBLE" => "Signature APK incompatible avec l'appli installée",
        "INSTALL_FAILED_ALREADY_EXISTS" => "Signature APK incompatible avec l'appli installée",
        "INSTALL_FAILED_NO_MATCHING_ABIS" => "APK incompatible avec l'architecture de l'appareil",
        "INSTALL_FAILED_INVALID_APK" => "APK corrompu ou invalide",
        "INSTALL_CANCELED_BY_USER" => "Installation annulée sur l'appareil",
        _ => $"Échec : {code}",
    };

    private static async Task<AdbCommandResult> RunProcessAsync(IEnumerable<string> arguments, TimeSpan timeout, CancellationToken ct)
    {
        var adbPath = AdbPathResolver.ResolveAdbPath();

        var psi = new ProcessStartInfo
        {
            FileName = adbPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* already exited */ }

            if (ct.IsCancellationRequested)
                throw new OperationCanceledException(ct);

            throw new TimeoutException($"La commande adb a dépassé le délai de {timeout.TotalSeconds:0}s.");
        }

        return new AdbCommandResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }
}
