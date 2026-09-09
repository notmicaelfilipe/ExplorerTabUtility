using System;
using System.IO;

namespace ExplorerTabUtility.Helpers;

/// <summary>Minimal always-on file log: %AppData%\ExplorerTabUtility\log.txt</summary>
internal static class Log
{
    private static readonly object _lock = new();
    private static readonly string FilePath;

    static Log()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Constants.AppName);

        FilePath = Path.Combine(directory, Constants.LogFileName);

        try
        {
            Directory.CreateDirectory(directory);

            // A session writes a handful of lines, so this is rare, but never lose the
            // previous file outright: the session we care about may be far back.
            if (File.Exists(FilePath) && new FileInfo(FilePath).Length > 1_000_000)
                File.Copy(FilePath, Path.ChangeExtension(FilePath, ".old.txt"), overwrite: true);
            else
                return;

            File.Delete(FilePath);
        }
        catch { /* logging must never break the app */ }
    }

    public static void Write(string message)
    {
        try
        {
            lock (_lock)
                File.AppendAllText(FilePath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{Environment.CurrentManagedThreadId}] {message}{Environment.NewLine}");
        }
        catch { /* logging must never break the app */ }
    }
}
