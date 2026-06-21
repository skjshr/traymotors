namespace TrayMotors;

public static class AppLog
{
    private static readonly object Gate = new();
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TrayMotors");

    public static string LogPath => Path.Combine(LogDirectory, "traymotors.log");

    public static void Info(string message) => Write("INFO", message);

    public static void Error(string message, Exception exception) =>
        Write("ERROR", $"{message}: {exception.GetType().Name}: {exception.Message}");

    private static void Write(string level, string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(LogDirectory);
                File.AppendAllText(
                    LogPath,
                    $"{DateTimeOffset.Now:O} [{level}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never make a tray utility fail.
        }
    }
}
