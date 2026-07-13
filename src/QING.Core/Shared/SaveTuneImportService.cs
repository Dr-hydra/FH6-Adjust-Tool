using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace QING.Core;

public sealed class SaveTuneImportCandidate
{
    public bool IsSelected { get; set; }
    public string FolderPath { get; set; } = "";
    public string FolderName { get; set; } = "";
    public string DataPath { get; set; } = "";
    public string HeaderPath { get; set; } = "";
    public string CarId { get; set; } = "";
    public string VehicleName { get; set; } = "";
    public string TuneName { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTime SavedAt { get; set; } = DateTime.MinValue;
    public DateTime LastWriteTime { get; set; } = DateTime.MinValue;
    public int PartCount { get; set; }
    public int SliderCount { get; set; }
    public int ResolvedPartCount { get; set; }
    public int UpgradeSlotCount { get; set; }
    public int DynamicRangeCount { get; set; }
    public double EstimatedWeightKg { get; set; }
    public string DataHash { get; set; } = "";
    public string SourceFingerprint { get; set; } = "";
    public SavedTune ImportedTune { get; set; } = new();
}

public static class SaveTuneImportService
{
    private const string LegacyGameSavePath = @"C:\XboxGames\GameSave\pgs";
    private const int TuningPartCount = 104;
    private const int TuningUpgradeSlotCount = 50;
    private const int TuningSliderCount = 45;
    private const int TuningDataSize = 598;
    private const int TuningSlidersOffset = 0x1A0;

    private static readonly Regex TuningFolderRegex = new(
        @"^Tuning_(?<carId>\d+)_(?<timestamp>\d{14})$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string ResolveDefaultSavePath()
    {
        foreach (var path in GetDefaultSavePathCandidates())
        {
            try
            {
                if (Directory.Exists(path))
                {
                    return Path.GetFullPath(path);
                }
            }
            catch
            {
            }
        }

        return LegacyGameSavePath;
    }

    public static IReadOnlyList<SaveTuneImportCandidate> Scan(string? configuredPath, int limit = 300)
    {
        var root = string.IsNullOrWhiteSpace(configuredPath)
            ? ResolveDefaultSavePath()
            : Path.GetFullPath(configuredPath!);

        if (!Directory.Exists(root))
        {
            return Array.Empty<SaveTuneImportCandidate>();
        }

        var candidates = new List<SaveTuneImportCandidate>();
        foreach (var containerRoot in FindContainerRoots(root).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var tuningFolder in SafeEnumerateDirectories(containerRoot, "Tuning_*"))
            {
                var candidate = TryParseTuningFolder(tuningFolder);
                if (candidate != null)
                {
                    candidates.Add(candidate);
                }
            }
        }

        return candidates
            .GroupBy(item => item.SourceFingerprint, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(item => item.SavedAt == DateTime.MinValue ? item.LastWriteTime : item.SavedAt)
            .ThenBy(item => item.TuneName, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, limit))
            .ToList();
    }

    private static IEnumerable<string> GetDefaultSavePathCandidates()
    {
        yield return LegacyGameSavePath;

        foreach (var drive in DriveInfo.GetDrives())
        {
            string root;
            try
            {
                root = drive.RootDirectory.FullName;
            }
            catch
            {
                continue;
            }

            yield return Path.Combine(root, "XboxGames", "GameSave", "pgs");
        }
    }

    private static IEnumerable<string> FindContainerRoots(string root)
    {
        if (LooksLikeContainerRoot(root))
        {
            yield return root;
            yield break;
        }

        var direct = Path.Combine(root, "ContainersRoot");
        if (LooksLikeContainerRoot(direct))
        {
            yield return direct;
        }

        foreach (var item in FindDirectoriesByName(root, "ContainersRoot", 5))
        {
            if (LooksLikeContainerRoot(item))
            {
                yield return item;
            }
        }
    }

    private static bool LooksLikeContainerRoot(string path)
    {
        try
        {
            return Directory.Exists(path) &&
                   SafeEnumerateDirectories(path, "Tuning_*").Any();
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> FindDirectoriesByName(string root, string name, int maxDepth)
    {
        if (maxDepth < 0 || !Directory.Exists(root))
        {
            yield break;
        }

        foreach (var child in SafeEnumerateDirectories(root, "*"))
        {
            if (string.Equals(Path.GetFileName(child), name, StringComparison.OrdinalIgnoreCase))
            {
                yield return child;
            }

            foreach (var nested in FindDirectoriesByName(child, name, maxDepth - 1))
            {
                yield return nested;
            }
        }
    }

    private static SaveTuneImportCandidate? TryParseTuningFolder(string folder)
    {
        try
        {
            var folderName = Path.GetFileName(folder);
            var match = TuningFolderRegex.Match(folderName);
            if (!match.Success)
            {
                return null;
            }

            var dataPath = Path.Combine(folder, "Data");
            if (!File.Exists(dataPath))
            {
                return null;
            }

            var data = File.ReadAllBytes(dataPath);
            if (data.Length != TuningDataSize)
            {
                return null;
            }

            var carId = match.Groups["carId"].Value;
            var timestamp = match.Groups["timestamp"].Value;
            var headerPath = Path.Combine(folder, "header");
            var header = File.Exists(headerPath) ? ParseHeader(headerPath) : new SaveHeaderInfo();
            var sliders = DecodeSliders(data);
            var partialGear10 = DecodePartialGear10(data);
            var hasNumericCarId = int.TryParse(carId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericCarId);
            var databaseCar = hasNumericCarId ? CarDatabase.FindById(numericCarId) : null;
            var databaseAnalysis = hasNumericCarId
                ? FH6TuneDatabase.Analyze(numericCarId, data)
                : new TuneDatabaseAnalysis();
            var vehicle = SaveVehicleDatabase.Lookup(carId);
            var lastWrite = new[] { folder, dataPath, headerPath }
                .Where(path => Directory.Exists(path) || File.Exists(path))
                .Select(path => Directory.Exists(path) ? Directory.GetLastWriteTime(path) : File.GetLastWriteTime(path))
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();

            var dataHash = Sha256(data);
            var candidate = new SaveTuneImportCandidate
            {
                FolderPath = folder,
                FolderName = folderName,
                DataPath = dataPath,
                HeaderPath = File.Exists(headerPath) ? headerPath : "",
                CarId = carId,
                VehicleName = vehicle?.DisplayName ??
                    (databaseCar == null ? $"未知车辆 #{carId}" : $"{databaseCar.year} {databaseCar.make} {databaseCar.model}"),
                TuneName = string.IsNullOrWhiteSpace(header.TuneName) ? $"存档调校 {timestamp}" : header.TuneName,
                Author = header.Author,
                SavedAt = ParseTimestamp(timestamp),
                LastWriteTime = lastWrite,
                PartCount = TuningPartCount,
                SliderCount = sliders.Count,
                ResolvedPartCount = databaseAnalysis.ResolvedPartCount,
                UpgradeSlotCount = TuningUpgradeSlotCount,
                DynamicRangeCount = databaseAnalysis.Ranges.Count,
                EstimatedWeightKg = databaseAnalysis.EstimatedWeightKg,
                DataHash = dataHash,
                SourceFingerprint = Sha256($"{folderName}\n{dataHash}\n{header.TuneName}\n{header.Author}")
            };
            candidate.ImportedTune = BuildSavedTune(candidate, vehicle, sliders, partialGear10, databaseAnalysis);
            return candidate;
        }
        catch
        {
            return null;
        }
    }

    private static SavedTune BuildSavedTune(
        SaveTuneImportCandidate candidate,
        SaveVehicleInfo? vehicle,
        IReadOnlyDictionary<int, float> sliders,
        string? partialGear10,
        TuneDatabaseAnalysis databaseAnalysis)
    {
        Car? car = null;
        if (int.TryParse(candidate.CarId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var carId))
        {
            car = CarDatabase.FindById(carId);
        }
        car ??= FindCar(candidate.VehicleName, vehicle);
        var gearCount = CountGears(sliders);
        if (!string.IsNullOrWhiteSpace(partialGear10))
        {
            gearCount = Math.Max(gearCount, 10);
        }
        var includeGearing = HasTunableSlider(sliders, 2) || gearCount > 0;
        var defaultGearCount = car?.numGears > 0 ? car.numGears : 0;
        var carClass = car?.cls;
        var effectiveWeight = databaseAnalysis.EstimatedWeightKg > 0
            ? databaseAnalysis.EstimatedWeightKg
            : car?.weight ?? 0;
        var effectiveWeightDistribution = databaseAnalysis.EstimatedWeightDistributionPercent > 0
            ? databaseAnalysis.EstimatedWeightDistributionPercent
            : car?.weightDist ?? 0;
        var effectiveDriveType = string.IsNullOrWhiteSpace(databaseAnalysis.EffectiveDriveType)
            ? car == null ? "" : NormalizeDrive(car.drive)
            : databaseAnalysis.EffectiveDriveType;
        var state = new TuningState
        {
            Make = vehicle?.Make ?? car?.make ?? "",
            Model = vehicle?.Model ?? car?.model ?? "",
            TuneId = databaseAnalysis.RecommendedTuneId,
            DriveType = effectiveDriveType,
            Surface = databaseAnalysis.RecommendedSurface,
            InputDevice = "controller",
            Weight = effectiveWeight,
            WeightDist = effectiveWeightDistribution,
            RedlineRpm = databaseAnalysis.EffectiveRedlineRpm > 0 ? databaseAnalysis.EffectiveRedlineRpm : car?.redlineRpm ?? 0,
            PeakTorqueRpm = databaseAnalysis.PerformanceRatingIsStockFallback ? 0 : car?.peakTorqueRpm ?? 0,
            MaxTorque = databaseAnalysis.PerformanceRatingIsStockFallback ? 0 : car?.maxTorqueNm ?? 0,
            Topspeed = databaseAnalysis.PerformanceRatingIsStockFallback ? 0 : car?.topSpeedKmh ?? 0,
            Gears = databaseAnalysis.EffectiveGearCount > 0 ? databaseAnalysis.EffectiveGearCount : defaultGearCount,
            TireWF = string.IsNullOrWhiteSpace(databaseAnalysis.EffectiveFrontTire) ? car?.frontTire ?? "" : databaseAnalysis.EffectiveFrontTire,
            TireWR = string.IsNullOrWhiteSpace(databaseAnalysis.EffectiveRearTire) ? car?.rearTire ?? "" : databaseAnalysis.EffectiveRearTire,
            Compound = databaseAnalysis.RecommendedCompound,
            HasAero = HasTunableSlider(sliders, 0) || HasTunableSlider(sliders, 1),
            AeroF = car?.frontAeroClampKg > 0 ? car.frontAeroClampKg : 0,
            AeroR = car?.rearAeroClampKg > 0 ? car.rearAeroClampKg : 0,
            DragCd = 0,
            Pi = car?.pi > 0 ? car.pi : 0,
            CarClass = string.IsNullOrWhiteSpace(carClass) ? "" : NormalizeClass(carClass),
            WeightUnit = "kg",
            SpeedUnit = "kmh",
            PressureUnit = "bar",
            SpringsUnit = "kgf/mm",
            IncludeGearing = includeGearing,
            FeelBalance = 0,
            FeelAggression = 0,
            DragDist = ""
        };

        var result = new TuningResult
        {
            Tires = Category(
                ("Front Pressure", Ui(sliders, 12, 1.0, 3.8, 1)),
                ("Rear Pressure", Ui(sliders, 23, 1.0, 3.8, 1))),
            Alignment = Category(
                ("Front Camber", Ui(sliders, 13, -5.0, 5.0, 1, " deg")),
                ("Rear Camber", Ui(sliders, 24, -5.0, 5.0, 1, " deg")),
                ("Front Toe", Ui(sliders, 14, -5.0, 5.0, 1, " deg")),
                ("Rear Toe", Ui(sliders, 25, -5.0, 5.0, 1, " deg")),
                ("Front Caster", Ui(sliders, 15, 1.0, 7.0, 1, " deg"))),
            Suspension = Category(
                ("Front Spring", Ui(sliders, 16, databaseAnalysis.Range("front_spring_rate"), 47.2, 236.0, 1)),
                ("Rear Spring", Ui(sliders, 27, databaseAnalysis.Range("rear_spring_rate"), 42.5, 236.0, 1)),
                ("Front Ride Height", Ui(sliders, 18, databaseAnalysis.Range("front_ride_height"), 17.0, 21.0, 1)),
                ("Rear Ride Height", Ui(sliders, 29, databaseAnalysis.Range("rear_ride_height"), 17.0, 21.0, 1))),
            ARB = Category(
                ("Front ARB", Ui(sliders, 17, databaseAnalysis.Range("front_antiroll_bar"), 1.0, 65.0, 1)),
                ("Rear ARB", Ui(sliders, 28, databaseAnalysis.Range("rear_antiroll_bar"), 1.0, 65.0, 1))),
            Damping = Category(
                ("Front Rebound", Ui(sliders, 20, databaseAnalysis.Range("front_rebound_stiffness"), 1.0, 20.0, 1)),
                ("Rear Rebound", Ui(sliders, 31, databaseAnalysis.Range("rear_rebound_stiffness"), 1.0, 20.0, 1)),
                ("Front Bump", Ui(sliders, 19, databaseAnalysis.Range("front_bump_stiffness"), 1.0, 20.0, 1)),
                ("Rear Bump", Ui(sliders, 30, databaseAnalysis.Range("rear_bump_stiffness"), 1.0, 20.0, 1))),
            Braking = Category(
                ("Brake Balance", Ui(sliders, 4, 0.0, 100.0, 0, "%")),
                ("Brake Pressure", Ui(sliders, 3, 0.0, 200.0, 0, "%"))),
            Diff = Category(
                ("Front Accel", Ui(sliders, 21, 0.0, 100.0, 0, "%")),
                ("Front Decel", Ui(sliders, 22, 0.0, 100.0, 0, "%")),
                ("Rear Accel", Ui(sliders, 32, 0.0, 100.0, 0, "%")),
                ("Rear Decel", Ui(sliders, 33, 0.0, 100.0, 0, "%")))
        };

        if (state.DriveType == "AWD" || HasTunableSlider(sliders, 6))
        {
            result.Diff.Values.Add(new TuningItem { Key = "Center Balance", Value = Ui(sliders, 6, 0.0, 100.0, 0, "%") });
            state.DriveType = "AWD";
        }

        if (state.HasAero)
        {
            result.Aero = Category(
                ("Front Downforce", Ui(sliders, 0, databaseAnalysis.Range("front_downforce"), 43.0, 204.0, 0)),
                ("Rear Downforce", Ui(sliders, 1, databaseAnalysis.Range("rear_downforce"), 22.0, 210.0, 0)));
        }

        if (state.IncludeGearing)
        {
            result.Gearing = new TuningCategory();
            result.Gearing.Values.Add(new TuningItem { Key = "Final Drive", Value = Ui(sliders, 2, 2.20, 6.10, 2) });
            for (var i = 1; i <= gearCount; i++)
            {
                var value = i == 10
                    ? partialGear10 ?? "--"
                    : Ui(sliders, 35 + i, 0.48, 6.00, 2);
                result.Gearing.Values.Add(new TuningItem { Key = GearKey(i), Value = value });
            }
        }

        var authorSuffix = string.IsNullOrWhiteSpace(candidate.Author) ? "" : $" - {candidate.Author}";
        return new SavedTune
        {
            Id = Guid.NewGuid().ToString(),
            Name = $"{candidate.TuneName}{authorSuffix}",
            SavedAt = DateTime.Now,
            CarSearchKeyword = car == null ? candidate.VehicleName : $"{car.make} {car.model}",
            SelectedCarText = car == null
                ? $"{candidate.VehicleName} (ID {candidate.CarId})"
                : $"{car.make} {car.model} ({car.year}) [{car.drive}]",
            State = state,
            Result = result,
            SourceFingerprint = candidate.SourceFingerprint,
            SourceCarId = candidate.CarId,
            SourceFolderName = candidate.FolderName,
            SourceAuthor = candidate.Author,
            EstimatedWeightKg = databaseAnalysis.EstimatedWeightKg,
            StockWeightKg = car?.weight ?? 0,
            PerformanceRatingIsStockFallback = databaseAnalysis.PerformanceRatingIsStockFallback,
            SpecificationNote = databaseAnalysis.PerformanceRatingIsStockFallback
                ? "重量、配重、驱动、轮胎、挡位和红线由已安装升级件推导；等级与 PI 仅为原厂参考；最终扭矩、峰值转速和极速无法从调教存档可靠恢复。"
                : "未识别到改变车辆规格的升级件，使用数据库车辆规格。",
            ModeRecommendationReason = databaseAnalysis.RecommendationReason,
            InstalledParts = databaseAnalysis.InstalledParts.ToList()
        };
    }

    private static SaveHeaderInfo ParseHeader(string path)
    {
        var data = File.ReadAllBytes(path);
        var strings = new List<string>();
        for (var offset = 0; offset <= data.Length - 6; offset++)
        {
            var charCount = ReadU32(data, offset);
            if (charCount <= 0 || charCount > 128)
            {
                continue;
            }

            var dataOffset = offset + 4;
            var byteCount = checked((int)charCount * 2);
            if (dataOffset + byteCount > data.Length)
            {
                continue;
            }

            try
            {
                var text = System.Text.Encoding.Unicode.GetString(data, dataOffset, byteCount);
                if (LooksLikeText(text) && !strings.Contains(text, StringComparer.Ordinal))
                {
                    strings.Add(text);
                }
            }
            catch
            {
            }
        }

        return new SaveHeaderInfo
        {
            TuneName = strings.Count > 0 ? strings[0] : "",
            Description = strings.Count > 1 ? strings[1] : "",
            Author = strings.Count > 2 ? strings[strings.Count - 1] : ""
        };
    }

    private static Dictionary<int, float> DecodeSliders(byte[] data)
    {
        var result = new Dictionary<int, float>();
        for (var index = 0; index < TuningSliderCount; index++)
        {
            var offset = TuningSlidersOffset + index * 4;
            var swapped = new[] { data[offset + 2], data[offset + 3], data[offset], data[offset + 1] };
            result[index] = BitConverter.ToSingle(swapped, 0);
        }

        return result;
    }

    private static string? DecodePartialGear10(byte[] data)
    {
        const int offset = 0x254;
        if (data.Length < offset + 2)
        {
            return null;
        }

        var high16 = BitConverter.ToUInt16(data, offset);
        if (high16 == 0xBF80)
        {
            return null;
        }

        var minBits = (uint)high16 << 16;
        var maxBits = minBits | 0xFFFF;
        var normalizedMin = BitConverter.ToSingle(BitConverter.GetBytes(minBits), 0);
        var normalizedMax = BitConverter.ToSingle(BitConverter.GetBytes(maxBits), 0);
        if (!IsTunableValue(normalizedMin) || !IsTunableValue(normalizedMax))
        {
            return null;
        }

        var displayMin = Math.Round(0.48 + normalizedMin * (6.0 - 0.48), 2);
        var displayMax = Math.Round(0.48 + normalizedMax * (6.0 - 0.48), 2);
        return Math.Abs(displayMin - displayMax) < 0.0001
            ? displayMin.ToString("0.00", CultureInfo.InvariantCulture)
            : null;
    }

    private static uint ReadU32(byte[] data, int offset)
    {
        if (offset + 4 > data.Length)
        {
            return 0;
        }

        return BitConverter.ToUInt32(data, offset);
    }

    private static bool LooksLikeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.IndexOf('\0') >= 0)
        {
            return false;
        }

        return text.All(ch => !char.IsControl(ch) || ch == '\t' || ch == '\r' || ch == '\n');
    }

    private static DateTime ParseTimestamp(string value)
    {
        return DateTime.TryParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
            ? parsed
            : DateTime.MinValue;
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string root, string pattern)
    {
        try
        {
            return Directory.EnumerateDirectories(root, pattern, SearchOption.TopDirectoryOnly).ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static TuningCategory Category(params (string Key, string Value)[] values)
    {
        var category = new TuningCategory();
        foreach (var item in values)
        {
            category.Values.Add(new TuningItem { Key = item.Key, Value = item.Value });
        }

        return category;
    }

    private static string Ui(IReadOnlyDictionary<int, float> sliders, int index, double min, double max, int decimals, string unit = "")
    {
        if (!sliders.TryGetValue(index, out var value) || !IsTunableValue(value))
        {
            return "--";
        }

        var normalized = value < 0.0f ? 0.0f : value > 1.0f ? 1.0f : value;
        var display = min + (max - min) * normalized;
        var format = decimals <= 0 ? "0" : "0." + new string('0', decimals);
        return display.ToString(format, CultureInfo.InvariantCulture) + unit;
    }

    private static string Ui(
        IReadOnlyDictionary<int, float> sliders,
        int index,
        TuneUiRange? range,
        double fallbackMin,
        double fallbackMax,
        int fallbackDecimals,
        string unit = "")
    {
        return range == null
            ? Ui(sliders, index, fallbackMin, fallbackMax, fallbackDecimals, unit)
            : Ui(sliders, index, range.Min, range.Max, range.Decimals, unit);
    }

    private static bool HasTunableSlider(IReadOnlyDictionary<int, float> sliders, int index)
    {
        return sliders.TryGetValue(index, out var value) && IsTunableValue(value);
    }

    private static bool IsTunableValue(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value > -0.5f;
    }

    private static int CountGears(IReadOnlyDictionary<int, float> sliders)
    {
        var count = 0;
        for (var index = 36; index <= 44; index++)
        {
            if (HasTunableSlider(sliders, index))
            {
                count++;
            }
        }

        return Math.Max(0, count);
    }

    private static string GearKey(int gear)
    {
        var suffix = gear == 1 ? "st" : gear == 2 ? "nd" : gear == 3 ? "rd" : "th";
        return $"{gear}{suffix} Gear";
    }

    private static string NormalizeDrive(string? drive)
    {
        if (string.Equals(drive, "FWD", StringComparison.OrdinalIgnoreCase)) return "FWD";
        if (string.Equals(drive, "RWD", StringComparison.OrdinalIgnoreCase)) return "RWD";
        return "AWD";
    }

    private static string Sha256(byte[] data)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(data);
        return string.Concat(hash.Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
    }

    private static string Sha256(string value)
    {
        return Sha256(System.Text.Encoding.UTF8.GetBytes(value));
    }

    private static string NormalizeClass(string? cls)
    {
        var value = (cls ?? "A").Trim().ToUpperInvariant();
        return new[] { "D", "C", "B", "A", "S1", "S2", "R", "X" }.Contains(value) ? value : "A";
    }

    private static Car? FindCar(string vehicleName, SaveVehicleInfo? vehicle)
    {
        if (vehicle != null)
        {
            var byFields = CarDatabase.CarsList.FirstOrDefault(car =>
                string.Equals(car.make, vehicle.Make, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(car.model, vehicle.Model, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(car.year, vehicle.Year.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase));
            if (byFields != null)
            {
                return byFields;
            }
        }

        return CarDatabase.CarsList.FirstOrDefault(car =>
            vehicleName.IndexOf(car.make, StringComparison.OrdinalIgnoreCase) >= 0 &&
            vehicleName.IndexOf(car.model, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private sealed class SaveHeaderInfo
    {
        public string TuneName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Author { get; set; } = "";
    }
}

internal sealed class SaveVehicleInfo
{
    public string DisplayName { get; set; } = "";
    public int Year { get; set; }
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public int CarId { get; set; }
}

internal static class SaveVehicleDatabase
{
    private static readonly Lazy<Dictionary<string, SaveVehicleInfo>> Data = new(Load);

    public static SaveVehicleInfo? Lookup(string carId)
    {
        if (Data.Value.TryGetValue(carId, out var info))
        {
            return info;
        }

        return int.TryParse(carId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericCarId) &&
               Data.Value.TryGetValue(numericCarId.ToString(CultureInfo.InvariantCulture), out info)
            ? info
            : null;
    }

    private static Dictionary<string, SaveVehicleInfo> Load()
    {
        var result = new Dictionary<string, SaveVehicleInfo>(StringComparer.OrdinalIgnoreCase);
        var path = CarDatabase.VehicleDatabasePath;
        if (!File.Exists(path))
        {
            return result;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            var item = property.Value;
            result[property.Name] = new SaveVehicleInfo
            {
                CarId = item.TryGetProperty("car_id", out var carId) ? carId.GetInt32() : 0,
                DisplayName = item.TryGetProperty("display_name", out var display) ? display.GetString() ?? "" : "",
                Year = item.TryGetProperty("year", out var year) ? year.GetInt32() : 0,
                Make = item.TryGetProperty("make", out var make) ? make.GetString() ?? "" : "",
                Model = item.TryGetProperty("model", out var model) ? model.GetString() ?? "" : ""
            };
        }

        return result;
    }
}
