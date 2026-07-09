using System.IO;

namespace ApkDragNDrop.Models;

public class ApkQueueItem
{
    public required string Path { get; init; }
    public string FileName => System.IO.Path.GetFileName(Path);
    public string SizeText
    {
        get
        {
            try
            {
                var bytes = new FileInfo(Path).Length;
                return bytes >= 1024 * 1024
                    ? $"{bytes / (1024.0 * 1024.0):0.0} Mo"
                    : $"{bytes / 1024.0:0} Ko";
            }
            catch
            {
                return "";
            }
        }
    }
}
