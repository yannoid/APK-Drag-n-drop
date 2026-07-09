using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using ApkDragNDrop.Models;

namespace ApkDragNDrop.Services;

public class InstallQueueManager
{
    private const int MaxParallelDevices = 3;

    private readonly AdbService _adbService;
    private readonly SemaphoreSlim _globalConcurrency = new(MaxParallelDevices);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _deviceLocks = new();
    private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;

    public ObservableCollection<InstallJob> Jobs { get; } = new();

    public InstallQueueManager(AdbService adbService)
    {
        _adbService = adbService;
    }

    public void Enqueue(IReadOnlyList<string> apkPaths, IReadOnlyList<DeviceInfo> devices)
    {
        var newJobs = new List<InstallJob>();
        foreach (var device in devices)
        {
            foreach (var apk in apkPaths)
            {
                var job = new InstallJob { ApkPath = apk, TargetDevice = device };
                newJobs.Add(job);
                Jobs.Insert(0, job);
            }
        }

        foreach (var device in devices)
            _ = RunDeviceLaneAsync(device, newJobs.Where(j => j.TargetDevice.Serial == device.Serial).ToList());
    }

    private async Task RunDeviceLaneAsync(DeviceInfo device, List<InstallJob> jobsForDevice)
    {
        var deviceLock = _deviceLocks.GetOrAdd(device.Serial, _ => new SemaphoreSlim(1, 1));

        foreach (var job in jobsForDevice)
        {
            await _globalConcurrency.WaitAsync();
            await deviceLock.WaitAsync();
            try
            {
                Post(() => job.Status = JobStatus.Installing);
                var (status, message) = await _adbService.InstallApkAsync(device.Serial, job.ApkPath, CancellationToken.None);
                Post(() =>
                {
                    job.Status = status;
                    job.ResultMessage = message;
                });
            }
            catch (Exception ex)
            {
                Post(() =>
                {
                    job.Status = JobStatus.Failed;
                    job.ResultMessage = ex.Message;
                });
            }
            finally
            {
                deviceLock.Release();
                _globalConcurrency.Release();
            }
        }
    }

    private void Post(Action action)
    {
        if (_uiContext is not null)
            _uiContext.Post(_ => action(), null);
        else
            action();
    }
}
