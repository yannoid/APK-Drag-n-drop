using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace ApkDragNDrop.Models;

public class InstallJob : INotifyPropertyChanged
{
    public Guid Id { get; } = Guid.NewGuid();
    public required string ApkPath { get; init; }
    public required DeviceInfo TargetDevice { get; init; }

    public string ApkFileName => Path.GetFileName(ApkPath);

    private JobStatus _status = JobStatus.Queued;
    public JobStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); }
    }

    private string? _resultMessage;
    public string? ResultMessage
    {
        get => _resultMessage;
        set { _resultMessage = value; OnPropertyChanged(); }
    }

    public string StatusText => Status switch
    {
        JobStatus.Queued => "En attente",
        JobStatus.Installing => "Installation...",
        JobStatus.Success => "Installé",
        JobStatus.Failed => "Échec",
        JobStatus.Cancelled => "Annulé",
        JobStatus.DeviceUnavailable => "Appareil indisponible",
        _ => "",
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
