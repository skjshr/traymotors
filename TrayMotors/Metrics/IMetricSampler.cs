namespace TrayMotors;

public interface IMetricSampler
{
    void Start();

    void Stop();

    MetricSnapshot GetLatest(ResourceKind kind);
}
