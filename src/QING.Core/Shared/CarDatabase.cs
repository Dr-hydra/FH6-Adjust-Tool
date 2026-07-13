using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace QING.Core;

public class Car
{
    public int id { get; set; }
    public string make { get; set; } = "";
    public string model { get; set; } = "";
    public string year { get; set; } = "";
    public string drive { get; set; } = "RWD";
    public string cls { get; set; } = "D";
    public double weight { get; set; }
    public double weightDist { get; set; }
    public bool ev { get; set; }
    public int pi { get; set; }
    public int numGears { get; set; }
    public double redlineRpm { get; set; }
    public double peakTorqueRpm { get; set; }
    public double maxTorqueNm { get; set; }
    public double topSpeedKmh { get; set; }
    public string frontTire { get; set; } = "";
    public string rearTire { get; set; } = "";
    public double frontAeroClampKg { get; set; }
    public double rearAeroClampKg { get; set; }
    public double drag { get; set; }

    // Retained for saved-tune compatibility. The FH6 vehicle table stores the
    // stock gear count, but not a reliable per-gear ratio list in Data_Car.
    public double? fd { get; set; }
    public List<double>? gears { get; set; }
}

public static class CarDatabase
{
    private const string RepositoryContentsUrl =
        "https://api.github.com/repos/Dr-hydra/FH6-Adjust-Tool/contents/";
    private const string GameDatabaseRelativePath = "src/FH6AdjustTool/Data/fh6_game_db.sqlite";
    private const string VehicleDatabaseRelativePath = "src/FH6AdjustTool/Data/vehicle-database.json";

    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FH6AdjustTool");
    private static readonly string CachedGameDatabasePath = Path.Combine(AppDataFolder, "fh6_game_db.sqlite");
    private static readonly string CachedVehicleDatabasePath = Path.Combine(AppDataFolder, "vehicle-database.json");
    private static readonly string SourceStatePath = Path.Combine(AppDataFolder, "game_database_sources.json");

    private static int _sqliteInitialized;
    private static string _seedDirectory = "";
    private static string _activeGameDatabasePath = "";
    private static string _activeVehicleDatabasePath = "";
    private static Dictionary<int, Car> _carsById = new();
    private static bool _allowLocalParserDiscovery;

    public static List<Car> CarsList { get; private set; } = new();
    public static int CurrentVersion { get; private set; }
    public static string LastUpdated { get; private set; } = "";
    public static string GameDatabasePath => _activeGameDatabasePath;
    public static string VehicleDatabasePath => _activeVehicleDatabasePath;

    public static void Initialize(string seedDirectory)
    {
        EnsureSqliteInitialized();
        Directory.CreateDirectory(AppDataFolder);
        _seedDirectory = Path.GetFullPath(seedDirectory);
        _allowLocalParserDiscovery = !IsTemporaryPath(_seedDirectory);

        var cachedDb = IsValidGameDatabase(CachedGameDatabasePath)
            ? CachedGameDatabasePath
            : "";
        var cachedVehicles = IsValidVehicleDatabase(CachedVehicleDatabasePath)
            ? CachedVehicleDatabasePath
            : "";

        _activeGameDatabasePath = FirstValidPath(
            cachedDb,
            Path.Combine(seedDirectory, "fh6_game_db.sqlite"));
        _activeVehicleDatabasePath = FirstValidPath(
            cachedVehicles,
            Path.Combine(seedDirectory, "vehicle-database.json"));

        Reload();
    }

    public static Car? FindById(int carId)
    {
        return _carsById.TryGetValue(carId, out var car) ? car : null;
    }

    public static async Task<bool> FetchUpdatesAsync()
    {
        try
        {
            var localParserData = FindLocalParserData();
            if (localParserData != null)
            {
                return SyncLocalParserData(localParserData.Value.GameDatabase, localParserData.Value.VehicleDatabase);
            }

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FH6AdjustTool/2.0");

            var state = LoadSourceState();
            var dbSource = await GetRepositoryFileAsync(client, GameDatabaseRelativePath).ConfigureAwait(false);
            var vehicleSource = await GetRepositoryFileAsync(client, VehicleDatabaseRelativePath).ConfigureAwait(false);
            if (dbSource == null || vehicleSource == null)
            {
                return false;
            }

            var needsDb = !string.Equals(state.GameDatabaseSha, dbSource.Sha, StringComparison.OrdinalIgnoreCase) ||
                          !IsValidGameDatabase(CachedGameDatabasePath);
            var needsVehicles = !string.Equals(state.VehicleDatabaseSha, vehicleSource.Sha, StringComparison.OrdinalIgnoreCase) ||
                                !IsValidVehicleDatabase(CachedVehicleDatabasePath);
            if (!needsDb && !needsVehicles)
            {
                return false;
            }

            var dbTemp = CachedGameDatabasePath + ".download";
            var vehiclesTemp = CachedVehicleDatabasePath + ".download";
            if (needsDb)
            {
                await DownloadFileAsync(client, dbSource.DownloadUrl, dbTemp).ConfigureAwait(false);
                if (!IsValidGameDatabase(dbTemp))
                {
                    TryDelete(dbTemp);
                    return false;
                }
            }

            if (needsVehicles)
            {
                await DownloadFileAsync(client, vehicleSource.DownloadUrl, vehiclesTemp).ConfigureAwait(false);
                if (!IsValidVehicleDatabase(vehiclesTemp))
                {
                    TryDelete(vehiclesTemp);
                    return false;
                }
            }

            if (needsDb)
            {
                ReplaceFile(dbTemp, CachedGameDatabasePath);
            }
            if (needsVehicles)
            {
                ReplaceFile(vehiclesTemp, CachedVehicleDatabasePath);
            }

            SaveSourceState(new SourceState
            {
                GameDatabaseSha = dbSource.Sha,
                VehicleDatabaseSha = vehicleSource.Sha
            });
            _activeGameDatabasePath = CachedGameDatabasePath;
            _activeVehicleDatabasePath = CachedVehicleDatabasePath;
            Reload();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to update FH6 game database: {ex.Message}");
            return false;
        }
    }

    private static (string GameDatabase, string VehicleDatabase)? FindLocalParserData()
    {
        if (!_allowLocalParserDiscovery)
        {
            return null;
        }

        var current = new DirectoryInfo(_seedDirectory);
        while (current != null)
        {
            var parserRoot = Path.Combine(current.FullName, "fh6-file-analyse");
            var database = Path.Combine(parserRoot, "data", "fh6_game_db.sqlite");
            var vehicles = Path.Combine(parserRoot, "docs", "format-tables", "vehicle-database.json");
            if (IsValidGameDatabase(database) && IsValidVehicleDatabase(vehicles))
            {
                return (database, vehicles);
            }
            current = current.Parent;
        }
        return null;
    }

    private static bool IsTemporaryPath(string path)
    {
        var roots = new[]
        {
            Path.GetTempPath(),
            Environment.GetEnvironmentVariable("TEMP"),
            Environment.GetEnvironmentVariable("TMP")
        };
        return roots
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(root => Path.GetFullPath(root!))
            .Any(root => IsPathWithin(path, root));
    }

    private static bool IsPathWithin(string path, string root)
    {
        var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SyncLocalParserData(string sourceDatabase, string sourceVehicles)
    {
        var databaseChanged = !FilesMatch(sourceDatabase, _activeGameDatabasePath);
        var vehiclesChanged = !FilesMatch(sourceVehicles, _activeVehicleDatabasePath);
        if (!databaseChanged && !vehiclesChanged)
        {
            return false;
        }

        if (databaseChanged)
        {
            var temp = CachedGameDatabasePath + ".local";
            File.Copy(sourceDatabase, temp, true);
            if (!IsValidGameDatabase(temp))
            {
                TryDelete(temp);
                return false;
            }
            ReplaceFile(temp, CachedGameDatabasePath);
            _activeGameDatabasePath = CachedGameDatabasePath;
        }

        if (vehiclesChanged)
        {
            var temp = CachedVehicleDatabasePath + ".local";
            File.Copy(sourceVehicles, temp, true);
            if (!IsValidVehicleDatabase(temp))
            {
                TryDelete(temp);
                return false;
            }
            ReplaceFile(temp, CachedVehicleDatabasePath);
            _activeVehicleDatabasePath = CachedVehicleDatabasePath;
        }

        Reload();
        return true;
    }

    private static bool FilesMatch(string first, string second)
    {
        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second) ||
            !File.Exists(first) || !File.Exists(second))
        {
            return false;
        }
        var firstInfo = new FileInfo(first);
        var secondInfo = new FileInfo(second);
        if (firstInfo.Length != secondInfo.Length)
        {
            return false;
        }
        using var algorithm = SHA256.Create();
        using var firstStream = File.OpenRead(first);
        var firstHash = algorithm.ComputeHash(firstStream);
        using var secondStream = File.OpenRead(second);
        var secondHash = algorithm.ComputeHash(secondStream);
        return firstHash.SequenceEqual(secondHash);
    }

    private static void Reload()
    {
        if (!IsValidGameDatabase(_activeGameDatabasePath) ||
            !IsValidVehicleDatabase(_activeVehicleDatabasePath))
        {
            CarsList = new List<Car>();
            _carsById = new Dictionary<int, Car>();
            CurrentVersion = 0;
            LastUpdated = "";
            return;
        }

        var names = LoadVehicleNames(_activeVehicleDatabasePath);
        var cars = new List<Car>();
        using var connection = OpenReadOnlyConnection(_activeGameDatabasePath);
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT c.Id, c.Year, c.MediaName, c.ClassID, c.CurbWeight,
                   c.WeightDistribution, c.NumGears, c.PI, c.DriveTypeID,
                   c.EngineConfigID, c.FrontTireWidthMM, c.FrontTireAspect,
                   c.FrontWheelDiameterIN, c.RearTireWidthMM, c.RearTireAspect,
                   c.RearWheelDiameterIN, c.SimRedlineAngVel,
                   c.SimPeakTorqueAngVel, c.SimPeakTorque, c.SimTopSpeed,
                   c.FrontDownforceClampKG, c.RearDownforceClampKG,
                   c.BodyAeroLongitudinalDrag, transmission.FinalDriveRatio,
                   transmission.GearRatio1, transmission.GearRatio2,
                   transmission.GearRatio3, transmission.GearRatio4,
                   transmission.GearRatio5, transmission.GearRatio6,
                   transmission.GearRatio7, transmission.GearRatio8,
                   transmission.GearRatio9, transmission.GearRatio10
            FROM Drivable_Data_Car c
            INNER JOIN List_UpgradeDrivetrain drivetrain
                    ON drivetrain.Ordinal = c.Id AND drivetrain.IsStock = 1
            INNER JOIN List_UpgradeDrivetrainTransmission transmission
                    ON transmission.DrivetrainID = drivetrain.DrivetrainID
                   AND transmission.IsStock = 1
            ORDER BY c.Year, c.Id";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetInt32(0);
            names.TryGetValue(id, out var name);
            var numberOfGears = ReadInt(reader, 6);
            cars.Add(new Car
            {
                id = id,
                year = (name?.Year ?? reader.GetInt32(1)).ToString(CultureInfo.InvariantCulture),
                make = name?.Make ?? "FH6",
                model = name?.Model ?? ReadString(reader, 2),
                drive = MapDrive(ReadInt(reader, 8)),
                cls = MapClass(ReadInt(reader, 3)),
                weight = Math.Round(ReadDouble(reader, 4) * 100.0, 0),
                weightDist = Math.Round(ReadDouble(reader, 5) * 100.0, 1),
                numGears = numberOfGears,
                pi = ReadInt(reader, 7),
                ev = ReadInt(reader, 9) == 6,
                frontTire = FormatTire(reader, 10, 11, 12),
                rearTire = FormatTire(reader, 13, 14, 15),
                redlineRpm = AngularVelocityToRpm(ReadDouble(reader, 16)),
                peakTorqueRpm = AngularVelocityToRpm(ReadDouble(reader, 17)),
                maxTorqueNm = Math.Round(ReadDouble(reader, 18) * 100.0, 1),
                topSpeedKmh = Math.Round(ReadDouble(reader, 19) * 3.6, 1),
                frontAeroClampKg = ReadDouble(reader, 20),
                rearAeroClampKg = ReadDouble(reader, 21),
                drag = ReadDouble(reader, 22),
                fd = ReadDouble(reader, 23),
                gears = ReadGearRatios(reader, 24, numberOfGears)
            });
        }
        reader.Close();

        var carsById = cars.ToDictionary(car => car.id);
        using var fallbackCommand = connection.CreateCommand();
        fallbackCommand.CommandText = @"
            SELECT c.Id, c.Year, c.MediaName, c.ClassID, c.CurbWeight,
                   c.WeightDistribution, c.NumGears, c.PI, c.DriveTypeID,
                   c.EngineConfigID, c.FrontTireWidthMM, c.FrontTireAspect,
                   c.FrontWheelDiameterIN, c.RearTireWidthMM, c.RearTireAspect,
                   c.RearWheelDiameterIN, c.SimRedlineAngVel,
                   c.SimPeakTorqueAngVel, c.SimPeakTorque, c.SimTopSpeed,
                   c.FrontDownforceClampKG, c.RearDownforceClampKG,
                   c.BodyAeroLongitudinalDrag
            FROM Data_Car c
            ORDER BY c.Id";
        using var fallbackReader = fallbackCommand.ExecuteReader();
        while (fallbackReader.Read())
        {
            var id = fallbackReader.GetInt32(0);
            if (carsById.ContainsKey(id))
            {
                continue;
            }

            names.TryGetValue(id, out var name);
            carsById[id] = new Car
            {
                id = id,
                year = (name?.Year ?? fallbackReader.GetInt32(1)).ToString(CultureInfo.InvariantCulture),
                make = name?.Make ?? "FH6",
                model = name?.Model ?? ReadString(fallbackReader, 2),
                drive = MapDrive(ReadInt(fallbackReader, 8)),
                cls = MapClass(ReadInt(fallbackReader, 3)),
                weight = Math.Round(ReadDouble(fallbackReader, 4) * 100.0, 0),
                weightDist = Math.Round(ReadDouble(fallbackReader, 5) * 100.0, 1),
                numGears = ReadInt(fallbackReader, 6),
                pi = ReadInt(fallbackReader, 7),
                ev = ReadInt(fallbackReader, 9) == 6,
                frontTire = FormatTire(fallbackReader, 10, 11, 12),
                rearTire = FormatTire(fallbackReader, 13, 14, 15),
                redlineRpm = AngularVelocityToRpm(ReadDouble(fallbackReader, 16)),
                peakTorqueRpm = AngularVelocityToRpm(ReadDouble(fallbackReader, 17)),
                maxTorqueNm = Math.Round(ReadDouble(fallbackReader, 18) * 100.0, 1),
                topSpeedKmh = Math.Round(ReadDouble(fallbackReader, 19) * 3.6, 1),
                frontAeroClampKg = ReadDouble(fallbackReader, 20),
                rearAeroClampKg = ReadDouble(fallbackReader, 21),
                drag = ReadDouble(fallbackReader, 22)
            };
        }
        fallbackReader.Close();
        _carsById = carsById;

        CarsList = cars
            .OrderBy(car => car.make, StringComparer.OrdinalIgnoreCase)
            .ThenBy(car => car.model, StringComparer.OrdinalIgnoreCase)
            .ThenBy(car => car.year, StringComparer.OrdinalIgnoreCase)
            .ToList();

        using var versionCommand = connection.CreateCommand();
        versionCommand.CommandText = "SELECT * FROM VersionInfo LIMIT 1";
        using var versionReader = versionCommand.ExecuteReader();
        if (versionReader.Read())
        {
            CurrentVersion = 1;
            LastUpdated = string.Join(" · ", Enumerable.Range(0, versionReader.FieldCount)
                .Select(index => Convert.ToString(versionReader.GetValue(index), CultureInfo.InvariantCulture))
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        }
    }

    internal static SqliteConnection OpenGameDatabase()
    {
        if (!IsValidGameDatabase(_activeGameDatabasePath))
        {
            throw new InvalidOperationException("FH6 game database is not available.");
        }
        return OpenReadOnlyConnection(_activeGameDatabasePath);
    }

    private static SqliteConnection OpenReadOnlyConnection(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        connection.Open();
        return connection;
    }

    private static Dictionary<int, VehicleName> LoadVehicleNames(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var result = new Dictionary<int, VehicleName>();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!int.TryParse(property.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                continue;
            }
            var value = property.Value;
            result[id] = new VehicleName
            {
                Year = value.TryGetProperty("year", out var year) && year.TryGetInt32(out var parsedYear) ? parsedYear : 0,
                Make = value.TryGetProperty("make", out var make) ? make.GetString() ?? "" : "",
                Model = value.TryGetProperty("model", out var model) ? model.GetString() ?? "" : ""
            };
        }
        return result;
    }

    private static async Task<RepositoryFile?> GetRepositoryFileAsync(HttpClient client, string relativePath)
    {
        var json = await client.GetStringAsync(RepositoryContentsUrl + relativePath).ConfigureAwait(false);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var sha = root.TryGetProperty("sha", out var shaElement) ? shaElement.GetString() : null;
        var downloadUrl = root.TryGetProperty("download_url", out var urlElement) ? urlElement.GetString() : null;
        return string.IsNullOrWhiteSpace(sha) || string.IsNullOrWhiteSpace(downloadUrl)
            ? null
            : new RepositoryFile(sha!, downloadUrl!);
    }

    private static async Task DownloadFileAsync(HttpClient client, string url, string destination)
    {
        TryDelete(destination);
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var target = File.Create(destination);
        await source.CopyToAsync(target).ConfigureAwait(false);
    }

    private static bool IsValidGameDatabase(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }
        try
        {
            using var connection = OpenReadOnlyConnection(path);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Data_Car";
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 500;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidVehicleDatabase(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                   document.RootElement.EnumerateObject().Take(501).Count() > 500;
        }
        catch
        {
            return false;
        }
    }

    private static string FirstValidPath(params string[] paths)
    {
        return paths.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path)) ?? "";
    }

    private static void ReplaceFile(string source, string destination)
    {
        TryDelete(destination);
        File.Move(source, destination);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
        }
    }

    private static SourceState LoadSourceState()
    {
        try
        {
            return File.Exists(SourceStatePath)
                ? JsonSerializer.Deserialize<SourceState>(File.ReadAllText(SourceStatePath)) ?? new SourceState()
                : new SourceState();
        }
        catch
        {
            return new SourceState();
        }
    }

    private static void SaveSourceState(SourceState state)
    {
        File.WriteAllText(SourceStatePath, JsonSerializer.Serialize(state));
    }

    private static string MapDrive(int value) => value switch
    {
        1 => "FWD",
        3 => "AWD",
        _ => "RWD"
    };

    private static string MapClass(int value) => value switch
    {
        0 => "D",
        1 => "C",
        2 => "B",
        3 => "A",
        4 => "S1",
        5 => "S2",
        6 => "R",
        7 => "X",
        _ => "D"
    };

    private static string FormatTire(SqliteDataReader reader, int widthIndex, int aspectIndex, int diameterIndex)
    {
        var width = ReadInt(reader, widthIndex);
        var aspect = ReadInt(reader, aspectIndex);
        var diameter = ReadInt(reader, diameterIndex);
        return width > 0 && aspect > 0 && diameter > 0 ? $"{width}/{aspect}R{diameter}" : "";
    }

    private static double AngularVelocityToRpm(double radiansPerSecond)
    {
        return radiansPerSecond > 0 ? Math.Round(radiansPerSecond * 60.0 / (2.0 * Math.PI), 0) : 0;
    }

    private static List<double> ReadGearRatios(SqliteDataReader reader, int firstIndex, int count)
    {
        var result = new List<double>();
        var safeCount = Math.Max(0, Math.Min(10, count));
        for (var index = 0; index < safeCount; index++)
        {
            var ratio = ReadDouble(reader, firstIndex + index);
            if (ratio > 0)
            {
                result.Add(ratio);
            }
        }
        return result;
    }

    private static string ReadString(SqliteDataReader reader, int index) =>
        reader.IsDBNull(index) ? "" : reader.GetString(index);

    private static int ReadInt(SqliteDataReader reader, int index) =>
        reader.IsDBNull(index) ? 0 : Convert.ToInt32(reader.GetValue(index), CultureInfo.InvariantCulture);

    private static double ReadDouble(SqliteDataReader reader, int index) =>
        reader.IsDBNull(index) ? 0 : Convert.ToDouble(reader.GetValue(index), CultureInfo.InvariantCulture);

    private static void EnsureSqliteInitialized()
    {
        if (Interlocked.Exchange(ref _sqliteInitialized, 1) == 0)
        {
            Batteries_V2.Init();
        }
    }

    private sealed class VehicleName
    {
        public int Year { get; set; }
        public string Make { get; set; } = "";
        public string Model { get; set; } = "";
    }

    private sealed class SourceState
    {
        public string GameDatabaseSha { get; set; } = "";
        public string VehicleDatabaseSha { get; set; } = "";
    }

    private sealed class RepositoryFile
    {
        public RepositoryFile(string sha, string downloadUrl)
        {
            Sha = sha;
            DownloadUrl = downloadUrl;
        }

        public string Sha { get; }
        public string DownloadUrl { get; }
    }
}
