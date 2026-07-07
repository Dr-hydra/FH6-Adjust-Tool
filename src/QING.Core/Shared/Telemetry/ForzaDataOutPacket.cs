using System;

namespace QING.Core.Telemetry;

public sealed class ForzaDataOutPacket
{
    public const int PacketLength = 324;
    public const int MinimumPacketLength = 323;

    public int IsRaceOn { get; private set; }
    public uint TimestampMs { get; private set; }
    public float EngineMaxRpm { get; private set; }
    public float EngineIdleRpm { get; private set; }
    public float CurrentEngineRpm { get; private set; }
    public float AccelerationX { get; private set; }
    public float AccelerationY { get; private set; }
    public float AccelerationZ { get; private set; }
    public float VelocityX { get; private set; }
    public float VelocityY { get; private set; }
    public float VelocityZ { get; private set; }
    public float AngularVelocityX { get; private set; }
    public float AngularVelocityY { get; private set; }
    public float AngularVelocityZ { get; private set; }
    public float Yaw { get; private set; }
    public float Pitch { get; private set; }
    public float Roll { get; private set; }
    public float NormalizedSuspensionTravelFrontLeft { get; private set; }
    public float NormalizedSuspensionTravelFrontRight { get; private set; }
    public float NormalizedSuspensionTravelRearLeft { get; private set; }
    public float NormalizedSuspensionTravelRearRight { get; private set; }
    public float TireSlipRatioFrontLeft { get; private set; }
    public float TireSlipRatioFrontRight { get; private set; }
    public float TireSlipRatioRearLeft { get; private set; }
    public float TireSlipRatioRearRight { get; private set; }
    public float WheelRotationSpeedFrontLeft { get; private set; }
    public float WheelRotationSpeedFrontRight { get; private set; }
    public float WheelRotationSpeedRearLeft { get; private set; }
    public float WheelRotationSpeedRearRight { get; private set; }
    public int WheelOnRumbleStripFrontLeft { get; private set; }
    public int WheelOnRumbleStripFrontRight { get; private set; }
    public int WheelOnRumbleStripRearLeft { get; private set; }
    public int WheelOnRumbleStripRearRight { get; private set; }
    public int WheelInPuddleDepthFrontLeft { get; private set; }
    public int WheelInPuddleDepthFrontRight { get; private set; }
    public int WheelInPuddleDepthRearLeft { get; private set; }
    public int WheelInPuddleDepthRearRight { get; private set; }
    public float SurfaceRumbleFrontLeft { get; private set; }
    public float SurfaceRumbleFrontRight { get; private set; }
    public float SurfaceRumbleRearLeft { get; private set; }
    public float SurfaceRumbleRearRight { get; private set; }
    public float TireSlipAngleFrontLeft { get; private set; }
    public float TireSlipAngleFrontRight { get; private set; }
    public float TireSlipAngleRearLeft { get; private set; }
    public float TireSlipAngleRearRight { get; private set; }
    public float TireCombinedSlipFrontLeft { get; private set; }
    public float TireCombinedSlipFrontRight { get; private set; }
    public float TireCombinedSlipRearLeft { get; private set; }
    public float TireCombinedSlipRearRight { get; private set; }
    public float SuspensionTravelMetersFrontLeft { get; private set; }
    public float SuspensionTravelMetersFrontRight { get; private set; }
    public float SuspensionTravelMetersRearLeft { get; private set; }
    public float SuspensionTravelMetersRearRight { get; private set; }
    public int CarOrdinal { get; private set; }
    public int CarClass { get; private set; }
    public int CarPerformanceIndex { get; private set; }
    public int DrivetrainType { get; private set; }
    public int NumCylinders { get; private set; }
    public uint CarGroup { get; private set; }
    public float SmashableVelDiff { get; private set; }
    public float SmashableMass { get; private set; }
    public float PositionX { get; private set; }
    public float PositionY { get; private set; }
    public float PositionZ { get; private set; }
    public float Speed { get; private set; }
    public float Power { get; private set; }
    public float Torque { get; private set; }
    public float TireTempFrontLeft { get; private set; }
    public float TireTempFrontRight { get; private set; }
    public float TireTempRearLeft { get; private set; }
    public float TireTempRearRight { get; private set; }
    public float Boost { get; private set; }
    public float Fuel { get; private set; }
    public float DistanceTraveled { get; private set; }
    public float BestLap { get; private set; }
    public float LastLap { get; private set; }
    public float CurrentLap { get; private set; }
    public float CurrentRaceTime { get; private set; }
    public ushort LapNumber { get; private set; }
    public byte RacePosition { get; private set; }
    public byte Accel { get; private set; }
    public byte Brake { get; private set; }
    public byte Clutch { get; private set; }
    public byte HandBrake { get; private set; }
    public byte Gear { get; private set; }
    public sbyte Steer { get; private set; }
    public sbyte NormalizedDrivingLine { get; private set; }
    public sbyte NormalizedAiBrakeDifference { get; private set; }

    public static bool TryParse(byte[] data, int length, out ForzaDataOutPacket? packet)
    {
        packet = null;
        if (data == null || length < MinimumPacketLength)
        {
            return false;
        }

        try
        {
            packet = new ForzaDataOutPacket
            {
                IsRaceOn = S32(data, 0),
                TimestampMs = U32(data, 4),
                EngineMaxRpm = F32(data, 8),
                EngineIdleRpm = F32(data, 12),
                CurrentEngineRpm = F32(data, 16),
                AccelerationX = F32(data, 20),
                AccelerationY = F32(data, 24),
                AccelerationZ = F32(data, 28),
                VelocityX = F32(data, 32),
                VelocityY = F32(data, 36),
                VelocityZ = F32(data, 40),
                AngularVelocityX = F32(data, 44),
                AngularVelocityY = F32(data, 48),
                AngularVelocityZ = F32(data, 52),
                Yaw = F32(data, 56),
                Pitch = F32(data, 60),
                Roll = F32(data, 64),
                NormalizedSuspensionTravelFrontLeft = F32(data, 68),
                NormalizedSuspensionTravelFrontRight = F32(data, 72),
                NormalizedSuspensionTravelRearLeft = F32(data, 76),
                NormalizedSuspensionTravelRearRight = F32(data, 80),
                TireSlipRatioFrontLeft = F32(data, 84),
                TireSlipRatioFrontRight = F32(data, 88),
                TireSlipRatioRearLeft = F32(data, 92),
                TireSlipRatioRearRight = F32(data, 96),
                WheelRotationSpeedFrontLeft = F32(data, 100),
                WheelRotationSpeedFrontRight = F32(data, 104),
                WheelRotationSpeedRearLeft = F32(data, 108),
                WheelRotationSpeedRearRight = F32(data, 112),
                WheelOnRumbleStripFrontLeft = S32(data, 116),
                WheelOnRumbleStripFrontRight = S32(data, 120),
                WheelOnRumbleStripRearLeft = S32(data, 124),
                WheelOnRumbleStripRearRight = S32(data, 128),
                WheelInPuddleDepthFrontLeft = S32(data, 132),
                WheelInPuddleDepthFrontRight = S32(data, 136),
                WheelInPuddleDepthRearLeft = S32(data, 140),
                WheelInPuddleDepthRearRight = S32(data, 144),
                SurfaceRumbleFrontLeft = F32(data, 148),
                SurfaceRumbleFrontRight = F32(data, 152),
                SurfaceRumbleRearLeft = F32(data, 156),
                SurfaceRumbleRearRight = F32(data, 160),
                TireSlipAngleFrontLeft = F32(data, 164),
                TireSlipAngleFrontRight = F32(data, 168),
                TireSlipAngleRearLeft = F32(data, 172),
                TireSlipAngleRearRight = F32(data, 176),
                TireCombinedSlipFrontLeft = F32(data, 180),
                TireCombinedSlipFrontRight = F32(data, 184),
                TireCombinedSlipRearLeft = F32(data, 188),
                TireCombinedSlipRearRight = F32(data, 192),
                SuspensionTravelMetersFrontLeft = F32(data, 196),
                SuspensionTravelMetersFrontRight = F32(data, 200),
                SuspensionTravelMetersRearLeft = F32(data, 204),
                SuspensionTravelMetersRearRight = F32(data, 208),
                CarOrdinal = S32(data, 212),
                CarClass = S32(data, 216),
                CarPerformanceIndex = S32(data, 220),
                DrivetrainType = S32(data, 224),
                NumCylinders = S32(data, 228),
                CarGroup = U32(data, 232),
                SmashableVelDiff = F32(data, 236),
                SmashableMass = F32(data, 240),
                PositionX = F32(data, 244),
                PositionY = F32(data, 248),
                PositionZ = F32(data, 252),
                Speed = F32(data, 256),
                Power = F32(data, 260),
                Torque = F32(data, 264),
                TireTempFrontLeft = F32(data, 268),
                TireTempFrontRight = F32(data, 272),
                TireTempRearLeft = F32(data, 276),
                TireTempRearRight = F32(data, 280),
                Boost = F32(data, 284),
                Fuel = F32(data, 288),
                DistanceTraveled = F32(data, 292),
                BestLap = F32(data, 296),
                LastLap = F32(data, 300),
                CurrentLap = F32(data, 304),
                CurrentRaceTime = F32(data, 308),
                LapNumber = U16(data, 312),
                RacePosition = data[314],
                Accel = data[315],
                Brake = data[316],
                Clutch = data[317],
                HandBrake = data[318],
                Gear = data[319],
                Steer = S8(data, 320),
                NormalizedDrivingLine = S8(data, 321),
                NormalizedAiBrakeDifference = S8(data, 322)
            };
            return true;
        }
        catch
        {
            packet = null;
            return false;
        }
    }

    public static string ClassName(int value)
    {
        return value switch
        {
            0 => "D",
            1 => "C",
            2 => "B",
            3 => "A",
            4 => "S1",
            5 => "S2",
            6 => "R",
            7 => "X",
            _ => value.ToString()
        };
    }

    public static string DrivetrainName(int value)
    {
        return value switch
        {
            0 => "FWD",
            1 => "RWD",
            2 => "AWD",
            _ => value.ToString()
        };
    }

    private static int S32(byte[] data, int offset)
    {
        return BitConverter.ToInt32(data, offset);
    }

    private static uint U32(byte[] data, int offset)
    {
        return BitConverter.ToUInt32(data, offset);
    }

    private static ushort U16(byte[] data, int offset)
    {
        return BitConverter.ToUInt16(data, offset);
    }

    private static float F32(byte[] data, int offset)
    {
        return BitConverter.ToSingle(data, offset);
    }

    private static sbyte S8(byte[] data, int offset)
    {
        return unchecked((sbyte)data[offset]);
    }
}
