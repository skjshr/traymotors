namespace TrayMotors;

static class Program
{
    private const string MutexName = @"Local\TrayMotors";

    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        using var context = new TrayMotorsApplicationContext(SettingsStore.Load());
        Application.Run(context);
    }
}
