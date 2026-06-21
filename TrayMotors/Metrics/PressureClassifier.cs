namespace TrayMotors;

public static class PressureClassifier
{
    public static PressureState Classify(ResourceKind kind, double usagePercent, double? temperatureCelsius)
    {
        if (temperatureCelsius is { } temperature)
        {
            return temperature switch
            {
                >= 90 => PressureState.Critical,
                >= 80 => PressureState.Hot,
                >= 65 => PressureState.Warm,
                _ => PressureState.Normal
            };
        }

        if (kind == ResourceKind.Memory)
        {
            return usagePercent switch
            {
                >= 95 => PressureState.Critical,
                >= 88 => PressureState.Hot,
                >= 75 => PressureState.Warm,
                _ => PressureState.Normal
            };
        }

        return usagePercent switch
        {
            >= 98 => PressureState.Critical,
            >= 85 => PressureState.Hot,
            >= 65 => PressureState.Warm,
            _ => PressureState.Normal
        };
    }
}
