using System.Buffers.Binary;

namespace FH6TelemetryHud.Telemetry;

public static class ForzaPacketParser
{
    public const int PacketSize = 324;

    public static bool TryParse(ReadOnlySpan<byte> packet, out ForzaTelemetryData data)
    {
        data = null!;
        if (packet.Length < PacketSize)
        {
            return false;
        }

        data = new ForzaTelemetryData
        {
            IsRaceOn = ReadInt32(packet, 0),
            TimestampMs = ReadUInt32(packet, 4),
            EngineMaxRpm = ReadSingle(packet, 8),
            EngineIdleRpm = ReadSingle(packet, 12),
            CurrentEngineRpm = ReadSingle(packet, 16),
            Speed = ReadSingle(packet, 256),
            Accel = packet[315],
            Brake = packet[316],
            Clutch = packet[317],
            HandBrake = packet[318],
            Gear = packet[319],
            Steer = unchecked((sbyte)packet[320])
        };

        return true;
    }

    private static int ReadInt32(ReadOnlySpan<byte> packet, int offset) =>
        BinaryPrimitives.ReadInt32LittleEndian(packet.Slice(offset, sizeof(int)));

    private static uint ReadUInt32(ReadOnlySpan<byte> packet, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(packet.Slice(offset, sizeof(uint)));

    private static float ReadSingle(ReadOnlySpan<byte> packet, int offset)
    {
        var bits = BinaryPrimitives.ReadInt32LittleEndian(packet.Slice(offset, sizeof(float)));
        return BitConverter.Int32BitsToSingle(bits);
    }
}
