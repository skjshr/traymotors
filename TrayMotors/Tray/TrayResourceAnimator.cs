namespace TrayMotors;

public sealed class TrayResourceAnimator : ITrayAnimator, IDisposable
{
    private readonly NotifyIcon notifyIcon;
    private readonly TrayIconCache iconCache;
    private readonly ResourceKind kind;
    private readonly Func<ResourceKind, MetricSnapshot> snapshotProvider;
    private readonly ContextMenuStrip menu;
    private MetricSnapshot snapshot;
    private DateTimeOffset lastTick = DateTimeOffset.Now;
    private double phase;
    private int lastFrame = -1;
    private IconColorBucket lastBucket;

    public TrayResourceAnimator(
        ResourceKind kind,
        TrayIconCache iconCache,
        ContextMenuStrip menu,
        Func<ResourceKind, MetricSnapshot> snapshotProvider,
        EventHandler clickHandler)
    {
        this.kind = kind;
        this.iconCache = iconCache;
        this.menu = menu;
        this.snapshotProvider = snapshotProvider;
        snapshot = MetricSnapshot.Unknown(kind);
        lastBucket = IconColorBucket.Unknown;

        notifyIcon = new NotifyIcon
        {
            Text = $"TrayMotors {Label(kind)}",
            Icon = iconCache.Get(kind, 0, IconColorBucket.Unknown),
            ContextMenuStrip = menu,
            Visible = true
        };

        notifyIcon.MouseClick += (_, args) =>
        {
            if (args.Button == MouseButtons.Left)
            {
                clickHandler(this, EventArgs.Empty);
            }
        };
    }

    public void SetSnapshot(MetricSnapshot snapshot) => this.snapshot = snapshot;

    public void Tick()
    {
        SetSnapshot(snapshotProvider(kind));

        var now = DateTimeOffset.Now;
        var delta = Math.Max(0.001, (now - lastTick).TotalSeconds);
        lastTick = now;

        phase += FramesPerSecond(snapshot) * delta;
        var frame = ((int)Math.Floor(phase)) % TrayIconCache.FrameCount;
        var bucket = ToBucket(snapshot.PressureState);

        if (frame == lastFrame && bucket == lastBucket)
        {
            return;
        }

        notifyIcon.Icon = iconCache.Get(kind, frame, bucket);
        notifyIcon.Text = ToolTip(snapshot);
        lastFrame = frame;
        lastBucket = bucket;
    }

    private static double FramesPerSecond(MetricSnapshot snapshot)
    {
        if (snapshot.SampleSource == SampleSource.Unavailable)
        {
            return 1;
        }

        var usage = Math.Clamp(snapshot.UsagePercent, 0, 100);

        if (snapshot.ResourceKind == ResourceKind.Memory && usage >= 88)
        {
            var wobble = DateTimeOffset.Now.Millisecond % 3 == 0 ? 0.35 : 0.7;
            return 2.0 + wobble;
        }

        return snapshot.ResourceKind switch
        {
            ResourceKind.Cpu => 2.0 + usage / 8.0,
            ResourceKind.Gpu => 1.5 + usage / 7.0,
            ResourceKind.Memory => 1.0 + usage / 12.0,
            _ => 1
        };
    }

    private static IconColorBucket ToBucket(PressureState state) =>
        state switch
        {
            PressureState.Warm => IconColorBucket.Warm,
            PressureState.Hot => IconColorBucket.Hot,
            PressureState.Critical => IconColorBucket.Critical,
            PressureState.Unknown => IconColorBucket.Unknown,
            _ => IconColorBucket.Normal
        };

    private static string Label(ResourceKind kind) =>
        kind switch
        {
            ResourceKind.Cpu => "CPU",
            ResourceKind.Memory => "MEM",
            ResourceKind.Gpu => "GPU",
            _ => kind.ToString()
        };

    private static string ToolTip(MetricSnapshot snapshot)
    {
        var temperature = snapshot.TemperatureCelsius is { } value ? $" {value:0}C" : "";
        var text = $"{Label(snapshot.ResourceKind)} {snapshot.UsagePercent:0}%{temperature} {snapshot.SampleSource}";
        return text.Length <= 63 ? text : text[..63];
    }

    public void Dispose()
    {
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        menu.Dispose();
    }
}
