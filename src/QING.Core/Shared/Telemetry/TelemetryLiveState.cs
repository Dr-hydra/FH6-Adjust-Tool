using System;
using System.Collections.Generic;
using System.Linq;

namespace QING.Core.Telemetry;

public sealed class TelemetryLiveState
{
    private readonly object _lock = new();
    private readonly int _sampleLimit;
    private readonly int _lapLimit;
    private readonly int _issueLimit;
    private readonly Queue<TelemetrySample> _samples = new();
    private readonly Queue<TelemetryLap> _laps = new();
    private readonly Queue<TelemetryIssueMarker> _issues = new();
    private TelemetrySample? _currentSample;

    public TelemetryLiveState(int sampleLimit = 900, int lapLimit = 80, int issueLimit = 120)
    {
        _sampleLimit = Math.Max(30, sampleLimit);
        _lapLimit = Math.Max(10, lapLimit);
        _issueLimit = Math.Max(20, issueLimit);
    }

    public TelemetrySample? CurrentSample
    {
        get
        {
            lock (_lock)
            {
                return _currentSample;
            }
        }
    }

    public void AddSample(TelemetrySample sample)
    {
        lock (_lock)
        {
            _currentSample = sample;
            _samples.Enqueue(sample);
            while (_samples.Count > _sampleLimit)
            {
                _samples.Dequeue();
            }
        }
    }

    public void AddLap(TelemetryLap lap)
    {
        lock (_lock)
        {
            _laps.Enqueue(lap);
            while (_laps.Count > _lapLimit)
            {
                _laps.Dequeue();
            }
        }
    }

    public void AddIssue(TelemetryIssueMarker marker)
    {
        lock (_lock)
        {
            _issues.Enqueue(marker);
            while (_issues.Count > _issueLimit)
            {
                _issues.Dequeue();
            }
        }
    }

    public IReadOnlyList<TelemetrySample> RecentSamples(int limit)
    {
        lock (_lock)
        {
            return _samples.Reverse().Take(Math.Max(1, limit)).Reverse().ToList();
        }
    }

    public IReadOnlyList<TelemetryLap> RecentLaps(int limit)
    {
        lock (_lock)
        {
            return _laps.Reverse().Take(Math.Max(1, limit)).Reverse().ToList();
        }
    }

    public IReadOnlyList<TelemetryIssueMarker> RecentIssues(int limit)
    {
        lock (_lock)
        {
            return _issues.Reverse().Take(Math.Max(1, limit)).Reverse().ToList();
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _samples.Clear();
            _laps.Clear();
            _issues.Clear();
            _currentSample = null;
        }
    }
}
