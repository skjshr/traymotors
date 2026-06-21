namespace TrayMotors;

public interface ITrayAnimator
{
    void SetSnapshot(MetricSnapshot snapshot);

    void Tick();
}
