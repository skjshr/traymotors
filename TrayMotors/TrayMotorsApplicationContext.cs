namespace TrayMotors;

public sealed class TrayMotorsApplicationContext : ApplicationContext
{
    private readonly TrayIconCache iconCache = new();
    private readonly System.Windows.Forms.Timer animationTimer = new();
    private readonly List<TrayResourceAnimator> animators = [];
    private readonly List<ToolStripMenuItem> startupMenuItems = [];
    private readonly List<ToolStripMenuItem> temperatureMenuItems = [];
    private AppSettings settings;
    private CompositeMetricSampler sampler;
    private StatusPanelForm? statusPanel;

    public TrayMotorsApplicationContext(AppSettings settings)
    {
        this.settings = settings.Normalized();
        ApplyStartupRegistration();
        sampler = new CompositeMetricSampler(this.settings);
        sampler.Start();

        foreach (var kind in Enum.GetValues<ResourceKind>())
        {
            animators.Add(new TrayResourceAnimator(
                kind,
                iconCache,
                CreateMenu(),
                kind => sampler.GetLatest(kind),
                (_, _) => ShowStatusPanel()));
        }

        animationTimer.Interval = Math.Max(16, 1000 / Math.Max(1, settings.MaxIconFps));
        animationTimer.Tick += (_, _) =>
        {
            foreach (var animator in animators)
            {
                animator.Tick();
            }
        };
        animationTimer.Start();
    }

    private ContextMenuStrip CreateMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Status", null, (_, _) => ShowStatusPanel());

        var startupMenuItem = new ToolStripMenuItem();
        startupMenuItem.Click += (_, _) => ToggleStartWithWindows();
        startupMenuItems.Add(startupMenuItem);
        menu.Items.Add(startupMenuItem);

        var temperatureMenuItem = new ToolStripMenuItem();
        temperatureMenuItem.Click += (_, _) => ToggleTemperatureSensors();
        temperatureMenuItems.Add(temperatureMenuItem);
        menu.Items.Add(temperatureMenuItem);

        RefreshMenuItems();

        menu.Items.Add("Open log", null, (_, _) => OpenLog());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        return menu;
    }

    private void ToggleStartWithWindows()
    {
        settings = settings with { StartWithWindows = !settings.StartWithWindows };
        try
        {
            ApplyStartupRegistration();
            SettingsStore.Save(settings);
            RefreshMenuItems();
            AppLog.Info($"Start with Windows enabled: {settings.StartWithWindows}");
        }
        catch (Exception exception)
        {
            AppLog.Error("Startup registration failed", exception);
            settings = settings with { StartWithWindows = StartupRegistration.IsEnabled() };
            SettingsStore.Save(settings);
            RefreshMenuItems();
        }
    }

    private void ApplyStartupRegistration() => StartupRegistration.SetEnabled(settings.StartWithWindows);

    private void RefreshMenuItems()
    {
        foreach (var item in startupMenuItems)
        {
            item.SetChecked(settings.StartWithWindows, "Start with Windows", "Stop starting with Windows");
        }

        foreach (var item in temperatureMenuItems)
        {
            item.SetChecked(settings.EnableTemperatureSensors, "Enable temperature sensors", "Disable temperature sensors");
        }
    }

    private void ToggleTemperatureSensors()
    {
        settings = settings with { EnableTemperatureSensors = !settings.EnableTemperatureSensors };
        SettingsStore.Save(settings);
        RefreshMenuItems();

        sampler.Dispose();
        sampler = new CompositeMetricSampler(settings);
        sampler.Start();
        AppLog.Info($"Temperature sensors enabled: {settings.EnableTemperatureSensors}");
    }

    private void ShowStatusPanel()
    {
        statusPanel ??= new StatusPanelForm(kind => sampler.GetLatest(kind));

        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 800, 600);
        statusPanel.Location = new Point(
            Math.Max(workingArea.Left, workingArea.Right - statusPanel.Width - 12),
            Math.Max(workingArea.Top, workingArea.Bottom - statusPanel.Height - 12));

        statusPanel.Show();
        statusPanel.Activate();
    }

    private static void OpenLog()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AppLog.LogPath)!);
            if (!File.Exists(AppLog.LogPath))
            {
                File.WriteAllText(AppLog.LogPath, string.Empty);
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = AppLog.LogPath,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            AppLog.Error("Opening log failed", exception);
        }
    }

    protected override void ExitThreadCore()
    {
        animationTimer.Stop();
        animationTimer.Dispose();
        statusPanel?.Dispose();

        foreach (var animator in animators)
        {
            animator.Dispose();
        }

        sampler.Dispose();
        iconCache.Dispose();
        base.ExitThreadCore();
    }
}
