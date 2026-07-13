using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace QING.Core;

public sealed class TuneUiRange
{
    public double Min { get; set; }
    public double Max { get; set; }
    public int Decimals { get; set; }
    public string Source { get; set; } = "";
}

public sealed class TuneDatabaseAnalysis
{
    public int ResolvedPartCount { get; set; }
    public double EstimatedWeightKg { get; set; }
    public double EstimatedWeightDistributionPercent { get; set; }
    public string EffectiveDriveType { get; set; } = "";
    public int EffectiveGearCount { get; set; }
    public string EffectiveFrontTire { get; set; } = "";
    public string EffectiveRearTire { get; set; } = "";
    public double EffectiveRedlineRpm { get; set; }
    public bool PerformanceRatingIsStockFallback { get; set; }
    public string RecommendedTuneId { get; set; } = "General";
    public string RecommendedSurface { get; set; } = "Road";
    public string RecommendedCompound { get; set; } = "Street";
    public string RecommendationReason { get; set; } = "未识别到专用轮胎，使用通用公路模式。";
    public List<InstalledPartSummary> InstalledParts { get; } = new();
    public Dictionary<string, TuneUiRange> Ranges { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public TuneUiRange? Range(string name) =>
        Ranges.TryGetValue(name, out var range) ? range : null;
}

public sealed class InstalledPartSummary
{
    public string SlotName { get; set; } = "";
    public string Category { get; set; } = "其他";
    public string DisplayName { get; set; } = "";
    public long PartId { get; set; }
    public int? Level { get; set; }
    public string Details { get; set; } = "";
    public bool IsEnginePart { get; set; }
}

internal static class FH6TuneDatabase
{
    private static readonly HashSet<string> EngineParts = new(StringComparer.OrdinalIgnoreCase)
    {
        "Camshaft", "Valves", "Displacement", "PistonsCompression", "FuelSystem",
        "Ignition", "Exhaust", "Intake", "Flywheel", "Manifold", "RestrictorPlate",
        "OilCooling", "SingleTurbo", "TwinTurbo", "QuadTurbo", "SuperchargerCSC",
        "SuperchargerDSC", "Intercooler"
    };

    private static readonly HashSet<string> DrivetrainParts = new(StringComparer.OrdinalIgnoreCase)
    {
        "Clutch", "Transmission", "Driveline", "Differential"
    };

    private static readonly HashSet<string> CarBodyParts = new(StringComparer.OrdinalIgnoreCase)
    {
        "FrontBumper", "RearBumper", "Hood", "SideSkirts", "TireWidthFront",
        "TireWidthRear", "WeightReduction", "ChassisStiffness", "TrackSpacingFront",
        "TrackSpacingRear", "FrontAspectRatio", "RearAspectRatio"
    };

    public static TuneDatabaseAnalysis Analyze(int carId, byte[] data)
    {
        var analysis = new TuneDatabaseAnalysis();
        if (data.Length < 53 * 4 || carId <= 0 || string.IsNullOrWhiteSpace(CarDatabase.GameDatabasePath))
        {
            return analysis;
        }

        try
        {
            var rawParts = Enumerable.Range(0, Math.Min(104, data.Length / 4))
                .Select(index => BitConverter.ToUInt32(data, index * 4))
                .ToArray();
            using var connection = CarDatabase.OpenGameDatabase();
            var selected = ResolveParts(connection, carId, rawParts);
            analysis.ResolvedPartCount = selected.Count;
            analysis.EstimatedWeightKg = EstimateWeight(connection, carId, selected);
            BuildEffectiveVehicleSpecifications(connection, carId, selected, analysis);
            BuildRanges(connection, carId, rawParts, selected, analysis);
            BuildModeRecommendation(selected, analysis);
            BuildInstalledPartSummaries(connection, carId, selected, analysis);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FH6 tune database analysis failed: {ex.Message}");
        }
        return analysis;
    }

    private static void BuildEffectiveVehicleSpecifications(
        SqliteConnection connection,
        int carId,
        IReadOnlyDictionary<string, Dictionary<string, object?>> selected,
        TuneDatabaseAnalysis analysis)
    {
        var car = QueryUnique(connection, "Data_Car", "Id = @id", ("@id", carId));
        if (car == null) return;

        var driveTypeId = GetInt(car, "DriveTypeID") ?? 0;
        if (selected.TryGetValue("Drivetrain", out var drivetrain))
        {
            var drivetrainId = GetInt(drivetrain, "DrivetrainID");
            if (drivetrainId != null)
            {
                var drivetrainData = QueryUnique(connection, "Data_Drivetrain", "DrivetrainID = @id", ("@id", drivetrainId.Value));
                driveTypeId = drivetrainData == null ? driveTypeId : GetInt(drivetrainData, "DrivetypeID") ?? driveTypeId;
            }
        }
        analysis.EffectiveDriveType = MapDriveType(driveTypeId);

        var distribution = GetDouble(car, "WeightDistribution");
        if (selected.TryGetValue("WeightReduction", out var reduction))
        {
            var reducedDistribution = GetDouble(reduction, "CMBackFront");
            if (reducedDistribution > 0 && reducedDistribution < 1)
            {
                distribution = reducedDistribution;
            }
        }
        distribution += selected.Values.Where(row => HasValue(row, "WeightDistDiff")).Sum(row => GetDouble(row, "WeightDistDiff"));
        analysis.EstimatedWeightDistributionPercent = Math.Round(Math.Min(1.0, Math.Max(0.0, distribution)) * 100.0, 1);

        var frontWidth = SelectedNumber(selected, "TireWidthFront", "FrontTireWidth", GetDouble(car, "FrontTireWidthMM"));
        var rearWidth = SelectedNumber(selected, "TireWidthRear", "RearTireWidth", GetDouble(car, "RearTireWidthMM"));
        var frontAspect = GetDouble(car, "FrontTireAspect") + SelectedNumber(selected, "FrontAspectRatio", "FrontTireAspectRatioOffset", 0);
        var rearAspect = GetDouble(car, "RearTireAspect") + SelectedNumber(selected, "RearAspectRatio", "RearTireAspectRatioOffset", 0);
        var frontRim = SelectedNumber(selected, "RimSizeFront", "FrontWheelDiameter", GetDouble(car, "FrontWheelDiameterIN"));
        var rearRim = SelectedNumber(selected, "RimSizeRear", "RearWheelDiameter", GetDouble(car, "RearWheelDiameterIN"));
        analysis.EffectiveFrontTire = FormatTire(frontWidth, frontAspect, frontRim);
        analysis.EffectiveRearTire = FormatTire(rearWidth, rearAspect, rearRim);

        if (selected.TryGetValue("Transmission", out var transmission))
        {
            analysis.EffectiveGearCount = Enumerable.Range(1, 10)
                .Count(index => GetDouble(transmission, $"GearRatio{index}") > 0);
        }
        if (analysis.EffectiveGearCount <= 0)
        {
            analysis.EffectiveGearCount = GetInt(car, "NumGears") ?? 0;
        }

        analysis.EffectiveRedlineRpm = selected.TryGetValue("Camshaft", out var camshaft) && GetDouble(camshaft, "RedlineRPM") > 0
            ? GetDouble(camshaft, "RedlineRPM")
            : GetDouble(car, "SimRedlineAngVel") * 60.0 / (2.0 * Math.PI);
        analysis.PerformanceRatingIsStockFallback = selected.Values.Any(row => (GetInt(row, "IsStock") ?? 1) == 0);
    }

    private static double SelectedNumber(
        IReadOnlyDictionary<string, Dictionary<string, object?>> selected,
        string slot,
        string field,
        double fallback)
    {
        return selected.TryGetValue(slot, out var row) && HasValue(row, field) ? GetDouble(row, field) : fallback;
    }

    private static string FormatTire(double width, double aspect, double rim)
    {
        return width > 0 && aspect > 0 && rim > 0
            ? $"{Math.Round(width):0}/{Math.Round(aspect):0}R{Math.Round(rim):0}"
            : "";
    }

    private static string MapDriveType(int value) => value switch
    {
        1 => "FWD",
        3 => "AWD",
        _ => "RWD"
    };

    private static void BuildModeRecommendation(
        IReadOnlyDictionary<string, Dictionary<string, object?>> selected,
        TuneDatabaseAnalysis analysis)
    {
        if (!selected.TryGetValue("TireCompound", out var tire))
        {
            return;
        }

        var model = GetString(tire, "TireModelName");
        var compoundId = GetInt(tire, "TireCompoundID") ?? 0;
        var normalized = model.ToUpperInvariant();

        if (normalized.Contains("DRAG") || compoundId == 14)
        {
            SetRecommendation(analysis, "Drag", "Road", "Drag", model, "直线加速轮胎");
        }
        else if (normalized.Contains("DRIFT") || compoundId is 17 or 20)
        {
            SetRecommendation(analysis, "Drift", "Road", "Drift", model, "漂移轮胎");
        }
        else if (normalized.Contains("SNOW") || compoundId == 19)
        {
            SetRecommendation(analysis, "Rally", "Snow", "Snow", model, "雪地轮胎");
        }
        else if (normalized.Contains("RALLY") || compoundId is >= 49 and <= 52)
        {
            SetRecommendation(analysis, "Rally", "Mixed", "Rally", model, "拉力轮胎");
        }
        else if (normalized.Contains("OFFROAD") || compoundId is >= 41 and <= 48 or 53)
        {
            SetRecommendation(analysis, "Rally", "Dirt", "Rally", model, "越野轮胎");
        }
        else if (normalized.Contains("SEMI_SLICK") || compoundId is 7 or 10 or 11)
        {
            SetRecommendation(analysis, "Race", "Road", "Race Semi-Slick", model, "半热熔赛道轮胎");
        }
        else if (normalized.Contains("SLICK") || compoundId is 12 or 13 or 18)
        {
            SetRecommendation(analysis, "Race", "Road", "Race Slick", model, "光头赛道轮胎");
        }
        else if (normalized.Contains("SPORT") || compoundId == 9)
        {
            SetRecommendation(analysis, "General", "Road", "Sport", model, "运动型公路轮胎");
        }
        else
        {
            SetRecommendation(analysis, "General", "Road", "Street", model, "公路轮胎");
        }
    }

    private static void SetRecommendation(
        TuneDatabaseAnalysis analysis,
        string tuneId,
        string surface,
        string compound,
        string sourceModel,
        string reason)
    {
        analysis.RecommendedTuneId = tuneId;
        analysis.RecommendedSurface = surface;
        analysis.RecommendedCompound = compound;
        analysis.RecommendationReason = string.IsNullOrWhiteSpace(sourceModel)
            ? $"识别为{reason}。"
            : $"轮胎 {sourceModel} 识别为{reason}。";
    }

    private static void BuildInstalledPartSummaries(
        SqliteConnection connection,
        int carId,
        IReadOnlyDictionary<string, Dictionary<string, object?>> selected,
        TuneDatabaseAnalysis analysis)
    {
        var car = QueryUnique(connection, "Data_Car", "Id = @id", ("@id", carId));
        var stockWheelId = car == null ? null : GetInt(car, "StockWheelID");

        foreach (var item in selected)
        {
            if (!IsModifiedPart(item.Key, item.Value, stockWheelId))
            {
                continue;
            }

            var summary = new InstalledPartSummary
            {
                SlotName = item.Key,
                Category = PartCategory(item.Key),
                DisplayName = PartDisplayName(item.Key),
                PartId = GetLong(item.Value, "Id") ?? GetLong(item.Value, "ID") ?? 0,
                Level = GetInt(item.Value, "Level"),
                IsEnginePart = IsEnginePart(item.Key),
                Details = BuildPartDetails(connection, item.Key, item.Value)
            };
            analysis.InstalledParts.Add(summary);
        }

        analysis.InstalledParts.Sort((left, right) =>
        {
            var category = PartCategoryOrder(left.Category).CompareTo(PartCategoryOrder(right.Category));
            return category != 0
                ? category
                : string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static bool IsModifiedPart(string slotName, Dictionary<string, object?> row, int? stockWheelId)
    {
        var isStock = GetInt(row, "IsStock");
        if (isStock != null)
        {
            return isStock.Value == 0;
        }
        if (slotName is "WheelStyle" or "WheelStyleRear")
        {
            var wheelId = GetInt(row, "ID") ?? GetInt(row, "Id");
            return wheelId != null && wheelId != stockWheelId;
        }
        return true;
    }

    private static bool IsEnginePart(string slotName) =>
        slotName is "Engine" or "Aspiration" || EngineParts.Contains(slotName);

    private static string PartCategory(string slotName)
    {
        if (IsEnginePart(slotName)) return "发动机";
        if (slotName is "Motor" or "MotorParts") return "电驱系统";
        if (DrivetrainParts.Contains(slotName) || slotName == "Drivetrain") return "传动系统";
        if (slotName is "Brakes" or "SpringDamper" or "AntiSwayFront" or "AntiSwayRear") return "底盘";
        if (slotName.IndexOf("Tire", StringComparison.OrdinalIgnoreCase) >= 0 ||
            slotName.IndexOf("Wheel", StringComparison.OrdinalIgnoreCase) >= 0 ||
            slotName.IndexOf("Rim", StringComparison.OrdinalIgnoreCase) >= 0 ||
            slotName.IndexOf("AspectRatio", StringComparison.OrdinalIgnoreCase) >= 0) return "轮胎轮毂";
        if (slotName is "FrontBumper" or "RearBumper" or "RearWing" or "Hood" or "SideSkirts") return "车身与空气动力";
        if (slotName is "WeightReduction" or "ChassisStiffness" or "CarBody" or "TrackSpacingFront" or "TrackSpacingRear") return "车身与重量";
        return "其他";
    }

    private static int PartCategoryOrder(string category) => category switch
    {
        "发动机" => 0,
        "电驱系统" => 1,
        "传动系统" => 2,
        "轮胎轮毂" => 3,
        "底盘" => 4,
        "车身与空气动力" => 5,
        "车身与重量" => 6,
        _ => 7
    };

    private static string PartDisplayName(string slotName) => slotName switch
    {
        "Engine" => "发动机互换",
        "Aspiration" => "进气形式",
        "Camshaft" => "凸轮轴",
        "Valves" => "气门",
        "Displacement" => "排量",
        "PistonsCompression" => "活塞与压缩比",
        "FuelSystem" => "燃油系统",
        "Ignition" => "点火系统",
        "Exhaust" => "排气系统",
        "Intake" => "进气系统",
        "Flywheel" => "飞轮",
        "Manifold" => "进气歧管",
        "RestrictorPlate" => "限流器",
        "OilCooling" => "机油冷却",
        "SingleTurbo" => "单涡轮",
        "TwinTurbo" => "双涡轮",
        "QuadTurbo" => "四涡轮",
        "SuperchargerCSC" => "离心式机械增压",
        "SuperchargerDSC" => "容积式机械增压",
        "Intercooler" => "中冷器",
        "Drivetrain" => "驱动系统互换",
        "Clutch" => "离合器",
        "Transmission" => "变速箱",
        "Driveline" => "传动轴",
        "Differential" => "差速器",
        "Brakes" => "制动系统",
        "SpringDamper" => "弹簧与阻尼",
        "AntiSwayFront" => "前防倾杆",
        "AntiSwayRear" => "后防倾杆",
        "TireCompound" => "轮胎配方",
        "TireWidthFront" => "前轮胎宽度",
        "TireWidthRear" => "后轮胎宽度",
        "RimSizeFront" => "前轮毂尺寸",
        "RimSizeRear" => "后轮毂尺寸",
        "WheelStyle" => "前轮毂样式",
        "WheelStyleRear" => "后轮毂样式",
        "FrontAspectRatio" => "前轮胎扁平比",
        "RearAspectRatio" => "后轮胎扁平比",
        "RearWing" => "尾翼",
        "FrontBumper" => "前包围",
        "RearBumper" => "后包围",
        "Hood" => "引擎盖",
        "SideSkirts" => "侧裙",
        "WeightReduction" => "车身减重",
        "ChassisStiffness" => "车身强化",
        "TrackSpacingFront" => "前轮距",
        "TrackSpacingRear" => "后轮距",
        "Motor" => "电机互换",
        "MotorParts" => "电机升级",
        _ => slotName
    };

    private static string BuildPartDetails(
        SqliteConnection connection,
        string slotName,
        Dictionary<string, object?> row)
    {
        var details = new List<string>();
        if (slotName == "Engine")
        {
            var engineId = GetInt(row, "EngineID");
            if (engineId != null)
            {
                var engine = QueryUnique(connection, "Data_Engine", "EngineID = @id", ("@id", engineId.Value));
                var engineName = engine == null ? "" : GetString(engine, "EngineName");
                details.Add(string.IsNullOrWhiteSpace(engineName) ? $"Engine {engineId}" : engineName);
            }
        }

        AddTextDetail(details, row, "TireModelName", "轮胎");
        AddNumberDetail(details, row, "TireCompoundID", "Compound", "0");
        AddNumberDetail(details, row, "RedlineRPM", "红线", "0", " rpm");
        AddNumberDetail(details, row, "Disp", "排量", "0.##");
        AddScaleDetail(details, row, "TorqueScale", "扭矩");
        AddScaleDetail(details, row, "MaxScale", "增压");
        AddNumberDetail(details, row, "PowerMaxScale", "功率参数", "0.###");
        AddNumberDetail(details, row, "FinalDriveRatio", "终传", "0.###");
        AddSignedDetail(details, row, "MassDiff", "质量", "+0.##;-0.##", " kg");
        AddSignedDetail(details, row, "WeightDistDiff", "配重修正", "+0.####;-0.####", "");
        AddScaleDetail(details, row, "DragScale", "阻力");
        AddNumberDetail(details, row, "FrontTirePressure", "前胎压", "0.#");
        AddNumberDetail(details, row, "RearTirePressure", "后胎压", "0.#");
        return string.Join(" · ", details.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static void AddTextDetail(List<string> details, Dictionary<string, object?> row, string key, string label)
    {
        var value = GetString(row, key);
        if (!string.IsNullOrWhiteSpace(value)) details.Add($"{label} {value}");
    }

    private static void AddNumberDetail(List<string> details, Dictionary<string, object?> row, string key, string label, string format, string suffix = "")
    {
        if (!HasValue(row, key)) return;
        var value = GetDouble(row, key);
        if (Math.Abs(value) > 0.000001) details.Add($"{label} {value.ToString(format, CultureInfo.InvariantCulture)}{suffix}");
    }

    private static void AddSignedDetail(List<string> details, Dictionary<string, object?> row, string key, string label, string format, string suffix)
    {
        if (!HasValue(row, key)) return;
        var value = GetDouble(row, key);
        if (Math.Abs(value) > 0.000001) details.Add($"{label} {value.ToString(format, CultureInfo.InvariantCulture)}{suffix}");
    }

    private static void AddScaleDetail(List<string> details, Dictionary<string, object?> row, string key, string label)
    {
        if (!HasValue(row, key)) return;
        var value = GetDouble(row, key);
        if (value > 0 && Math.Abs(value - 1.0) > 0.000001) details.Add($"{label} ×{value.ToString("0.###", CultureInfo.InvariantCulture)}");
    }

    private static Dictionary<string, Dictionary<string, object?>> ResolveParts(
        SqliteConnection connection,
        int carId,
        IReadOnlyList<uint> rawParts)
    {
        var result = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
        var context = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase)
        {
            ["EngineID"] = null,
            ["DrivetrainID"] = null,
            ["CarBodyID"] = null,
            ["MotorID"] = null
        };

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT ordering.Id, ordering.Name, definition.TableName
            FROM Data_UpgradePartOrder ordering
            LEFT JOIN Data_UpgradePart definition ON definition.PartName = ordering.Name
            ORDER BY ordering.Id";
        using var reader = command.ExecuteReader();
        var slots = new List<(int Order, string Name, string Table)>();
        while (reader.Read())
        {
            slots.Add((reader.GetInt32(0), reader.GetString(1), reader.IsDBNull(2) ? "" : reader.GetString(2)));
        }

        foreach (var slot in slots)
        {
            var index = slot.Order + 3;
            if (index >= rawParts.Count || rawParts[index] == uint.MaxValue || string.IsNullOrWhiteSpace(slot.Table))
            {
                continue;
            }

            var raw = rawParts[index];
            Dictionary<string, object?>? row;
            if (slot.Name.Equals("Engine", StringComparison.OrdinalIgnoreCase))
            {
                row = QueryUnique(connection, slot.Table,
                    "Ordinal = @car AND (Id & 65535) = @token",
                    ("@car", carId), ("@token", (int)(raw >> 16)));
            }
            else if (slot.Name.Equals("FrontBumper", StringComparison.OrdinalIgnoreCase) && context["CarBodyID"] != null)
            {
                row = QueryUnique(connection, slot.Table,
                    "CarBodyID = @parent AND (Id & 65535) = @token",
                    ("@parent", context["CarBodyID"]!.Value), ("@token", (int)(raw >> 16)));
            }
            else if (slot.Name is "WheelStyle" or "WheelStyleRear")
            {
                row = QueryUnique(connection, "List_Wheels", "ID = @id", ("@id", (int)(raw >> 16)));
            }
            else
            {
                var decoded = DecodePartId(raw);
                var parent = ParentFor(slot.Name, context);
                row = QueryPart(connection, slot.Table, decoded, (int)(raw >> 16), carId, parent.Field, parent.Value);
            }

            if (row == null)
            {
                continue;
            }
            result[slot.Name] = row;
            if (slot.Name.Equals("Engine", StringComparison.OrdinalIgnoreCase)) context["EngineID"] = GetInt(row, "EngineID");
            if (slot.Name.Equals("Drivetrain", StringComparison.OrdinalIgnoreCase)) context["DrivetrainID"] = GetInt(row, "DrivetrainID");
            if (slot.Name.Equals("CarBody", StringComparison.OrdinalIgnoreCase)) context["CarBodyID"] = GetInt(row, "CarBodyID");
            if (slot.Name.Equals("Motor", StringComparison.OrdinalIgnoreCase)) context["MotorID"] = GetInt(row, "MotorID");
        }
        return result;
    }

    private static (string? Field, int? Value) ParentFor(string partName, Dictionary<string, int?> context)
    {
        if (EngineParts.Contains(partName)) return ("EngineID", context["EngineID"]);
        if (DrivetrainParts.Contains(partName)) return ("DrivetrainID", context["DrivetrainID"]);
        if (partName.Equals("MotorParts", StringComparison.OrdinalIgnoreCase)) return ("MotorID", context["MotorID"]);
        if (CarBodyParts.Contains(partName)) return ("CarBodyID", context["CarBodyID"]);
        return ("Ordinal", null);
    }

    private static Dictionary<string, object?>? QueryPart(
        SqliteConnection connection,
        string table,
        long decodedId,
        int token,
        int carId,
        string? requestedParent,
        int? parentValue)
    {
        var columns = TableColumns(connection, table);
        var parent = MatchingColumn(columns, requestedParent);
        var filters = "Id = @id";
        var parameters = new List<(string, object)> { ("@id", decodedId) };
        if (parent != null && parentValue != null)
        {
            filters += $" AND \"{parent}\" = @parent";
            parameters.Add(("@parent", parentValue.Value));
        }
        else if (parent != null && requestedParent != null && !requestedParent.Equals("Ordinal", StringComparison.OrdinalIgnoreCase))
        {
            var unique = QueryUnique(connection, table, filters, parameters.ToArray());
            if (unique != null) return unique;
        }
        else if (columns.Contains("Ordinal") && carId > 0)
        {
            filters += " AND Ordinal = @car";
            parameters.Add(("@car", carId));
        }

        var row = QueryUnique(connection, table, filters, parameters.ToArray());
        if (row != null || token == 0xFFFF)
        {
            return row;
        }

        var tokenFilters = "(Id & 65535) = @token";
        var tokenParameters = new List<(string, object)> { ("@token", token) };
        if (parent != null && parentValue != null)
        {
            tokenFilters += $" AND \"{parent}\" = @parent";
            tokenParameters.Add(("@parent", parentValue.Value));
        }
        else if (columns.Contains("Ordinal"))
        {
            tokenFilters += " AND Ordinal = @car";
            tokenParameters.Add(("@car", carId));
        }
        return QueryUnique(connection, table, tokenFilters, tokenParameters.ToArray());
    }

    private static void BuildRanges(
        SqliteConnection connection,
        int carId,
        IReadOnlyList<uint> rawParts,
        IReadOnlyDictionary<string, Dictionary<string, object?>> selected,
        TuneDatabaseAnalysis analysis)
    {
        var decodedIds = new HashSet<long>(rawParts.Skip(3).Select(DecodePartId));
        var spring = SelectedOrStock(connection, carId, decodedIds, selected, "SpringDamper");
        if (spring != null)
        {
            AddSpringRanges(connection, analysis, "front", GetInt(spring, "FrontSpringDamperPhysicsID"));
            AddSpringRanges(connection, analysis, "rear", GetInt(spring, "RearSpringDamperPhysicsID"));
        }

        AddAntiRollRange(connection, carId, decodedIds, selected, analysis, "AntiSwayFront", "front_antiroll_bar");
        AddAntiRollRange(connection, carId, decodedIds, selected, analysis, "AntiSwayRear", "rear_antiroll_bar");
        AddAeroRange(connection, carId, decodedIds, selected, analysis, "FrontBumper", "front_downforce");
        AddAeroRange(connection, carId, decodedIds, selected, analysis, "RearWing", "rear_downforce");
    }

    private static void AddSpringRanges(SqliteConnection connection, TuneDatabaseAnalysis analysis, string side, int? physicsId)
    {
        if (physicsId == null) return;
        var row = QueryUnique(connection, "List_SpringDamperPhysics", "SpringDamperPhysicsID = @id", ("@id", physicsId.Value));
        if (row == null) return;
        var scale = analysis.EstimatedWeightKg > 0
            ? analysis.EstimatedWeightKg / 1000.0 * 10.0 / 9.80665
            : 1.18;
        AddRange(analysis, $"{side}_ride_height", GetDouble(row, "MinRideHeight") * 100.0, GetDouble(row, "MaxRideHeight") * 100.0, 1, "FH6 DB");
        AddRange(analysis, $"{side}_spring_rate", GetDouble(row, "MinSpringRate") * scale, GetDouble(row, "MaxSpringRate") * scale, 1, "FH6 DB + estimated weight");
        AddRange(analysis, $"{side}_bump_stiffness", GetDouble(row, "MinDampenBumpRate"), GetDouble(row, "MaxDampenBumpRate"), 1, "FH6 DB");
        AddRange(analysis, $"{side}_rebound_stiffness", GetDouble(row, "MinDampenReboundRate"), GetDouble(row, "MaxDampenReboundRate"), 1, "FH6 DB");
    }

    private static void AddAntiRollRange(
        SqliteConnection connection, int carId, HashSet<long> ids,
        IReadOnlyDictionary<string, Dictionary<string, object?>> selected,
        TuneDatabaseAnalysis analysis, string partName, string rangeName)
    {
        var part = SelectedOrStock(connection, carId, ids, selected, partName);
        var physicsId = part == null ? null : GetInt(part, "AntiSwayPhysicsID");
        if (physicsId == null) return;
        var row = QueryUnique(connection, "List_AntiSwayPhysics", "AntiSwayPhysicsID = @id", ("@id", physicsId.Value));
        if (row != null) AddRange(analysis, rangeName, GetDouble(row, "MinSwaybarStiffness"), GetDouble(row, "MaxSwaybarStiffness"), 1, "FH6 DB");
    }

    private static void AddAeroRange(
        SqliteConnection connection, int carId, HashSet<long> ids,
        IReadOnlyDictionary<string, Dictionary<string, object?>> selected,
        TuneDatabaseAnalysis analysis, string partName, string rangeName)
    {
        var part = SelectedOrStock(connection, carId, ids, selected, partName);
        var physicsId = part == null ? null : GetInt(part, "AeroPhysicsID");
        if (physicsId == null) return;
        var row = QueryUnique(connection, "List_AeroPhysics", "AeroPhysicsID = @id", ("@id", physicsId.Value));
        if (row == null) return;
        var first = GetDouble(row, "Downforce0");
        var second = GetDouble(row, "Downforce1");
        AddRange(analysis, rangeName, Math.Min(first, second), Math.Max(first, second), 0, "FH6 DB");
    }

    private static Dictionary<string, object?>? SelectedOrStock(
        SqliteConnection connection,
        int carId,
        HashSet<long> decodedIds,
        IReadOnlyDictionary<string, Dictionary<string, object?>> selected,
        string partName)
    {
        if (selected.TryGetValue(partName, out var selectedRow)) return selectedRow;
        using var definition = connection.CreateCommand();
        definition.CommandText = "SELECT TableName FROM Data_UpgradePart WHERE PartName = @name LIMIT 1";
        definition.Parameters.AddWithValue("@name", partName);
        var table = Convert.ToString(definition.ExecuteScalar(), CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(table)) return null;
        foreach (var id in decodedIds)
        {
            var row = QueryUnique(connection, table!, "Id = @id", ("@id", id));
            if (row != null) return row;
        }

        if (partName is "FrontBumper" or "RearBumper" or "Hood" or "SideSkirts")
        {
            var body = QueryUnique(connection, "List_UpgradeCarBody", "Ordinal = @car AND IsStock = 1", ("@car", carId));
            var bodyId = body == null ? null : GetInt(body, "CarBodyID");
            return bodyId == null ? null : QueryUnique(connection, table!, "CarBodyID = @body AND IsStock = 1", ("@body", bodyId.Value));
        }
        return QueryUnique(connection, table!, "Ordinal = @car AND IsStock = 1", ("@car", carId));
    }

    private static double EstimateWeight(
        SqliteConnection connection,
        int carId,
        IReadOnlyDictionary<string, Dictionary<string, object?>> selected)
    {
        var car = QueryUnique(connection, "Data_Car", "Id = @id", ("@id", carId));
        if (car == null) return 0;
        var total = selected.TryGetValue("WeightReduction", out var weight) && HasValue(weight, "Mass")
            ? GetDouble(weight, "Mass")
            : GetDouble(car, "CurbWeight") * 100.0;
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "WeightReduction", "CarBody", "WheelStyle", "WheelStyleRear"
        };
        foreach (var item in selected)
        {
            if (excluded.Contains(item.Key) || !HasValue(item.Value, "MassDiff")) continue;
            var multiplier = item.Key is "TireWidthFront" or "TireWidthRear" ? 2.0 : 1.0;
            total += GetDouble(item.Value, "MassDiff") * multiplier;
        }

        var stockWheelId = GetInt(car, "StockWheelID");
        var stockWheel = stockWheelId == null
            ? null
            : QueryUnique(connection, "List_Wheels", "ID = @id", ("@id", stockWheelId.Value));
        if (stockWheel != null && HasValue(stockWheel, "Mass"))
        {
            var stockMass = GetDouble(stockWheel, "Mass");
            foreach (var wheelPart in new[] { "WheelStyle", "WheelStyleRear" })
            {
                if (selected.TryGetValue(wheelPart, out var selectedWheel) && HasValue(selectedWheel, "Mass"))
                {
                    total += (GetDouble(selectedWheel, "Mass") - stockMass) * 10.0;
                }
            }
        }
        return total;
    }

    private static Dictionary<string, object?>? QueryUnique(
        SqliteConnection connection,
        string table,
        string where,
        params (string Name, object Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM \"{table}\" WHERE {where} LIMIT 2";
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        var row = ReadRow(reader);
        return reader.Read() ? null : row;
    }

    private static HashSet<string> TableColumns(SqliteConnection connection, string table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{table}\")";
        using var reader = command.ExecuteReader();
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read()) result.Add(reader.GetString(1));
        return result;
    }

    private static string? MatchingColumn(HashSet<string> columns, string? requested)
    {
        if (requested == null) return null;
        if (columns.Contains(requested)) return columns.First(column => column.Equals(requested, StringComparison.OrdinalIgnoreCase));
        var alias = requested switch
        {
            "CarBodyID" => "CarBodyId",
            "DrivetrainID" => "DrivetrainId",
            _ => requested
        };
        return columns.Contains(alias) ? columns.First(column => column.Equals(alias, StringComparison.OrdinalIgnoreCase)) : null;
    }

    private static Dictionary<string, object?> ReadRow(SqliteDataReader reader)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < reader.FieldCount; index++)
        {
            row[reader.GetName(index)] = reader.IsDBNull(index) ? null : reader.GetValue(index);
        }
        return row;
    }

    private static long DecodePartId(uint value) => value == uint.MaxValue
        ? -1
        : ((long)(value & 0xFFFF) << 16) | ((value >> 16) & 0xFFFF);

    private static void AddRange(TuneDatabaseAnalysis analysis, string name, double min, double max, int decimals, string source)
    {
        if (double.IsNaN(min) || double.IsNaN(max)) return;
        analysis.Ranges[name] = new TuneUiRange { Min = min, Max = max, Decimals = decimals, Source = source };
    }

    private static bool HasValue(Dictionary<string, object?> row, string name) =>
        row.TryGetValue(name, out var value) && value != null && value != DBNull.Value;

    private static int? GetInt(Dictionary<string, object?> row, string name) =>
        HasValue(row, name) ? Convert.ToInt32(row[name], CultureInfo.InvariantCulture) : null;

    private static long? GetLong(Dictionary<string, object?> row, string name) =>
        HasValue(row, name) ? Convert.ToInt64(row[name], CultureInfo.InvariantCulture) : null;

    private static string GetString(Dictionary<string, object?> row, string name) =>
        HasValue(row, name) ? Convert.ToString(row[name], CultureInfo.InvariantCulture) ?? "" : "";

    private static double GetDouble(Dictionary<string, object?> row, string name) =>
        HasValue(row, name) ? Convert.ToDouble(row[name], CultureInfo.InvariantCulture) : 0;
}
