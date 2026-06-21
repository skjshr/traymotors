namespace TrayMotors;

public sealed class StatusPanelForm : Form
{
    private readonly Func<ResourceKind, MetricSnapshot> snapshotProvider;
    private readonly System.Windows.Forms.Timer refreshTimer = new();
    private readonly Dictionary<ResourceKind, Label> valueLabels = new();

    public StatusPanelForm(Func<ResourceKind, MetricSnapshot> snapshotProvider)
    {
        this.snapshotProvider = snapshotProvider;

        Text = "TrayMotors";
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Width = 310;
        Height = 170;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 4,
            Padding = new Padding(10),
            AutoSize = false
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddHeader(layout);
        AddRow(layout, 1, ResourceKind.Cpu, "CPU");
        AddRow(layout, 2, ResourceKind.Memory, "MEM");
        AddRow(layout, 3, ResourceKind.Gpu, "GPU");
        Controls.Add(layout);

        refreshTimer.Interval = 500;
        refreshTimer.Tick += (_, _) => RefreshValues();
        VisibleChanged += (_, _) =>
        {
            if (Visible)
            {
                RefreshValues();
                refreshTimer.Start();
            }
            else
            {
                refreshTimer.Stop();
            }
        };
    }

    private static void AddHeader(TableLayoutPanel layout)
    {
        layout.Controls.Add(Header("Kind"), 0, 0);
        layout.Controls.Add(Header("Usage"), 1, 0);
        layout.Controls.Add(Header("Temp"), 2, 0);
        layout.Controls.Add(Header("Source"), 3, 0);
    }

    private void AddRow(TableLayoutPanel layout, int row, ResourceKind kind, string label)
    {
        layout.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        var valueLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        valueLabels[kind] = valueLabel;
        layout.Controls.Add(valueLabel, 1, row);
        layout.Controls.Add(new Label { Name = $"{kind}Temp", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 2, row);
        layout.Controls.Add(new Label { Name = $"{kind}Source", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 3, row);
    }

    private static Label Header(string text) =>
        new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(SystemFonts.MessageBoxFont ?? Control.DefaultFont, FontStyle.Bold)
        };

    private void RefreshValues()
    {
        foreach (var kind in Enum.GetValues<ResourceKind>())
        {
            var snapshot = snapshotProvider(kind);
            valueLabels[kind].Text = $"{snapshot.UsagePercent:0}%";
            Controls.Find($"{kind}Temp", searchAllChildren: true).OfType<Label>().First().Text =
                snapshot.TemperatureCelsius is { } temp ? $"{temp:0} C" : "--";
            Controls.Find($"{kind}Source", searchAllChildren: true).OfType<Label>().First().Text =
                snapshot.SampleSource.ToString();
        }
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        Hide();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            refreshTimer.Dispose();
        }

        base.Dispose(disposing);
    }
}
