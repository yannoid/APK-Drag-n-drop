namespace ApkDragNDrop.Services;

public class AdbNotFoundException : Exception
{
    public AdbNotFoundException()
        : base("adb.exe introuvable (ni dans le dossier de l'application, ni dans le PATH système).")
    {
    }
}
