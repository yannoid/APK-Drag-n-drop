namespace ApkDragNDrop.Models;

public enum DeviceState
{
    Unknown,
    Device,
    Offline,
    Unauthorized,
    NoPermissions,
}

public enum JobStatus
{
    Queued,
    Installing,
    Success,
    Failed,
    Cancelled,
    DeviceUnavailable,
}
