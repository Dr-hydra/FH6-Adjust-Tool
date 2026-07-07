using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace QING.Core.Telemetry;

public sealed class TelemetrySampleEventArgs : EventArgs
{
    public TelemetrySampleEventArgs(TelemetrySample sample)
    {
        Sample = sample;
    }

    public TelemetrySample Sample { get; }
}

public sealed class TelemetryLapEventArgs : EventArgs
{
    public TelemetryLapEventArgs(TelemetryLap lap)
    {
        Lap = lap;
    }

    public TelemetryLap Lap { get; }
}

public sealed class TelemetryStatusEventArgs : EventArgs
{
    public TelemetryStatusEventArgs(string status)
    {
        Status = status;
    }

    public string Status { get; }
}

public sealed class TelemetryIssueEventArgs : EventArgs
{
    public TelemetryIssueEventArgs(TelemetryIssueMarker marker)
    {
        Marker = marker;
    }

    public TelemetryIssueMarker Marker { get; }
}

public sealed class TelemetryService : IDisposable
{
    private readonly object _lock = new();
    private readonly TimeAttackLapEngine _timeAttack = new();
    private readonly RivalsTracker _rivals = new();
    private readonly Dictionary<string, long> _lastIssueAtMs = new(StringComparer.OrdinalIgnoreCase);
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private long _sequence;
    private bool _disposed;
    private int _sessionUpdateCountdown;

    public TelemetryService(TelemetryStore store)
    {
        Store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public event EventHandler<TelemetrySampleEventArgs>? SampleReceived;
    public event EventHandler<TelemetryLapEventArgs>? LapCompleted;
    public event EventHandler<TelemetryIssueEventArgs>? IssueMarked;
    public event EventHandler<TelemetryStatusEventArgs>? StatusChanged;

    public TelemetryStore Store { get; }
    public TelemetryLiveState LiveState { get; } = new();
    public bool IsRunning { get; private set; }
    public string BindHost { get; private set; } = "127.0.0.1";
    public int BindPort { get; private set; } = 5400;
    public string Status { get; private set; } = "未启动";
    public string? CurrentSessionId { get; private set; }
    public TelemetrySessionContext Context { get; private set; } = new();

    public Task StartAsync(string bindHost, int bindPort, TelemetrySessionContext? context)
    {
        ThrowIfDisposed();
        if (bindPort <= 0 || bindPort > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(bindPort), "UDP port must be between 1 and 65535.");
        }

        lock (_lock)
        {
            if (IsRunning)
            {
                return Task.CompletedTask;
            }

            BindHost = string.IsNullOrWhiteSpace(bindHost) ? "0.0.0.0" : bindHost.Trim();
            BindPort = bindPort;
            Context = context ?? new TelemetrySessionContext();
            _sequence = 0;
            _sessionUpdateCountdown = 0;
            _lastIssueAtMs.Clear();
            LiveState.Reset();
            _rivals.Reset();
            CurrentSessionId = Store.CreateSession(Context, NowMs());

            var endpoint = new IPEndPoint(ParseBindAddress(BindHost), BindPort);
            _udpClient = new UdpClient(endpoint);
            _cts = new CancellationTokenSource();
            IsRunning = true;
            SetStatus($"正在监听 {BindHost}:{BindPort}");
            _listenTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(string reason = "manual_stop")
    {
        CancellationTokenSource? cts;
        UdpClient? client;
        Task? listenTask;
        string? sessionId;

        lock (_lock)
        {
            if (!IsRunning)
            {
                return;
            }

            cts = _cts;
            client = _udpClient;
            listenTask = _listenTask;
            sessionId = CurrentSessionId;
            IsRunning = false;
            _cts = null;
            _udpClient = null;
            _listenTask = null;
        }

        try
        {
            cts?.Cancel();
            client?.Close();
            if (listenTask != null)
            {
                await Task.WhenAny(listenTask, Task.Delay(1000)).ConfigureAwait(false);
            }
        }
        finally
        {
            client?.Dispose();
            cts?.Dispose();
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                Store.EndSession(sessionId!, NowMs(), reason);
            }

            SetStatus("已停止");
        }
    }

    public void SetContext(TelemetrySessionContext context)
    {
        Context = context ?? new TelemetrySessionContext();
    }

    public bool MarkGate(string? trackName = null)
    {
        var sample = LiveState.CurrentSample;
        if (sample == null)
        {
            return false;
        }

        var ok = _timeAttack.SetManualGate(sample, trackName);
        if (ok)
        {
            SetStatus($"已标记起终点：{(string.IsNullOrWhiteSpace(trackName) ? "手动路线" : trackName)}");
        }

        return ok;
    }

    public TelemetrySnapshot GetSnapshot(int sampleLimit = 600)
    {
        return new TelemetrySnapshot
        {
            IsRunning = IsRunning,
            Status = Status,
            SessionId = CurrentSessionId,
            BindHost = BindHost,
            BindPort = BindPort,
            Context = Context,
            CurrentSample = LiveState.CurrentSample,
            RecentSamples = LiveState.RecentSamples(sampleLimit),
            RecentLaps = LiveState.RecentLaps(80),
            RecentIssues = LiveState.RecentIssues(120)
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopAsync("disposed").GetAwaiter().GetResult();
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try
            {
                var client = _udpClient;
                if (client == null)
                {
                    break;
                }

                result = await client.ReceiveAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException ex) when (cancellationToken.IsCancellationRequested || ex.SocketErrorCode == SocketError.Interrupted)
            {
                break;
            }
            catch (Exception ex)
            {
                SetStatus("监听失败：" + ex.Message);
                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                ProcessPacket(result.Buffer);
            }
            catch (Exception ex)
            {
                SetStatus("遥测处理失败：" + ex.Message);
            }
        }
    }

    private void ProcessPacket(byte[] raw)
    {
        if (!ForzaDataOutPacket.TryParse(raw, raw.Length, out var packet) || packet == null)
        {
            return;
        }

        var sequence = Interlocked.Increment(ref _sequence);
        var sample = TelemetrySample.FromPacket(packet, sequence, NowMs());
        LiveState.AddSample(sample);

        var sessionId = CurrentSessionId;
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            sessionId = Store.CreateSession(Context, sample.ReceivedAtMs);
            CurrentSessionId = sessionId;
        }

        if (_sessionUpdateCountdown <= 0)
        {
            Store.UpdateSessionFromSample(sessionId!, Context, sample);
            _sessionUpdateCountdown = 30;
        }
        else
        {
            _sessionUpdateCountdown--;
        }

        Store.InsertSample(sessionId!, sample, raw);

        var rivalsLap = _rivals.Update(sample);
        if (rivalsLap != null)
        {
            CommitLap(ToTelemetryLap(rivalsLap, sessionId!, sample));
        }

        var timeAttackLap = _timeAttack.Update(sample);
        if (timeAttackLap != null)
        {
            CommitLap(ToTelemetryLap(timeAttackLap, sessionId!, sample));
        }

        DetectIssues(sessionId!, sample);
        SampleReceived?.Invoke(this, new TelemetrySampleEventArgs(sample));
    }

    private void CommitLap(TelemetryLap lap)
    {
        Store.InsertLap(lap);
        LiveState.AddLap(lap);
        LapCompleted?.Invoke(this, new TelemetryLapEventArgs(lap));
    }

    private TelemetryLap ToTelemetryLap(TimeAttackLapEvent e, string sessionId, TelemetrySample sample)
    {
        return FillLapContext(new TelemetryLap
        {
            SessionId = sessionId,
            Mode = "TimeAttack",
            LapNumber = e.LapNumber,
            TrackName = e.TrackName,
            StartedAtMs = e.StartedAtMs,
            EndedAtMs = e.EndedAtMs,
            LapTimeMs = e.LapTimeMs,
            BoundaryConfidence = e.BoundaryConfidence,
            Approximate = e.Approximate
        }, sample);
    }

    private TelemetryLap ToTelemetryLap(RivalsLapEvent e, string sessionId, TelemetrySample sample)
    {
        return FillLapContext(new TelemetryLap
        {
            SessionId = sessionId,
            Mode = "Rivals",
            LapNumber = e.LapNumber,
            TrackName = "Rivals",
            StartedAtMs = Math.Max(0, e.EndedAtMs - e.LapTimeMs),
            EndedAtMs = e.EndedAtMs,
            LapTimeMs = e.LapTimeMs,
            BoundaryConfidence = "game_timer",
            Approximate = false
        }, sample);
    }

    private TelemetryLap FillLapContext(TelemetryLap lap, TelemetrySample sample)
    {
        lap.TuneId = Context.TuneId ?? "";
        lap.TuneName = Context.TuneName ?? "";
        lap.CarName = string.IsNullOrWhiteSpace(Context.CarName) ? "" : Context.CarName;
        lap.CarOrdinal = sample.CarOrdinal;
        lap.CarClassName = sample.CarClassName;
        lap.PerformanceIndex = sample.PerformanceIndex;
        lap.DrivetrainName = sample.DrivetrainName;
        return lap;
    }

    private void DetectIssues(string sessionId, TelemetrySample sample)
    {
        if (!sample.RaceOn || sample.SpeedMps < 8)
        {
            return;
        }

        if (sample.PeakCombinedSlip > 1.35 && (sample.Throttle > 150 || sample.Brake > 120))
        {
            MarkIssue(sessionId, sample, "grip_limit", "warn", "轮胎综合滑移偏高，当前调校可能在弯中或出弯超过抓地极限。");
        }

        var maxSuspension = Math.Max(
            Math.Max(sample.SuspensionTravelFrontLeft, sample.SuspensionTravelFrontRight),
            Math.Max(sample.SuspensionTravelRearLeft, sample.SuspensionTravelRearRight));
        if (maxSuspension > 0.96)
        {
            MarkIssue(sessionId, sample, "bottoming", "warn", "悬挂接近压缩到底，建议检查弹簧硬度、车高或阻尼。");
        }

        var brakeSlip = Math.Max(
            Math.Max(Math.Abs(sample.TireSlipRatioFrontLeft), Math.Abs(sample.TireSlipRatioFrontRight)),
            Math.Max(Math.Abs(sample.TireSlipRatioRearLeft), Math.Abs(sample.TireSlipRatioRearRight)));
        if (sample.Brake > 210 && brakeSlip > 1.0)
        {
            MarkIssue(sessionId, sample, "brake_lock", "warn", "重刹时轮胎滑移明显，可能需要调整刹车压力或刹车平衡。");
        }
    }

    private void MarkIssue(string sessionId, TelemetrySample sample, string issueType, string severity, string message)
    {
        var now = sample.ReceivedAtMs;
        if (_lastIssueAtMs.TryGetValue(issueType, out var last) && now - last < 3500)
        {
            return;
        }

        _lastIssueAtMs[issueType] = now;
        var marker = new TelemetryIssueMarker
        {
            SessionId = sessionId,
            SampleSequence = sample.Sequence,
            CreatedAtMs = now,
            IssueType = issueType,
            Severity = severity,
            Message = message
        };
        Store.InsertIssueMarker(marker);
        LiveState.AddIssue(marker);
        IssueMarked?.Invoke(this, new TelemetryIssueEventArgs(marker));
    }

    private void SetStatus(string status)
    {
        Status = status;
        StatusChanged?.Invoke(this, new TelemetryStatusEventArgs(status));
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TelemetryService));
        }
    }

    private static IPAddress ParseBindAddress(string host)
    {
        if (string.IsNullOrWhiteSpace(host) || host == "*" || host == "0.0.0.0")
        {
            return IPAddress.Any;
        }

        if (IPAddress.TryParse(host, out var address))
        {
            return address;
        }

        return IPAddress.Any;
    }

    public static long NowMs()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}

public static class TelemetryRuntime
{
    private static readonly Lazy<TelemetryStore> StoreLazy = new(() => new TelemetryStore(DefaultDatabasePath));
    private static readonly Lazy<TelemetryService> ServiceLazy = new(() => new TelemetryService(StoreLazy.Value));

    public static TelemetryStore Store => StoreLazy.Value;
    public static TelemetryService Service => ServiceLazy.Value;

    public static string DefaultDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FH6AdjustTool");

    public static string DefaultDatabasePath => Path.Combine(DefaultDataFolder, "telemetry.db");
}
