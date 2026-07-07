using System;
using System.Collections.Generic;
using System.Linq;

namespace QING.Core.Telemetry;

internal readonly struct Vec2
{
    public Vec2(double x, double z)
    {
        X = x;
        Z = z;
    }

    public double X { get; }
    public double Z { get; }

    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Z + b.Z);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Z - b.Z);
    public static Vec2 operator *(Vec2 a, double k) => new(a.X * k, a.Z * k);

    public double Dot(Vec2 other) => X * other.X + Z * other.Z;
    public double Length() => Math.Sqrt(X * X + Z * Z);
    public double DistanceTo(Vec2 other) => (this - other).Length();

    public Vec2 Normalize()
    {
        var length = Length();
        return length <= double.Epsilon ? new Vec2(0, 0) : new Vec2(X / length, Z / length);
    }

    public Vec2 Perpendicular() => new(-Z, X);

    public static Vec2 Lerp(Vec2 a, Vec2 b, double f) => new(a.X + (b.X - a.X) * f, a.Z + (b.Z - a.Z) * f);
}

internal sealed class LineGate
{
    public LineGate(Vec2 center, Vec2 heading, double halfWidth = 2.5)
    {
        var normalized = heading.Normalize();
        if (normalized.Length() <= double.Epsilon)
        {
            throw new ArgumentException("Gate heading cannot be zero.", nameof(heading));
        }

        Center = center;
        Heading = normalized;
        Perpendicular = normalized.Perpendicular();
        HalfWidth = halfWidth;
    }

    public Vec2 Center { get; }
    public Vec2 Heading { get; }
    public Vec2 Perpendicular { get; }
    public double HalfWidth { get; }

    public GateCrossing? Cross(Vec2 p0, double t0, Vec2 p1, double t1)
    {
        var d0 = (p0 - Center).Dot(Heading);
        var d1 = (p1 - Center).Dot(Heading);
        if (!(d0 <= 0.0 && d1 > 0.0))
        {
            return null;
        }

        var denom = d1 - d0;
        if (Math.Abs(denom) <= double.Epsilon)
        {
            return null;
        }

        var fraction = -d0 / denom;
        var point = Vec2.Lerp(p0, p1, fraction);
        var lateral = (point - Center).Dot(Perpendicular);
        if (Math.Abs(lateral) > HalfWidth)
        {
            return null;
        }

        return new GateCrossing
        {
            Fraction = fraction,
            TimeSeconds = t0 + (t1 - t0) * fraction,
            Point = point,
            LateralMeters = lateral
        };
    }
}

internal sealed class GateCrossing
{
    public double Fraction { get; set; }
    public double TimeSeconds { get; set; }
    public Vec2 Point { get; set; }
    public double LateralMeters { get; set; }
}

internal sealed class TimeAttackLapEngine
{
    private sealed class HistoryPoint
    {
        public double TimeSeconds { get; set; }
        public Vec2 Position { get; set; }
        public double PathDistance { get; set; }
    }

    private readonly Queue<HistoryPoint> _history = new();
    private LineGate? _gate;
    private Vec2? _previousPosition;
    private Vec2? _lastMotionHeading;
    private double? _previousTimeSeconds;
    private double _pathDistance;
    private double? _lapStartSeconds;
    private int _lapNumber;
    private int _autoTrackNumber;

    public double SampleDistanceMeters { get; set; } = 5.0;
    public double MatchRadiusMeters { get; set; } = 40.0;
    public double MinLapSeconds { get; set; } = 15.0;
    public double MinLapDistanceMeters { get; set; } = 500.0;
    public double MaxLapSeconds { get; set; } = 240.0;
    public int HistoryLimit { get; set; } = 4000;
    public string? TrackName { get; private set; }
    public bool HasGate => _gate != null;

    public bool SetManualGate(TelemetrySample sample, string? trackName = null)
    {
        var current = new Vec2(sample.X, sample.Z);
        var heading = ResolveHeading(current, sample);
        if (heading.Length() <= double.Epsilon)
        {
            return false;
        }

        _gate = new LineGate(current, heading);
        TrackName = string.IsNullOrWhiteSpace(trackName) ? "手动路线" : trackName!.Trim();
        ClearLap();
        return true;
    }

    public TimeAttackLapEvent? Update(TelemetrySample sample)
    {
        if (!sample.RaceOn)
        {
            ResetRun();
            return null;
        }

        var current = new Vec2(sample.X, sample.Z);
        var timeSeconds = sample.ReceivedAtMs / 1000.0;

        var step = _previousPosition.HasValue ? current.DistanceTo(_previousPosition.Value) : 0.0;
        if (_previousPosition.HasValue && step > 0.05)
        {
            _lastMotionHeading = (current - _previousPosition.Value).Normalize();
        }
        _pathDistance += step;

        if (_history.Count == 0 || _pathDistance - _history.Last().PathDistance >= SampleDistanceMeters)
        {
            _history.Enqueue(new HistoryPoint
            {
                TimeSeconds = timeSeconds,
                Position = current,
                PathDistance = _pathDistance
            });
            while (_history.Count > HistoryLimit)
            {
                _history.Dequeue();
            }
        }

        if (_lapStartSeconds.HasValue && timeSeconds - _lapStartSeconds.Value > MaxLapSeconds)
        {
            ClearLap();
        }

        TimeAttackLapEvent? result = null;
        if (_gate != null)
        {
            result = UpdateWithGate(sample, current, timeSeconds);
        }
        else
        {
            result = UpdateSearching(sample, current, timeSeconds);
        }

        _previousPosition = current;
        _previousTimeSeconds = timeSeconds;
        return result;
    }

    private TimeAttackLapEvent? UpdateWithGate(TelemetrySample sample, Vec2 current, double timeSeconds)
    {
        if (_gate == null || !_previousPosition.HasValue || !_previousTimeSeconds.HasValue)
        {
            return null;
        }

        var crossing = _gate.Cross(_previousPosition.Value, _previousTimeSeconds.Value, current, timeSeconds);
        if (crossing == null)
        {
            return null;
        }

        if (!_lapStartSeconds.HasValue)
        {
            _lapStartSeconds = crossing.TimeSeconds;
            return null;
        }

        var lapSeconds = crossing.TimeSeconds - _lapStartSeconds.Value;
        if (lapSeconds < MinLapSeconds)
        {
            return null;
        }

        _lapNumber++;
        var startedAtMs = ToMs(_lapStartSeconds.Value);
        _lapStartSeconds = crossing.TimeSeconds;
        return new TimeAttackLapEvent
        {
            LapNumber = _lapNumber,
            TrackName = TrackName ?? "路线",
            StartedAtMs = startedAtMs,
            EndedAtMs = ToMs(crossing.TimeSeconds),
            LapTimeMs = Math.Max(1, (int)Math.Round(lapSeconds * 1000.0)),
            BoundaryConfidence = "line_gate",
            Approximate = false,
            Sample = sample
        };
    }

    private TimeAttackLapEvent? UpdateSearching(TelemetrySample sample, Vec2 current, double timeSeconds)
    {
        foreach (var point in _history)
        {
            if (timeSeconds - point.TimeSeconds < MinLapSeconds)
            {
                continue;
            }

            if (_pathDistance - point.PathDistance < MinLapDistanceMeters)
            {
                continue;
            }

            if (current.DistanceTo(point.Position) > MatchRadiusMeters)
            {
                continue;
            }

            var heading = ResolveHeading(current, sample);
            if (heading.Length() <= double.Epsilon)
            {
                continue;
            }

            _autoTrackNumber++;
            _gate = new LineGate(point.Position, heading);
            TrackName = $"自动路线 {_autoTrackNumber}";
            ClearLap();
            var lapSeconds = timeSeconds - point.TimeSeconds;
            return new TimeAttackLapEvent
            {
                LapNumber = 0,
                TrackName = TrackName,
                StartedAtMs = ToMs(point.TimeSeconds),
                EndedAtMs = ToMs(timeSeconds),
                LapTimeMs = Math.Max(1, (int)Math.Round(lapSeconds * 1000.0)),
                BoundaryConfidence = "loop_closure",
                Approximate = true,
                Sample = sample
            };
        }

        return null;
    }

    private Vec2 ResolveHeading(Vec2 current, TelemetrySample sample)
    {
        if (_previousPosition.HasValue)
        {
            var heading = (current - _previousPosition.Value).Normalize();
            if (heading.Length() > double.Epsilon)
            {
                return heading;
            }
        }

        if (_lastMotionHeading.HasValue && _lastMotionHeading.Value.Length() > double.Epsilon)
        {
            return _lastMotionHeading.Value;
        }

        var yawHeading = new Vec2(Math.Sin(sample.Yaw), Math.Cos(sample.Yaw)).Normalize();
        return yawHeading;
    }

    private void ResetRun()
    {
        _previousPosition = null;
        _lastMotionHeading = null;
        _previousTimeSeconds = null;
        _history.Clear();
        _pathDistance = 0.0;
        ClearLap();
    }

    private void ClearLap()
    {
        _lapStartSeconds = null;
    }

    private static long ToMs(double seconds)
    {
        return (long)Math.Round(seconds * 1000.0);
    }
}

internal sealed class TimeAttackLapEvent
{
    public int LapNumber { get; set; }
    public string TrackName { get; set; } = "";
    public long StartedAtMs { get; set; }
    public long EndedAtMs { get; set; }
    public int LapTimeMs { get; set; }
    public string BoundaryConfidence { get; set; } = "";
    public bool Approximate { get; set; }
    public TelemetrySample Sample { get; set; } = new();
}

internal sealed class RivalsTracker
{
    private double? _previousLastLapSeconds;
    private int _rivalsLapNumber;

    public RivalsLapEvent? Update(TelemetrySample sample)
    {
        var active = sample.LapNumber > 0 ||
                     sample.BestLapSeconds > 0 ||
                     sample.CurrentLapSeconds > 0 ||
                     sample.LastLapSeconds > 0;

        if (!active)
        {
            _previousLastLapSeconds = null;
            return null;
        }

        var last = sample.LastLapSeconds;
        RivalsLapEvent? result = null;
        if (_previousLastLapSeconds.HasValue && last > 0 && Math.Abs(last - _previousLastLapSeconds.Value) > 0.000001)
        {
            _rivalsLapNumber++;
            result = new RivalsLapEvent
            {
                LapNumber = sample.LapNumber > 0 ? Math.Max(0, sample.LapNumber - 1) : _rivalsLapNumber,
                LapTimeMs = Math.Max(1, (int)Math.Round(last * 1000.0)),
                EndedAtMs = sample.ReceivedAtMs,
                Sample = sample
            };
        }

        _previousLastLapSeconds = last;
        return result;
    }

    public void Reset()
    {
        _previousLastLapSeconds = null;
        _rivalsLapNumber = 0;
    }
}

internal sealed class RivalsLapEvent
{
    public int LapNumber { get; set; }
    public int LapTimeMs { get; set; }
    public long EndedAtMs { get; set; }
    public TelemetrySample Sample { get; set; } = new();
}
