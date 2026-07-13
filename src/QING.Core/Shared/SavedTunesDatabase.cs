using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace QING.Core;

public class SavedTune
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public DateTime SavedAt { get; set; } = DateTime.Now;
    public string CarSearchKeyword { get; set; } = "";
    public string SelectedCarText { get; set; } = "";
    public TuningState State { get; set; } = new();
    public TuningResult Result { get; set; } = new();
    public string SourceFingerprint { get; set; } = "";
    public string SourceCarId { get; set; } = "";
    public string SourceFolderName { get; set; } = "";
    public string SourceAuthor { get; set; } = "";
    public double EstimatedWeightKg { get; set; }
    public double StockWeightKg { get; set; }
    public bool PerformanceRatingIsStockFallback { get; set; }
    public string SpecificationNote { get; set; } = "";
    public string ModeRecommendationReason { get; set; } = "";
    public List<InstalledPartSummary> InstalledParts { get; set; } = new();
}

public class SavedTuneSummary
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime SavedAt { get; set; } = DateTime.Now;
    public string CarSearchKeyword { get; set; } = "";
    public string SelectedCarText { get; set; } = "";
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public string CarClass { get; set; } = "";
    public int Pi { get; set; }
    public string SourceFingerprint { get; set; } = "";
}

public enum ImportedTuneSaveResult
{
    Added,
    Updated
}

public static class SavedTunesDatabase
{
    private static readonly object Lock = new();
    private static int _sqliteInitialized;

    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FH6AdjustTool"
    );
    private static readonly string DatabasePath = Path.Combine(AppDataFolder, "saved_tunes.db");
    private static readonly string LegacySavedTunesFilePath = Path.Combine(AppDataFolder, "saved_tunes.json");
    private static readonly string LegacyTuneFolder = Path.Combine(AppDataFolder, "saved_tunes");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly string ConnectionString =
        new SqliteConnectionStringBuilder { DataSource = DatabasePath }.ToString();

    private static List<SavedTune> _tunes = new();
    private static List<SavedTuneSummary> _summaries = new();
    private static bool _fullLoaded;

    public static List<SavedTune> Tunes
    {
        get
        {
            EnsureFullLoaded();
            return _tunes;
        }
    }

    public static IReadOnlyList<SavedTuneSummary> Summaries => _summaries;

    public static void Initialize()
    {
        Load();
    }

    public static void Load()
    {
        lock (Lock)
        {
            try
            {
                EnsureSqliteInitialized();
                Migrate();
                MigrateLegacySourcesIfNeeded();
                _summaries = QuerySummaries();
                _tunes = new List<SavedTune>();
                _fullLoaded = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading saved tunes: {ex.Message}");
                _summaries = new List<SavedTuneSummary>();
                _tunes = new List<SavedTune>();
                _fullLoaded = false;
            }
        }
    }

    public static void Save()
    {
        lock (Lock)
        {
            EnsureFullLoaded();
            foreach (var tune in _tunes)
            {
                UpsertTune(tune);
            }

            _summaries = QuerySummaries();
        }
    }

    public static SavedTune? GetTune(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        lock (Lock)
        {
            var cached = _tunes.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
            if (cached != null)
            {
                return cached;
            }

            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT payload_json FROM saved_tunes WHERE id = $id;";
            Add(command, "$id", id);
            var payload = command.ExecuteScalar() as string;
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            var tune = JsonSerializer.Deserialize<SavedTune>(payload!);
            if (tune == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(tune.Id))
            {
                tune.Id = id;
            }

            _tunes.Add(tune);
            return tune;
        }
    }

    public static SavedTune? FindByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        lock (Lock)
        {
            var summary = _summaries.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (summary != null)
            {
                return GetTune(summary.Id);
            }

            return null;
        }
    }

    public static bool NameExists(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        lock (Lock)
        {
            return _summaries.Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }

    public static void SaveTune(SavedTune tune)
    {
        lock (Lock)
        {
            if (string.IsNullOrWhiteSpace(tune.Id))
            {
                tune.Id = Guid.NewGuid().ToString();
            }

            var existing = _summaries.FirstOrDefault(t =>
                t.Id == tune.Id || t.Name.Equals(tune.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                tune.Id = existing.Id;
            }

            tune.SavedAt = DateTime.Now;
            UpsertTune(tune);
            UpsertCache(tune);
            _summaries = QuerySummaries();
        }
    }

    public static ImportedTuneSaveResult SaveImportedTune(SavedTune tune)
    {
        lock (Lock)
        {
            var existing = string.IsNullOrWhiteSpace(tune.SourceFingerprint)
                ? null
                : _summaries.FirstOrDefault(summary =>
                    string.Equals(summary.SourceFingerprint, tune.SourceFingerprint, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                tune.Id = existing.Id;
                tune.Name = existing.Name;
                SaveTune(tune);
                return ImportedTuneSaveResult.Updated;
            }

            SaveTune(tune);
            return ImportedTuneSaveResult.Added;
        }
    }

    public static void DeleteTune(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        lock (Lock)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM saved_tunes WHERE id = $id;";
            Add(command, "$id", id);
            command.ExecuteNonQuery();

            _tunes.RemoveAll(t => t.Id == id);
            _summaries.RemoveAll(t => t.Id == id);
        }
    }

    private static void Migrate()
    {
        Directory.CreateDirectory(AppDataFolder);
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            PRAGMA journal_mode=WAL;
            PRAGMA synchronous=NORMAL;
            CREATE TABLE IF NOT EXISTS saved_tunes (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL COLLATE NOCASE,
                saved_at_ticks INTEGER NOT NULL,
                car_search_keyword TEXT,
                selected_car_text TEXT,
                make TEXT,
                model TEXT,
                car_class TEXT,
                pi INTEGER,
                source_fingerprint TEXT,
                payload_json TEXT NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS idx_saved_tunes_name ON saved_tunes(name COLLATE NOCASE);
            CREATE INDEX IF NOT EXISTS idx_saved_tunes_vehicle ON saved_tunes(make, model, selected_car_text);
            CREATE INDEX IF NOT EXISTS idx_saved_tunes_saved_at ON saved_tunes(saved_at_ticks DESC);
            """;
        command.ExecuteNonQuery();

        EnsureColumn(connection, "saved_tunes", "source_fingerprint", "TEXT");
        using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText =
            "CREATE UNIQUE INDEX IF NOT EXISTS idx_saved_tunes_source_fingerprint " +
            "ON saved_tunes(source_fingerprint) WHERE source_fingerprint IS NOT NULL AND source_fingerprint <> '';";
        indexCommand.ExecuteNonQuery();
    }

    private static void MigrateLegacySourcesIfNeeded()
    {
        if (HasRows())
        {
            return;
        }

        var migrated = new List<SavedTune>();
        if (File.Exists(LegacySavedTunesFilePath))
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<SavedTune>>(File.ReadAllText(LegacySavedTunesFilePath));
                if (list != null)
                {
                    migrated.AddRange(list);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error migrating saved_tunes.json: {ex.Message}");
            }
        }

        if (Directory.Exists(LegacyTuneFolder))
        {
            foreach (var file in Directory.EnumerateFiles(LegacyTuneFolder, "*.json", SearchOption.TopDirectoryOnly)
                         .Where(path => !Path.GetFileName(path).Equals("index.json", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    var tune = JsonSerializer.Deserialize<SavedTune>(File.ReadAllText(file));
                    if (tune != null)
                    {
                        migrated.Add(tune);
                    }
                }
                catch
                {
                }
            }
        }

        foreach (var tune in migrated)
        {
            if (string.IsNullOrWhiteSpace(tune.Id))
            {
                tune.Id = Guid.NewGuid().ToString();
            }

            UpsertTune(tune);
        }
    }

    private static bool HasRows()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM saved_tunes LIMIT 1);";
        return Convert.ToInt32(command.ExecuteScalar()) != 0;
    }

    private static void EnsureFullLoaded()
    {
        if (_fullLoaded)
        {
            return;
        }

        var loaded = new List<SavedTune>();
        foreach (var summary in _summaries)
        {
            var tune = GetTune(summary.Id);
            if (tune != null)
            {
                loaded.Add(tune);
            }
        }

        _tunes = loaded;
        _fullLoaded = true;
    }

    private static void UpsertTune(SavedTune tune)
    {
        var summary = CreateSummary(tune);
        var payload = JsonSerializer.Serialize(tune, JsonOptions);

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO saved_tunes (
                id, name, saved_at_ticks, car_search_keyword, selected_car_text,
                make, model, car_class, pi, source_fingerprint, payload_json
            ) VALUES (
                $id, $name, $saved_at_ticks, $car_search_keyword, $selected_car_text,
                $make, $model, $car_class, $pi, $source_fingerprint, $payload_json
            )
            ON CONFLICT(id) DO UPDATE SET
                name = excluded.name,
                saved_at_ticks = excluded.saved_at_ticks,
                car_search_keyword = excluded.car_search_keyword,
                selected_car_text = excluded.selected_car_text,
                make = excluded.make,
                model = excluded.model,
                car_class = excluded.car_class,
                pi = excluded.pi,
                source_fingerprint = excluded.source_fingerprint,
                payload_json = excluded.payload_json;
            """;
        AddSummaryParameters(command, summary);
        Add(command, "$payload_json", payload);
        command.ExecuteNonQuery();
    }

    private static List<SavedTuneSummary> QuerySummaries()
    {
        var result = new List<SavedTuneSummary>();
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, name, saved_at_ticks, car_search_keyword, selected_car_text,
                   make, model, car_class, pi, source_fingerprint
            FROM saved_tunes
            ORDER BY saved_at_ticks DESC, name COLLATE NOCASE;
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(ReadSummary(reader));
        }

        return result;
    }

    private static SavedTuneSummary ReadSummary(SqliteDataReader reader)
    {
        return new SavedTuneSummary
        {
            Id = Text(reader, "id"),
            Name = Text(reader, "name"),
            SavedAt = new DateTime(Long(reader, "saved_at_ticks"), DateTimeKind.Local),
            CarSearchKeyword = Text(reader, "car_search_keyword"),
            SelectedCarText = Text(reader, "selected_car_text"),
            Make = Text(reader, "make"),
            Model = Text(reader, "model"),
            CarClass = Text(reader, "car_class"),
            Pi = Int(reader, "pi"),
            SourceFingerprint = Text(reader, "source_fingerprint")
        };
    }

    private static SavedTuneSummary CreateSummary(SavedTune tune)
    {
        return new SavedTuneSummary
        {
            Id = tune.Id,
            Name = tune.Name,
            SavedAt = tune.SavedAt,
            CarSearchKeyword = tune.CarSearchKeyword,
            SelectedCarText = tune.SelectedCarText,
            Make = tune.State?.Make ?? "",
            Model = tune.State?.Model ?? "",
            CarClass = tune.State?.CarClass ?? "",
            Pi = tune.State?.Pi ?? 0,
            SourceFingerprint = tune.SourceFingerprint
        };
    }

    private static void UpsertCache(SavedTune tune)
    {
        var existing = _tunes.FirstOrDefault(t => t.Id == tune.Id);
        if (existing == null)
        {
            if (_fullLoaded || _tunes.Count > 0)
            {
                _tunes.Add(tune);
            }
            return;
        }

        var index = _tunes.IndexOf(existing);
        _tunes[index] = tune;
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout=5000;";
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

    private static void AddSummaryParameters(SqliteCommand command, SavedTuneSummary summary)
    {
        Add(command, "$id", summary.Id);
        Add(command, "$name", summary.Name);
        Add(command, "$saved_at_ticks", summary.SavedAt.Ticks);
        Add(command, "$car_search_keyword", NullIfEmpty(summary.CarSearchKeyword));
        Add(command, "$selected_car_text", NullIfEmpty(summary.SelectedCarText));
        Add(command, "$make", NullIfEmpty(summary.Make));
        Add(command, "$model", NullIfEmpty(summary.Model));
        Add(command, "$car_class", NullIfEmpty(summary.CarClass));
        Add(command, "$pi", summary.Pi);
        Add(command, "$source_fingerprint", NullIfEmpty(summary.SourceFingerprint));
    }

    private static void EnsureColumn(SqliteConnection connection, string tableName, string columnName, string columnType)
    {
        using var infoCommand = connection.CreateCommand();
        infoCommand.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = infoCommand.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(Convert.ToString(reader["name"]), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        reader.Close();
        using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType};";
        alterCommand.ExecuteNonQuery();
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static void Add(SqliteCommand command, string name, object? value)
    {
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
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

    private static long Long(SqliteDataReader reader, string column)
    {
        var value = reader[column];
        return value == DBNull.Value ? 0L : Convert.ToInt64(value);
    }
}
