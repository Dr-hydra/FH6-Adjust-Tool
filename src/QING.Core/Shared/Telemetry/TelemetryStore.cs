using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace QING.Core.Telemetry;

public sealed class TelemetryStore
{
    private readonly object _lock = new();
    private static int _sqliteInitialized;
    private readonly string _dbPath;
    private readonly string _connectionString;

    public TelemetryStore(string dbPath)
    {
        _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));
        EnsureSqliteInitialized();
        var builder = new SqliteConnectionStringBuilder { DataSource = _dbPath };
        _connectionString = builder.ToString();
        Migrate();
    }

    public string DatabasePath => _dbPath;

    public void Migrate()
    {
        lock (_lock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath) ?? ".");
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                PRAGMA journal_mode=WAL;
                PRAGMA synchronous=NORMAL;
                CREATE TABLE IF NOT EXISTS telemetry_sessions (
                    id TEXT PRIMARY KEY,
                    label TEXT NOT NULL,
                    status TEXT NOT NULL,
                    started_at_ms INTEGER NOT NULL,
                    ended_at_ms INTEGER,
                    ended_reason TEXT,
                    tune_id TEXT,
                    tune_name TEXT,
                    car_name TEXT,
                    car_ordinal INTEGER,
                    car_class_id INTEGER,
                    car_class_name TEXT,
                    performance_index INTEGER,
                    drivetrain_id INTEGER,
                    drivetrain_name TEXT,
                    sample_count INTEGER NOT NULL DEFAULT 0,
                    best_lap_ms INTEGER
                );
                CREATE TABLE IF NOT EXISTS telemetry_laps (
                    id TEXT PRIMARY KEY,
                    session_id TEXT NOT NULL,
                    lap_number INTEGER NOT NULL,
                    mode TEXT NOT NULL,
                    track_name TEXT,
                    started_at_ms INTEGER NOT NULL,
                    ended_at_ms INTEGER NOT NULL,
                    lap_time_ms INTEGER NOT NULL,
                    boundary_confidence TEXT,
                    approximate INTEGER NOT NULL DEFAULT 0,
                    tune_id TEXT,
                    tune_name TEXT,
                    car_name TEXT,
                    car_ordinal INTEGER,
                    car_class_name TEXT,
                    performance_index INTEGER,
                    drivetrain_name TEXT,
                    FOREIGN KEY(session_id) REFERENCES telemetry_sessions(id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS idx_telemetry_laps_session ON telemetry_laps(session_id, ended_at_ms);
                CREATE INDEX IF NOT EXISTS idx_telemetry_laps_tune ON telemetry_laps(tune_id, tune_name, lap_time_ms);
                CREATE TABLE IF NOT EXISTS telemetry_samples (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    session_id TEXT NOT NULL,
                    lap_id TEXT,
                    sequence INTEGER NOT NULL,
                    received_at_ms INTEGER NOT NULL,
                    game_timestamp_ms INTEGER NOT NULL,
                    race_on INTEGER NOT NULL,
                    car_ordinal INTEGER,
                    car_class_id INTEGER,
                    car_class_name TEXT,
                    performance_index INTEGER,
                    drivetrain_id INTEGER,
                    drivetrain_name TEXT,
                    x REAL NOT NULL,
                    y REAL NOT NULL,
                    z REAL NOT NULL,
                    speed_mps REAL NOT NULL,
                    current_rpm REAL,
                    engine_max_rpm REAL,
                    engine_idle_rpm REAL,
                    power_w REAL,
                    torque_nm REAL,
                    boost REAL,
                    fuel REAL,
                    distance_traveled REAL,
                    throttle INTEGER,
                    brake INTEGER,
                    clutch INTEGER,
                    handbrake INTEGER,
                    gear INTEGER,
                    steer INTEGER,
                    normalized_driving_line INTEGER,
                    normalized_ai_brake_difference INTEGER,
                    acceleration_x REAL,
                    acceleration_y REAL,
                    acceleration_z REAL,
                    velocity_x REAL,
                    velocity_y REAL,
                    velocity_z REAL,
                    angular_velocity_x REAL,
                    angular_velocity_y REAL,
                    angular_velocity_z REAL,
                    yaw REAL,
                    pitch REAL,
                    roll REAL,
                    tire_temp_front_left REAL,
                    tire_temp_front_right REAL,
                    tire_temp_rear_left REAL,
                    tire_temp_rear_right REAL,
                    tire_slip_ratio_front_left REAL,
                    tire_slip_ratio_front_right REAL,
                    tire_slip_ratio_rear_left REAL,
                    tire_slip_ratio_rear_right REAL,
                    tire_slip_angle_front_left REAL,
                    tire_slip_angle_front_right REAL,
                    tire_slip_angle_rear_left REAL,
                    tire_slip_angle_rear_right REAL,
                    tire_combined_slip_front_left REAL,
                    tire_combined_slip_front_right REAL,
                    tire_combined_slip_rear_left REAL,
                    tire_combined_slip_rear_right REAL,
                    suspension_travel_front_left REAL,
                    suspension_travel_front_right REAL,
                    suspension_travel_rear_left REAL,
                    suspension_travel_rear_right REAL,
                    suspension_travel_meters_front_left REAL,
                    suspension_travel_meters_front_right REAL,
                    suspension_travel_meters_rear_left REAL,
                    suspension_travel_meters_rear_right REAL,
                    best_lap_seconds REAL,
                    last_lap_seconds REAL,
                    current_lap_seconds REAL,
                    current_race_time_seconds REAL,
                    lap_number INTEGER,
                    race_position INTEGER,
                    FOREIGN KEY(session_id) REFERENCES telemetry_sessions(id) ON DELETE CASCADE,
                    FOREIGN KEY(lap_id) REFERENCES telemetry_laps(id) ON DELETE SET NULL
                );
                CREATE INDEX IF NOT EXISTS idx_telemetry_samples_session_sequence ON telemetry_samples(session_id, sequence);
                CREATE INDEX IF NOT EXISTS idx_telemetry_samples_lap ON telemetry_samples(lap_id, sequence);
                CREATE TABLE IF NOT EXISTS telemetry_raw_packets (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    session_id TEXT NOT NULL,
                    sequence INTEGER NOT NULL,
                    received_at_ms INTEGER NOT NULL,
                    raw_packet BLOB NOT NULL,
                    FOREIGN KEY(session_id) REFERENCES telemetry_sessions(id) ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS idx_telemetry_raw_session_sequence ON telemetry_raw_packets(session_id, sequence);
                CREATE TABLE IF NOT EXISTS telemetry_issue_markers (
                    id TEXT PRIMARY KEY,
                    session_id TEXT NOT NULL,
                    lap_id TEXT,
                    sample_sequence INTEGER NOT NULL,
                    created_at_ms INTEGER NOT NULL,
                    issue_type TEXT NOT NULL,
                    severity TEXT NOT NULL,
                    message TEXT NOT NULL,
                    FOREIGN KEY(session_id) REFERENCES telemetry_sessions(id) ON DELETE CASCADE,
                    FOREIGN KEY(lap_id) REFERENCES telemetry_laps(id) ON DELETE SET NULL
                );
                CREATE INDEX IF NOT EXISTS idx_telemetry_issues_session ON telemetry_issue_markers(session_id, created_at_ms);
                """;
            command.ExecuteNonQuery();
        }
    }

    public string CreateSession(TelemetrySessionContext context, long startedAtMs)
    {
        context ??= TelemetrySessionContext.Empty;
        var sessionId = Guid.NewGuid().ToString("N");
        var label = string.IsNullOrWhiteSpace(context.Label)
            ? BuildSessionLabel(context, startedAtMs)
            : context.Label.Trim();

        lock (_lock)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO telemetry_sessions(
                    id, label, status, started_at_ms, tune_id, tune_name, car_name
                ) VALUES (
                    $id, $label, 'recording', $started_at_ms, $tune_id, $tune_name, $car_name
                )
                """;
            Add(command, "$id", sessionId);
            Add(command, "$label", label);
            Add(command, "$started_at_ms", startedAtMs);
            Add(command, "$tune_id", NullIfEmpty(context.TuneId));
            Add(command, "$tune_name", NullIfEmpty(context.TuneName));
            Add(command, "$car_name", NullIfEmpty(context.CarName));
            command.ExecuteNonQuery();
        }

        return sessionId;
    }

    public void UpdateSessionFromSample(string sessionId, TelemetrySessionContext context, TelemetrySample sample)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || sample == null)
        {
            return;
        }

        context ??= TelemetrySessionContext.Empty;
        lock (_lock)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                UPDATE telemetry_sessions
                SET tune_id = COALESCE(NULLIF($tune_id, ''), tune_id),
                    tune_name = COALESCE(NULLIF($tune_name, ''), tune_name),
                    car_name = COALESCE(NULLIF($context_car_name, ''), NULLIF(car_name, ''), car_name),
                    car_ordinal = $car_ordinal,
                    car_class_id = $car_class_id,
                    car_class_name = $car_class_name,
                    performance_index = $performance_index,
                    drivetrain_id = $drivetrain_id,
                    drivetrain_name = $drivetrain_name
                WHERE id = $session_id
                """;
            Add(command, "$session_id", sessionId);
            Add(command, "$tune_id", context.TuneId ?? "");
            Add(command, "$tune_name", context.TuneName ?? "");
            Add(command, "$context_car_name", context.CarName ?? "");
            Add(command, "$car_ordinal", sample.CarOrdinal);
            Add(command, "$car_class_id", sample.CarClassId);
            Add(command, "$car_class_name", sample.CarClassName);
            Add(command, "$performance_index", sample.PerformanceIndex);
            Add(command, "$drivetrain_id", sample.DrivetrainId);
            Add(command, "$drivetrain_name", sample.DrivetrainName);
            command.ExecuteNonQuery();
        }
    }

    public void EndSession(string sessionId, long endedAtMs, string reason)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        lock (_lock)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                UPDATE telemetry_sessions
                SET status = 'stopped',
                    ended_at_ms = COALESCE(ended_at_ms, $ended_at_ms),
                    ended_reason = $reason
                WHERE id = $session_id
                """;
            Add(command, "$session_id", sessionId);
            Add(command, "$ended_at_ms", endedAtMs);
            Add(command, "$reason", reason ?? "");
            command.ExecuteNonQuery();
        }
    }

    public void InsertSample(string sessionId, TelemetrySample sample, byte[]? rawPacket, string? lapId = null)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || sample == null)
        {
            return;
        }

        lock (_lock)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    """
                    INSERT INTO telemetry_samples(
                        session_id, lap_id, sequence, received_at_ms, game_timestamp_ms, race_on,
                        car_ordinal, car_class_id, car_class_name, performance_index, drivetrain_id, drivetrain_name,
                        x, y, z, speed_mps, current_rpm, engine_max_rpm, engine_idle_rpm, power_w, torque_nm,
                        boost, fuel, distance_traveled, throttle, brake, clutch, handbrake, gear, steer,
                        normalized_driving_line, normalized_ai_brake_difference,
                        acceleration_x, acceleration_y, acceleration_z, velocity_x, velocity_y, velocity_z,
                        angular_velocity_x, angular_velocity_y, angular_velocity_z, yaw, pitch, roll,
                        tire_temp_front_left, tire_temp_front_right, tire_temp_rear_left, tire_temp_rear_right,
                        tire_slip_ratio_front_left, tire_slip_ratio_front_right, tire_slip_ratio_rear_left, tire_slip_ratio_rear_right,
                        tire_slip_angle_front_left, tire_slip_angle_front_right, tire_slip_angle_rear_left, tire_slip_angle_rear_right,
                        tire_combined_slip_front_left, tire_combined_slip_front_right, tire_combined_slip_rear_left, tire_combined_slip_rear_right,
                        suspension_travel_front_left, suspension_travel_front_right, suspension_travel_rear_left, suspension_travel_rear_right,
                        suspension_travel_meters_front_left, suspension_travel_meters_front_right, suspension_travel_meters_rear_left, suspension_travel_meters_rear_right,
                        best_lap_seconds, last_lap_seconds, current_lap_seconds, current_race_time_seconds, lap_number, race_position
                    ) VALUES (
                        $session_id, $lap_id, $sequence, $received_at_ms, $game_timestamp_ms, $race_on,
                        $car_ordinal, $car_class_id, $car_class_name, $performance_index, $drivetrain_id, $drivetrain_name,
                        $x, $y, $z, $speed_mps, $current_rpm, $engine_max_rpm, $engine_idle_rpm, $power_w, $torque_nm,
                        $boost, $fuel, $distance_traveled, $throttle, $brake, $clutch, $handbrake, $gear, $steer,
                        $normalized_driving_line, $normalized_ai_brake_difference,
                        $acceleration_x, $acceleration_y, $acceleration_z, $velocity_x, $velocity_y, $velocity_z,
                        $angular_velocity_x, $angular_velocity_y, $angular_velocity_z, $yaw, $pitch, $roll,
                        $tire_temp_front_left, $tire_temp_front_right, $tire_temp_rear_left, $tire_temp_rear_right,
                        $tire_slip_ratio_front_left, $tire_slip_ratio_front_right, $tire_slip_ratio_rear_left, $tire_slip_ratio_rear_right,
                        $tire_slip_angle_front_left, $tire_slip_angle_front_right, $tire_slip_angle_rear_left, $tire_slip_angle_rear_right,
                        $tire_combined_slip_front_left, $tire_combined_slip_front_right, $tire_combined_slip_rear_left, $tire_combined_slip_rear_right,
                        $suspension_travel_front_left, $suspension_travel_front_right, $suspension_travel_rear_left, $suspension_travel_rear_right,
                        $suspension_travel_meters_front_left, $suspension_travel_meters_front_right, $suspension_travel_meters_rear_left, $suspension_travel_meters_rear_right,
                        $best_lap_seconds, $last_lap_seconds, $current_lap_seconds, $current_race_time_seconds, $lap_number, $race_position
                    )
                    """;
                AddSampleParameters(command, sessionId, sample, lapId);
                command.ExecuteNonQuery();
            }

            if (rawPacket != null)
            {
                using var rawCommand = connection.CreateCommand();
                rawCommand.Transaction = transaction;
                rawCommand.CommandText =
                    """
                    INSERT INTO telemetry_raw_packets(session_id, sequence, received_at_ms, raw_packet)
                    VALUES($session_id, $sequence, $received_at_ms, $raw_packet)
                    """;
                Add(rawCommand, "$session_id", sessionId);
                Add(rawCommand, "$sequence", sample.Sequence);
                Add(rawCommand, "$received_at_ms", sample.ReceivedAtMs);
                Add(rawCommand, "$raw_packet", rawPacket);
                rawCommand.ExecuteNonQuery();
            }

            using (var countCommand = connection.CreateCommand())
            {
                countCommand.Transaction = transaction;
                countCommand.CommandText =
                    """
                    UPDATE telemetry_sessions
                    SET sample_count = sample_count + 1
                    WHERE id = $session_id
                    """;
                Add(countCommand, "$session_id", sessionId);
                countCommand.ExecuteNonQuery();
            }

            transaction.Commit();
        }
    }

    public void InsertLap(TelemetryLap lap)
    {
        if (lap == null || string.IsNullOrWhiteSpace(lap.SessionId))
        {
            return;
        }

        lock (_lock)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    """
                    INSERT INTO telemetry_laps(
                        id, session_id, lap_number, mode, track_name, started_at_ms, ended_at_ms,
                        lap_time_ms, boundary_confidence, approximate, tune_id, tune_name, car_name,
                        car_ordinal, car_class_name, performance_index, drivetrain_name
                    ) VALUES (
                        $id, $session_id, $lap_number, $mode, $track_name, $started_at_ms, $ended_at_ms,
                        $lap_time_ms, $boundary_confidence, $approximate, $tune_id, $tune_name, $car_name,
                        $car_ordinal, $car_class_name, $performance_index, $drivetrain_name
                    )
                    """;
                Add(command, "$id", lap.Id);
                Add(command, "$session_id", lap.SessionId);
                Add(command, "$lap_number", lap.LapNumber);
                Add(command, "$mode", lap.Mode);
                Add(command, "$track_name", NullIfEmpty(lap.TrackName));
                Add(command, "$started_at_ms", lap.StartedAtMs);
                Add(command, "$ended_at_ms", lap.EndedAtMs);
                Add(command, "$lap_time_ms", lap.LapTimeMs);
                Add(command, "$boundary_confidence", lap.BoundaryConfidence);
                Add(command, "$approximate", lap.Approximate ? 1 : 0);
                Add(command, "$tune_id", NullIfEmpty(lap.TuneId));
                Add(command, "$tune_name", NullIfEmpty(lap.TuneName));
                Add(command, "$car_name", NullIfEmpty(lap.CarName));
                Add(command, "$car_ordinal", lap.CarOrdinal);
                Add(command, "$car_class_name", NullIfEmpty(lap.CarClassName));
                Add(command, "$performance_index", lap.PerformanceIndex);
                Add(command, "$drivetrain_name", NullIfEmpty(lap.DrivetrainName));
                command.ExecuteNonQuery();
            }

            using (var updateCommand = connection.CreateCommand())
            {
                updateCommand.Transaction = transaction;
                updateCommand.CommandText =
                    """
                    UPDATE telemetry_sessions
                    SET best_lap_ms = CASE
                        WHEN best_lap_ms IS NULL OR $lap_time_ms < best_lap_ms THEN $lap_time_ms
                        ELSE best_lap_ms
                    END
                    WHERE id = $session_id
                    """;
                Add(updateCommand, "$session_id", lap.SessionId);
                Add(updateCommand, "$lap_time_ms", lap.LapTimeMs);
                updateCommand.ExecuteNonQuery();
            }

            transaction.Commit();
        }
    }

    public void InsertIssueMarker(TelemetryIssueMarker marker)
    {
        if (marker == null || string.IsNullOrWhiteSpace(marker.SessionId))
        {
            return;
        }

        lock (_lock)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT OR IGNORE INTO telemetry_issue_markers(
                    id, session_id, lap_id, sample_sequence, created_at_ms, issue_type, severity, message
                ) VALUES (
                    $id, $session_id, $lap_id, $sample_sequence, $created_at_ms, $issue_type, $severity, $message
                )
                """;
            Add(command, "$id", marker.Id);
            Add(command, "$session_id", marker.SessionId);
            Add(command, "$lap_id", NullIfEmpty(marker.LapId));
            Add(command, "$sample_sequence", marker.SampleSequence);
            Add(command, "$created_at_ms", marker.CreatedAtMs);
            Add(command, "$issue_type", marker.IssueType);
            Add(command, "$severity", marker.Severity);
            Add(command, "$message", marker.Message);
            command.ExecuteNonQuery();
        }
    }

    public IReadOnlyList<TelemetrySample> GetRecentSamples(string? sessionId, int limit)
    {
        limit = Math.Max(1, Math.Min(limit, 5000));
        lock (_lock)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                command.CommandText =
                    """
                    SELECT *
                    FROM telemetry_samples
                    ORDER BY received_at_ms DESC, sequence DESC
                    LIMIT $limit
                    """;
            }
            else
            {
                command.CommandText =
                    """
                    SELECT *
                    FROM telemetry_samples
                    WHERE session_id = $session_id
                    ORDER BY sequence DESC
                    LIMIT $limit
                    """;
                Add(command, "$session_id", sessionId);
            }

            Add(command, "$limit", limit);
            var samples = new List<TelemetrySample>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                samples.Add(ReadSample(reader));
            }

            samples.Reverse();
            return samples;
        }
    }

    public IReadOnlyList<TelemetryLap> GetRecentLaps(string? sessionId, int limit)
    {
        limit = Math.Max(1, Math.Min(limit, 500));
        lock (_lock)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                command.CommandText =
                    """
                    SELECT *
                    FROM telemetry_laps
                    ORDER BY ended_at_ms DESC
                    LIMIT $limit
                    """;
            }
            else
            {
                command.CommandText =
                    """
                    SELECT *
                    FROM telemetry_laps
                    WHERE session_id = $session_id
                    ORDER BY ended_at_ms DESC
                    LIMIT $limit
                    """;
                Add(command, "$session_id", sessionId);
            }

            Add(command, "$limit", limit);
            var laps = new List<TelemetryLap>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                laps.Add(ReadLap(reader));
            }

            laps.Reverse();
            return laps;
        }
    }

    public IReadOnlyList<TelemetryIssueMarker> GetRecentIssueMarkers(string? sessionId, int limit)
    {
        limit = Math.Max(1, Math.Min(limit, 500));
        lock (_lock)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                command.CommandText =
                    """
                    SELECT *
                    FROM telemetry_issue_markers
                    ORDER BY created_at_ms DESC
                    LIMIT $limit
                    """;
            }
            else
            {
                command.CommandText =
                    """
                    SELECT *
                    FROM telemetry_issue_markers
                    WHERE session_id = $session_id
                    ORDER BY created_at_ms DESC
                    LIMIT $limit
                    """;
                Add(command, "$session_id", sessionId);
            }

            Add(command, "$limit", limit);
            var markers = new List<TelemetryIssueMarker>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                markers.Add(ReadIssue(reader));
            }

            markers.Reverse();
            return markers;
        }
    }

    public IReadOnlyList<TelemetrySessionSummary> GetRecentSessions(int limit)
    {
        limit = Math.Max(1, Math.Min(limit, 200));
        lock (_lock)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT s.*,
                       (SELECT COUNT(*) FROM telemetry_laps l WHERE l.session_id = s.id) AS lap_count
                FROM telemetry_sessions s
                ORDER BY s.started_at_ms DESC
                LIMIT $limit
                """;
            Add(command, "$limit", limit);
            var sessions = new List<TelemetrySessionSummary>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                sessions.Add(ReadSession(reader));
            }

            return sessions;
        }
    }

    public IReadOnlyList<TelemetryTuneComparison> GetTuneComparisons(int limit)
    {
        limit = Math.Max(1, Math.Min(limit, 200));
        lock (_lock)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT
                    COALESCE(NULLIF(tune_id, ''), tune_name, 'draft') AS tune_id,
                    COALESCE(NULLIF(tune_name, ''), '未命名调校') AS tune_name,
                    COALESCE(NULLIF(car_name, ''), '') AS car_name,
                    COALESCE(NULLIF(car_class_name, ''), '') AS car_class_name,
                    COALESCE(performance_index, 0) AS performance_index,
                    COUNT(*) AS lap_count,
                    MIN(lap_time_ms) AS best_lap_ms,
                    AVG(lap_time_ms) AS avg_lap_ms,
                    MAX(ended_at_ms) AS last_run_at_ms
                FROM telemetry_laps
                WHERE lap_time_ms > 0
                GROUP BY COALESCE(NULLIF(tune_id, ''), tune_name, 'draft'), COALESCE(NULLIF(car_name, ''), '')
                ORDER BY best_lap_ms ASC, last_run_at_ms DESC
                LIMIT $limit
                """;
            Add(command, "$limit", limit);
            var comparisons = new List<TelemetryTuneComparison>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                comparisons.Add(new TelemetryTuneComparison
                {
                    TuneId = Text(reader, "tune_id"),
                    TuneName = Text(reader, "tune_name"),
                    CarName = Text(reader, "car_name"),
                    CarClassName = Text(reader, "car_class_name"),
                    PerformanceIndex = Int(reader, "performance_index"),
                    LapCount = Int(reader, "lap_count"),
                    BestLapMs = Int(reader, "best_lap_ms"),
                    AverageLapMs = Double(reader, "avg_lap_ms"),
                    LastRunAtMs = Long(reader, "last_run_at_ms")
                });
            }

            return comparisons;
        }
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout=5000; PRAGMA foreign_keys=ON;";
        command.ExecuteNonQuery();
        return connection;
    }

    private static void EnsureSqliteInitialized()
    {
        if (Interlocked.Exchange(ref _sqliteInitialized, 1) == 0)
        {
            Batteries_V2.Init();
        }
    }

    private static string BuildSessionLabel(TelemetrySessionContext context, long startedAtMs)
    {
        var tune = string.IsNullOrWhiteSpace(context.TuneName) ? "未命名调校" : context.TuneName.Trim();
        var time = DateTimeOffset.FromUnixTimeMilliseconds(startedAtMs).LocalDateTime.ToString("yyyy-MM-dd HH:mm");
        return $"{tune} · {time}";
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static void Add(SqliteCommand command, string name, object? value)
    {
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }

    private static void AddSampleParameters(SqliteCommand command, string sessionId, TelemetrySample s, string? lapId)
    {
        Add(command, "$session_id", sessionId);
        Add(command, "$lap_id", NullIfEmpty(lapId));
        Add(command, "$sequence", s.Sequence);
        Add(command, "$received_at_ms", s.ReceivedAtMs);
        Add(command, "$game_timestamp_ms", (long)s.GameTimestampMs);
        Add(command, "$race_on", s.RaceOn ? 1 : 0);
        Add(command, "$car_ordinal", s.CarOrdinal);
        Add(command, "$car_class_id", s.CarClassId);
        Add(command, "$car_class_name", s.CarClassName);
        Add(command, "$performance_index", s.PerformanceIndex);
        Add(command, "$drivetrain_id", s.DrivetrainId);
        Add(command, "$drivetrain_name", s.DrivetrainName);
        Add(command, "$x", s.X);
        Add(command, "$y", s.Y);
        Add(command, "$z", s.Z);
        Add(command, "$speed_mps", s.SpeedMps);
        Add(command, "$current_rpm", s.CurrentRpm);
        Add(command, "$engine_max_rpm", s.EngineMaxRpm);
        Add(command, "$engine_idle_rpm", s.EngineIdleRpm);
        Add(command, "$power_w", s.PowerW);
        Add(command, "$torque_nm", s.TorqueNm);
        Add(command, "$boost", s.Boost);
        Add(command, "$fuel", s.Fuel);
        Add(command, "$distance_traveled", s.DistanceTraveled);
        Add(command, "$throttle", s.Throttle);
        Add(command, "$brake", s.Brake);
        Add(command, "$clutch", s.Clutch);
        Add(command, "$handbrake", s.Handbrake);
        Add(command, "$gear", s.Gear);
        Add(command, "$steer", s.Steer);
        Add(command, "$normalized_driving_line", s.NormalizedDrivingLine);
        Add(command, "$normalized_ai_brake_difference", s.NormalizedAiBrakeDifference);
        Add(command, "$acceleration_x", s.AccelerationX);
        Add(command, "$acceleration_y", s.AccelerationY);
        Add(command, "$acceleration_z", s.AccelerationZ);
        Add(command, "$velocity_x", s.VelocityX);
        Add(command, "$velocity_y", s.VelocityY);
        Add(command, "$velocity_z", s.VelocityZ);
        Add(command, "$angular_velocity_x", s.AngularVelocityX);
        Add(command, "$angular_velocity_y", s.AngularVelocityY);
        Add(command, "$angular_velocity_z", s.AngularVelocityZ);
        Add(command, "$yaw", s.Yaw);
        Add(command, "$pitch", s.Pitch);
        Add(command, "$roll", s.Roll);
        Add(command, "$tire_temp_front_left", s.TireTempFrontLeft);
        Add(command, "$tire_temp_front_right", s.TireTempFrontRight);
        Add(command, "$tire_temp_rear_left", s.TireTempRearLeft);
        Add(command, "$tire_temp_rear_right", s.TireTempRearRight);
        Add(command, "$tire_slip_ratio_front_left", s.TireSlipRatioFrontLeft);
        Add(command, "$tire_slip_ratio_front_right", s.TireSlipRatioFrontRight);
        Add(command, "$tire_slip_ratio_rear_left", s.TireSlipRatioRearLeft);
        Add(command, "$tire_slip_ratio_rear_right", s.TireSlipRatioRearRight);
        Add(command, "$tire_slip_angle_front_left", s.TireSlipAngleFrontLeft);
        Add(command, "$tire_slip_angle_front_right", s.TireSlipAngleFrontRight);
        Add(command, "$tire_slip_angle_rear_left", s.TireSlipAngleRearLeft);
        Add(command, "$tire_slip_angle_rear_right", s.TireSlipAngleRearRight);
        Add(command, "$tire_combined_slip_front_left", s.TireCombinedSlipFrontLeft);
        Add(command, "$tire_combined_slip_front_right", s.TireCombinedSlipFrontRight);
        Add(command, "$tire_combined_slip_rear_left", s.TireCombinedSlipRearLeft);
        Add(command, "$tire_combined_slip_rear_right", s.TireCombinedSlipRearRight);
        Add(command, "$suspension_travel_front_left", s.SuspensionTravelFrontLeft);
        Add(command, "$suspension_travel_front_right", s.SuspensionTravelFrontRight);
        Add(command, "$suspension_travel_rear_left", s.SuspensionTravelRearLeft);
        Add(command, "$suspension_travel_rear_right", s.SuspensionTravelRearRight);
        Add(command, "$suspension_travel_meters_front_left", s.SuspensionTravelMetersFrontLeft);
        Add(command, "$suspension_travel_meters_front_right", s.SuspensionTravelMetersFrontRight);
        Add(command, "$suspension_travel_meters_rear_left", s.SuspensionTravelMetersRearLeft);
        Add(command, "$suspension_travel_meters_rear_right", s.SuspensionTravelMetersRearRight);
        Add(command, "$best_lap_seconds", s.BestLapSeconds);
        Add(command, "$last_lap_seconds", s.LastLapSeconds);
        Add(command, "$current_lap_seconds", s.CurrentLapSeconds);
        Add(command, "$current_race_time_seconds", s.CurrentRaceTimeSeconds);
        Add(command, "$lap_number", (int)s.LapNumber);
        Add(command, "$race_position", (int)s.RacePosition);
    }

    private static TelemetrySample ReadSample(SqliteDataReader reader)
    {
        return new TelemetrySample
        {
            Sequence = Long(reader, "sequence"),
            ReceivedAtMs = Long(reader, "received_at_ms"),
            GameTimestampMs = (uint)Long(reader, "game_timestamp_ms"),
            RaceOn = Int(reader, "race_on") != 0,
            CarOrdinal = Int(reader, "car_ordinal"),
            CarClassId = Int(reader, "car_class_id"),
            CarClassName = Text(reader, "car_class_name"),
            PerformanceIndex = Int(reader, "performance_index"),
            DrivetrainId = Int(reader, "drivetrain_id"),
            DrivetrainName = Text(reader, "drivetrain_name"),
            X = Float(reader, "x"),
            Y = Float(reader, "y"),
            Z = Float(reader, "z"),
            SpeedMps = Float(reader, "speed_mps"),
            CurrentRpm = Float(reader, "current_rpm"),
            EngineMaxRpm = Float(reader, "engine_max_rpm"),
            EngineIdleRpm = Float(reader, "engine_idle_rpm"),
            PowerW = Float(reader, "power_w"),
            TorqueNm = Float(reader, "torque_nm"),
            Boost = Float(reader, "boost"),
            Fuel = Float(reader, "fuel"),
            DistanceTraveled = Float(reader, "distance_traveled"),
            Throttle = Int(reader, "throttle"),
            Brake = Int(reader, "brake"),
            Clutch = Int(reader, "clutch"),
            Handbrake = Int(reader, "handbrake"),
            Gear = Int(reader, "gear"),
            Steer = Int(reader, "steer"),
            NormalizedDrivingLine = Int(reader, "normalized_driving_line"),
            NormalizedAiBrakeDifference = Int(reader, "normalized_ai_brake_difference"),
            AccelerationX = Float(reader, "acceleration_x"),
            AccelerationY = Float(reader, "acceleration_y"),
            AccelerationZ = Float(reader, "acceleration_z"),
            VelocityX = Float(reader, "velocity_x"),
            VelocityY = Float(reader, "velocity_y"),
            VelocityZ = Float(reader, "velocity_z"),
            AngularVelocityX = Float(reader, "angular_velocity_x"),
            AngularVelocityY = Float(reader, "angular_velocity_y"),
            AngularVelocityZ = Float(reader, "angular_velocity_z"),
            Yaw = Float(reader, "yaw"),
            Pitch = Float(reader, "pitch"),
            Roll = Float(reader, "roll"),
            TireTempFrontLeft = Float(reader, "tire_temp_front_left"),
            TireTempFrontRight = Float(reader, "tire_temp_front_right"),
            TireTempRearLeft = Float(reader, "tire_temp_rear_left"),
            TireTempRearRight = Float(reader, "tire_temp_rear_right"),
            TireSlipRatioFrontLeft = Float(reader, "tire_slip_ratio_front_left"),
            TireSlipRatioFrontRight = Float(reader, "tire_slip_ratio_front_right"),
            TireSlipRatioRearLeft = Float(reader, "tire_slip_ratio_rear_left"),
            TireSlipRatioRearRight = Float(reader, "tire_slip_ratio_rear_right"),
            TireSlipAngleFrontLeft = Float(reader, "tire_slip_angle_front_left"),
            TireSlipAngleFrontRight = Float(reader, "tire_slip_angle_front_right"),
            TireSlipAngleRearLeft = Float(reader, "tire_slip_angle_rear_left"),
            TireSlipAngleRearRight = Float(reader, "tire_slip_angle_rear_right"),
            TireCombinedSlipFrontLeft = Float(reader, "tire_combined_slip_front_left"),
            TireCombinedSlipFrontRight = Float(reader, "tire_combined_slip_front_right"),
            TireCombinedSlipRearLeft = Float(reader, "tire_combined_slip_rear_left"),
            TireCombinedSlipRearRight = Float(reader, "tire_combined_slip_rear_right"),
            SuspensionTravelFrontLeft = Float(reader, "suspension_travel_front_left"),
            SuspensionTravelFrontRight = Float(reader, "suspension_travel_front_right"),
            SuspensionTravelRearLeft = Float(reader, "suspension_travel_rear_left"),
            SuspensionTravelRearRight = Float(reader, "suspension_travel_rear_right"),
            SuspensionTravelMetersFrontLeft = Float(reader, "suspension_travel_meters_front_left"),
            SuspensionTravelMetersFrontRight = Float(reader, "suspension_travel_meters_front_right"),
            SuspensionTravelMetersRearLeft = Float(reader, "suspension_travel_meters_rear_left"),
            SuspensionTravelMetersRearRight = Float(reader, "suspension_travel_meters_rear_right"),
            BestLapSeconds = Float(reader, "best_lap_seconds"),
            LastLapSeconds = Float(reader, "last_lap_seconds"),
            CurrentLapSeconds = Float(reader, "current_lap_seconds"),
            CurrentRaceTimeSeconds = Float(reader, "current_race_time_seconds"),
            LapNumber = (ushort)Int(reader, "lap_number"),
            RacePosition = (byte)Int(reader, "race_position")
        };
    }

    private static TelemetryLap ReadLap(SqliteDataReader reader)
    {
        return new TelemetryLap
        {
            Id = Text(reader, "id"),
            SessionId = Text(reader, "session_id"),
            LapNumber = Int(reader, "lap_number"),
            Mode = Text(reader, "mode"),
            TrackName = Text(reader, "track_name"),
            StartedAtMs = Long(reader, "started_at_ms"),
            EndedAtMs = Long(reader, "ended_at_ms"),
            LapTimeMs = Int(reader, "lap_time_ms"),
            BoundaryConfidence = Text(reader, "boundary_confidence"),
            Approximate = Int(reader, "approximate") != 0,
            TuneId = Text(reader, "tune_id"),
            TuneName = Text(reader, "tune_name"),
            CarName = Text(reader, "car_name"),
            CarOrdinal = Int(reader, "car_ordinal"),
            CarClassName = Text(reader, "car_class_name"),
            PerformanceIndex = Int(reader, "performance_index"),
            DrivetrainName = Text(reader, "drivetrain_name")
        };
    }

    private static TelemetryIssueMarker ReadIssue(SqliteDataReader reader)
    {
        return new TelemetryIssueMarker
        {
            Id = Text(reader, "id"),
            SessionId = Text(reader, "session_id"),
            LapId = Text(reader, "lap_id"),
            SampleSequence = Long(reader, "sample_sequence"),
            CreatedAtMs = Long(reader, "created_at_ms"),
            IssueType = Text(reader, "issue_type"),
            Severity = Text(reader, "severity"),
            Message = Text(reader, "message")
        };
    }

    private static TelemetrySessionSummary ReadSession(SqliteDataReader reader)
    {
        return new TelemetrySessionSummary
        {
            Id = Text(reader, "id"),
            Label = Text(reader, "label"),
            Status = Text(reader, "status"),
            StartedAtMs = Long(reader, "started_at_ms"),
            EndedAtMs = NullableLong(reader, "ended_at_ms"),
            TuneId = Text(reader, "tune_id"),
            TuneName = Text(reader, "tune_name"),
            CarName = Text(reader, "car_name"),
            CarOrdinal = Int(reader, "car_ordinal"),
            CarClassName = Text(reader, "car_class_name"),
            PerformanceIndex = Int(reader, "performance_index"),
            DrivetrainName = Text(reader, "drivetrain_name"),
            SampleCount = Int(reader, "sample_count"),
            LapCount = Int(reader, "lap_count"),
            BestLapMs = NullableInt(reader, "best_lap_ms")
        };
    }

    private static string Text(SqliteDataReader reader, string column)
    {
        var value = reader[column];
        return value == DBNull.Value ? "" : Convert.ToString(value) ?? "";
    }

    private static int Int(SqliteDataReader reader, string column)
    {
        var value = reader[column];
        return value == DBNull.Value ? 0 : Convert.ToInt32(value);
    }

    private static int? NullableInt(SqliteDataReader reader, string column)
    {
        var value = reader[column];
        return value == DBNull.Value ? null : Convert.ToInt32(value);
    }

    private static long Long(SqliteDataReader reader, string column)
    {
        var value = reader[column];
        return value == DBNull.Value ? 0L : Convert.ToInt64(value);
    }

    private static long? NullableLong(SqliteDataReader reader, string column)
    {
        var value = reader[column];
        return value == DBNull.Value ? null : Convert.ToInt64(value);
    }

    private static float Float(SqliteDataReader reader, string column)
    {
        var value = reader[column];
        return value == DBNull.Value ? 0f : Convert.ToSingle(value);
    }

    private static double Double(SqliteDataReader reader, string column)
    {
        var value = reader[column];
        return value == DBNull.Value ? 0.0 : Convert.ToDouble(value);
    }
}
