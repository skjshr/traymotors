namespace TrayMotors.Tests;

public sealed class PressureClassifierTests
{
    [Theory]
    [InlineData(ResourceKind.Cpu, 10, null, PressureState.Normal)]
    [InlineData(ResourceKind.Cpu, 86, null, PressureState.Hot)]
    [InlineData(ResourceKind.Memory, 76, null, PressureState.Warm)]
    [InlineData(ResourceKind.Memory, 96, null, PressureState.Critical)]
    [InlineData(ResourceKind.Gpu, 12, 82.0, PressureState.Hot)]
    public void Classify_MapsUsageAndTemperatureToPressure(
        ResourceKind kind,
        double usage,
        double? temperature,
        PressureState expected)
    {
        Assert.Equal(expected, PressureClassifier.Classify(kind, usage, temperature));
    }
}
