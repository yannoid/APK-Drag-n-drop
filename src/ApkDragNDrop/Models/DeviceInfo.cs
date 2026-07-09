using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ApkDragNDrop.Models;

public class DeviceInfo : INotifyPropertyChanged
{
    public required string Serial { get; init; }
    public DeviceState State { get; set; } = DeviceState.Unknown;
    public string? Model { get; set; }
    public string? Product { get; set; }

    public bool IsUsable => State == DeviceState.Device;

    public string DisplayModel => string.IsNullOrWhiteSpace(Model) ? Serial : Model!.Replace('_', ' ');

    public string StatusText => State switch
    {
        DeviceState.Device => "En ligne",
        DeviceState.Offline => "Hors ligne",
        DeviceState.Unauthorized => "Non autorisé",
        DeviceState.NoPermissions => "Permissions manquantes",
        _ => "Inconnu",
    };

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
