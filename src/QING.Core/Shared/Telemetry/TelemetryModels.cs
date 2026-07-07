using System;
using System.Collections.Generic;

namespace QING.Core.Telemetry;

public sealed class TelemetrySessionContext
{
    public string TuneId { get; set; } = "";
    public string TuneName { get; set; } = "";
    public string CarName { get; set; } = "";
    public string Source { get; set; } = "Tuner";
    public string Label { get; set; } = "";

    public static TelemetrySessionContext Empty { get; } = new();
}

public sealed class TelemetrySample
{
    public long Sequence { get; set; }
    public long ReceivedAtMs { get; set; }
    public uint GameTimestampMs { get; set; }
    public bool RaceOn { get; set; }
    public int CarOrdinal { get; set; }
    public int CarClassId { get; set; }
    public string CarClassName { get; set; } = "";
    public int PerformanceIndex { get; set; }
    public int DrivetrainId { get; set; }
    public string DrivetrainName { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float SpeedMps { get; set; }
    public double SpeedKmh => SpeedMps * 3.6;
    public double SpeedMph => SpeedMps * 2.2369362921;
    public float CurrentRpm { get; set; }
    public float EngineMaxRpm { get; set; }
    public float EngineIdleRpm { get; set; }
    public float PowerW { get; set; }
    public double PowerKw => PowerW / 1000.0;
    public float TorqueNm { get; set; }
    public float Boost { get; set; }
    public float Fuel { get; set; }
    public float DistanceTraveled { get; set; }
    public int Throttle { get; set; }
    public int Brake { get; set; }
    public int Clutch { get; set; }
    public int Handbrake { get; set; }
    public int Gear { get; set; }
    public int Steer { get; set; }
    public int NormalizedDrivingLine { get; set; }
    public int NormalizedAiBrakeDifference { get; set; }
    public float AccelerationX { get; set; }
    public float AccelerationY { get; set; }
    public float AccelerationZ { get; set; }
    public double AccelLatG => AccelerationX / 9.80665;
    public double AccelVertG => AccelerationY / 9.80665;
    public double AccelLongG => AccelerationZ / 9.80665;
    public float VelocityX { get; set; }
    public float VelocityY { get; set; }
    public float VelocityZ { get; set; }
    public float AngularVelocityX { get; set; }
    public float AngularVelocityY { get; set; }
    public float AngularVelocityZ { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float Roll { get; set; }
    public float TireTempFrontLeft { get; set; }
    public float TireTempFrontRight { get; set; }
    public float TireTempRearLeft { get; set; }
    public float TireTempRearRight { get; set; }
    public float TireSlipRatioFrontLeft { get; set; }
    public float TireSlipRatioFrontRight { get; set; }
    public float TireSlipRatioRearLeft { get; set; }
    public float TireSlipRatioRearRight { get; set; }
    public float TireSlipAngleFrontLeft { get; set; }
    public float TireSlipAngleFrontRight { get; set; }
    public float TireSlipAngleRearLeft { get; set; }
    public float TireSlipAngleRearRight { get; set; }
    public float TireCombinedSlipFrontLeft { get; set; }
    public float TireCombinedSlipFrontRight { get; set; }
    public float TireCombinedSlipRearLeft { get; set; }
    public float TireCombinedSlipRearRight { get; set; }
    public float SuspensionTravelFrontLeft { get; set; }
    public float SuspensionTravelFrontRight { get; set; }
    public float SuspensionTravelRearLeft { get; set; }
    public float SuspensionTravelRearRight { get; set; }
    public float SuspensionTravelMetersFrontLeft { get; set; }
    public float SuspensionTravelMetersFrontRight { get; set; }
    public float SuspensionTravelMetersRearLeft { get; set; }
    public float SuspensionTravelMetersRearRight { get; set; }
    public float BestLapSeconds { get; set; }
    public float LastLapSeconds { get; set; }
    public float CurrentLapSeconds { get; set; }
    public float CurrentRaceTimeSeconds { get; set; }
    public ushort LapNumber { get; set; }
    public byte RacePosition { get; set; }

    public double PeakCombinedSlip =>
        Math.Max(
            Math.Max(Math.Abs(TireCombinedSlipFrontLeft), Math.Abs(TireCombinedSlipFrontRight)),
            Math.Max(Math.Abs(TireCombinedSlipRearLeft), Math.Abs(TireCombinedSlipRearRight)));

    public double RearCombinedSlip =>
        Math.Max(Math.Abs(TireCombinedSlipRearLeft), Math.Abs(TireCombinedSlipRearRight));

    public static TelemetrySample FromPacket(ForzaDataOutPacket packet, long sequence, long receivedAtMs)
    {
        return new TelemetrySample
        {
            Sequence = sequence,
            ReceivedAtMs = receivedAtMs,
            GameTimestampMs = packet.TimestampMs,
            RaceOn = packet.IsRaceOn > 0,
            CarOrdinal = packet.CarOrdinal,
            CarClassId = packet.CarClass,
            CarClassName = ForzaDataOutPacket.ClassName(packet.CarClass),
            PerformanceIndex = packet.CarPerformanceIndex,
            DrivetrainId = packet.DrivetrainType,
            DrivetrainName = ForzaDataOutPacket.DrivetrainName(packet.DrivetrainType),
            X = packet.PositionX,
            Y = packet.PositionY,
            Z = packet.PositionZ,
            SpeedMps = packet.Speed,
            CurrentRpm = packet.CurrentEngineRpm,
            EngineMaxRpm = packet.EngineMaxRpm,
            EngineIdleRpm = packet.EngineIdleRpm,
            PowerW = packet.Power,
            TorqueNm = packet.Torque,
            Boost = packet.Boost,
            Fuel = packet.Fuel,
            DistanceTraveled = packet.DistanceTraveled,
            Throttle = packet.Accel,
            Brake = packet.Brake,
            Clutch = packet.Clutch,
            Handbrake = packet.HandBrake,
            Gear = packet.Gear,
            Steer = packet.Steer,
            NormalizedDrivingLine = packet.NormalizedDrivingLine,
            NormalizedAiBrakeDifference = packet.NormalizedAiBrakeDifference,
            AccelerationX = packet.AccelerationX,
            AccelerationY = packet.AccelerationY,
            AccelerationZ = packet.AccelerationZ,
            VelocityX = packet.VelocityX,
            VelocityY = packet.VelocityY,
            VelocityZ = packet.VelocityZ,
            AngularVelocityX = packet.AngularVelocityX,
            AngularVelocityY = packet.AngularVelocityY,
            AngularVelocityZ = packet.AngularVelocityZ,
            Yaw = packet.Yaw,
            Pitch = packet.Pitch,
            Roll = packet.Roll,
            TireTempFrontLeft = packet.TireTempFrontLeft,
            TireTempFrontRight = packet.TireTempFrontRight,
            TireTempRearLeft = packet.TireTempRearLeft,
            TireTempRearRight = packet.TireTempRearRight,
            TireSlipRatioFrontLeft = packet.TireSlipRatioFrontLeft,
            TireSlipRatioFrontRight = packet.TireSlipRatioFrontRight,
            TireSlipRatioRearLeft = packet.TireSlipRatioRearLeft,
            TireSlipRatioRearRight = packet.TireSlipRatioRearRight,
            TireSlipAngleFrontLeft = packet.TireSlipAngleFrontLeft,
            TireSlipAngleFrontRight = packet.TireSlipAngleFrontRight,
            TireSlipAngleRearLeft = packet.TireSlipAngleRearLeft,
            TireSlipAngleRearRight = packet.TireSlipAngleRearRight,
            TireCombinedSlipFrontLeft = packet.TireCombinedSlipFrontLeft,
            TireCombinedSlipFrontRight = packet.TireCombinedSlipFrontRight,
            TireCombinedSlipRearLeft = packet.TireCombinedSlipRearLeft,
            TireCombinedSlipRearRight = packet.TireCombinedSlipRearRight,
            SuspensionTravelFrontLeft = packet.NormalizedSuspensionTravelFrontLeft,
            SuspensionTravelFrontRight = packet.NormalizedSuspensionTravelFrontRight,
            SuspensionTravelRearLeft = packet.NormalizedSuspensionTravelRearLeft,
            SuspensionTravelRearRight = packet.NormalizedSuspensionTravelRearRight,
            SuspensionTravelMetersFrontLeft = packet.SuspensionTravelMetersFrontLeft,
            SuspensionTravelMetersFrontRight = packet.SuspensionTravelMetersFrontRight,
            SuspensionTravelMetersRearLeft = packet.SuspensionTravelMetersRearLeft,
            SuspensionTravelMetersRearRight = packet.SuspensionTravelMetersRearRight,
            BestLapSeconds = packet.BestLap,
            LastLapSeconds = packet.LastLap,
            CurrentLapSeconds = packet.CurrentLap,
            CurrentRaceTimeSeconds = packet.CurrentRaceTime,
            LapNumber = packet.LapNumber,
            RacePosition = packet.RacePosition
        };
    }
}

public sealed class TelemetryLap
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SessionId { get; set; } = "";
    public string Mode { get; set; } = "TimeAttack";
    public int LapNumber { get; set; }
    public string TrackName { get; set; } = "";
    public long StartedAtMs { get; set; }
    public long EndedAtMs { get; set; }
    public int LapTimeMs { get; set; }
    public string BoundaryConfidence { get; set; } = "unknown";
    public bool Approximate { get; set; }
    public string TuneId { get; set; } = "";
    public string TuneName { get; set; } = "";
    public string CarName { get; set; } = "";
    public int CarOrdinal { get; set; }
    public string CarClassName { get; set; } = "";
    public int PerformanceIndex { get; set; }
    public string DrivetrainName { get; set; } = "";
}

public sealed class TelemetrySessionSummary
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Status { get; set; } = "";
    public long StartedAtMs { get; set; }
    public long? EndedAtMs { get; set; }
    public string TuneId { get; set; } = "";
    public string TuneName { get; set; } = "";
    public string CarName { get; set; } = "";
    public int CarOrdinal { get; set; }
    public string CarClassName { get; set; } = "";
    public int PerformanceIndex { get; set; }
    public string DrivetrainName { get; set; } = "";
    public int SampleCount { get; set; }
    public int LapCount { get; set; }
    public int? BestLapMs { get; set; }
}

public sealed class TelemetryTuneComparison
{
    public string TuneId { get; set; } = "";
    public string TuneName { get; set; } = "";
    public string CarName { get; set; } = "";
    public string CarClassName { get; set; } = "";
    public int PerformanceIndex { get; set; }
    public int LapCount { get; set; }
    public int BestLapMs { get; set; }
    public double AverageLapMs { get; set; }
    public long LastRunAtMs { get; set; }
}

public sealed class TelemetryIssueMarker
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SessionId { get; set; } = "";
    public string? LapId { get; set; }
    public long SampleSequence { get; set; }
    public long CreatedAtMs { get; set; }
    public string IssueType { get; set; } = "";
    public string Severity { get; set; } = "info";
    public string Message { get; set; } = "";
}

public sealed class TelemetrySnapshot
{
    public bool IsRunning { get; set; }
    public string Status { get; set; } = "";
    public string? SessionId { get; set; }
    public string BindHost { get; set; } = "127.0.0.1";
    public int BindPort { get; set; } = 5400;
    public TelemetrySessionContext Context { get; set; } = new();
    public TelemetrySample? CurrentSample { get; set; }
    public IReadOnlyList<TelemetrySample> RecentSamples { get; set; } = Array.Empty<TelemetrySample>();
    public IReadOnlyList<TelemetryLap> RecentLaps { get; set; } = Array.Empty<TelemetryLap>();
    public IReadOnlyList<TelemetryIssueMarker> RecentIssues { get; set; } = Array.Empty<TelemetryIssueMarker>();
}
