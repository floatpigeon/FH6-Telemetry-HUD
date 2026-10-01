namespace FH6TelemetryHud.Telemetry;

public sealed class ForzaTelemetryData
{
    public int IsRaceOn { get; init; }
    public uint TimestampMs { get; init; }
    public float EngineMaxRpm { get; init; }
    public float EngineIdleRpm { get; init; }
    public float CurrentEngineRpm { get; init; }
    public float Speed { get; init; }
    public byte Accel { get; init; }
    public byte Brake { get; init; }
    public byte Clutch { get; init; }
    public byte HandBrake { get; init; }
    public byte Gear { get; init; }
    public sbyte Steer { get; init; }
}
