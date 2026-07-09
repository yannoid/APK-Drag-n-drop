using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ApkDragNDrop.Models;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace ApkDragNDrop.Converters;

public class DeviceStateToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var state = value as DeviceState? ?? DeviceState.Unknown;
        var colorHex = state == DeviceState.Device ? "#3DDC84" : "#9BA0AE";
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class JobStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value as JobStatus? ?? JobStatus.Queued;
        var colorHex = status switch
        {
            JobStatus.Installing => "#5B8CFF",
            JobStatus.Success => "#2FBE7A",
            JobStatus.Failed => "#FF5A6A",
            JobStatus.DeviceUnavailable => "#FF5A6A",
            JobStatus.Cancelled => "#9BA0AE",
            _ => "#8B93A6",
        };
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
