using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace QING.Core;

/// <summary>
/// FH6 Adjust Tool 调校分享码编解码器。
///
/// 当前格式版本: v1   前缀: "FH6v1:"
/// 编码流程: Payload JSON → UTF-8 → GZip(Optimal) → Base64
///
/// ── 版本升级规则 ──────────────────────────────────────────────────────────
/// 1. 若只是新增枚举值（如新增路面类型），在编码表末尾追加即可，无需升级版本，
///    旧分享码仍可解码（不含新枚举的旧码依然有效）。
/// 2. 若改变 State 数组的字段顺序/语义，或 Result 数组的固定段顺序，
///    必须升级版本号（PREFIX 改为 "FH6v2:"，并新增 DecodeV2 方法）。
/// 3. TryDecode 应按前缀路由到对应版本的解码器，实现向前兼容。
///
/// ── State 数组布局 v1 (24 个元素) ─────────────────────────────────────────
/// [0]  TuneId       枚举索引
/// [1]  DriveType    枚举索引
/// [2]  Surface      枚举索引
/// [3]  InputDevice  枚举索引
/// [4]  Weight       double
/// [5]  WeightDist   double
/// [6]  Gears        int
/// [7]  TireWF       string (自由文本)
/// [8]  TireWR       string (自由文本)
/// [9]  Compound     枚举索引
/// [10] HasAero      0/1
/// [11] AeroF        double
/// [12] AeroR        double
/// [13] DragCd       double
/// [14] Pi           int
/// [15] CarClass     枚举索引
/// [16] WeightUnit   枚举索引
/// [17] SpeedUnit    枚举索引
/// [18] PressureUnit 枚举索引
/// [19] SpringsUnit  枚举索引
/// [20] FeelBalance  double
/// [21] FeelAggression double
/// [22] IncludeGearing 0/1
/// [23] DragDist     枚举索引
///
/// ── Result 数组布局 v1 (变长平铺) ─────────────────────────────────────────
/// 固定段 (始终存在, 23 个值):
///   Tires:      Front Pressure, Rear Pressure
///   Alignment:  Front Camber, Rear Camber, Front Toe, Rear Toe, Front Caster
///   Suspension: Front Spring, Rear Spring, Front Ride Height, Rear Ride Height
///   ARB:        Front ARB, Rear ARB
///   Damping:    Front Rebound, Rear Rebound, Front Bump, Rear Bump
///   Braking:    Brake Balance, Brake Pressure
///   Diff:       Front Accel, Front Decel, Rear Accel, Rear Decel
/// 条件段 (按 State 推断):
///   if DriveType=="AWD": Diff/Center Balance  (+1)
///   if HasAero==true:    Aero/Front Downforce, Rear Downforce  (+2)
///   if IncludeGearing==true: Gearing/Final Drive, 1st Gear … {Gears}th Gear  (+1+Gears)
/// </summary>
public static class ShareCodec
{
    private const string PrefixV1 = "FH6v1:";

    // ── 编码表 v1 ─────────────────────────────────────────────────────────
    // ⚠️ 已有条目的顺序永远不能改变，可在末尾追加新值

    private static readonly string[] TuneIds =
        ["General", "Race", "Touge", "Wangan", "Drift", "Drag", "Rally", "Rain"];

    private static readonly string[] DriveTypes =
        ["AWD", "RWD", "FWD"];

    private static readonly string[] Surfaces =
        ["Road", "Dirt", "Snow", "Mixed"];

    private static readonly string[] InputDevices =
        ["controller", "wheel", "keyboard"];

    private static readonly string[] Compounds =
        ["Street", "Sport", "Race Semi-Slick", "Race Slick", "Rally", "Drift", "Snow", "Drag"];

    private static readonly string[] CarClasses =
        ["D", "C", "B", "A", "S1", "S2", "R", "X"];

    private static readonly string[] WeightUnits  = ["lbs", "kg"];
    private static readonly string[] SpeedUnits   = ["mph", "kmh"];
    private static readonly string[] PressUnits   = ["psi", "bar"];
    private static readonly string[] SpringUnits  = ["lbs/in", "n/mm", "kgf/mm"];
    private static readonly string[] DragDists    = ["quarter", "half", "top"];

    // ── 公开 API ──────────────────────────────────────────────────────────

    /// <summary>将 SavedTune 编码为可分享的字符串。</summary>
    public static string Encode(SavedTune tune)
    {
        var s = tune.State;
        var r = tune.Result;

        // State → 紧凑数组（枚举→索引，bool→0/1）
        object[] stateArr =
        [
            Idx(TuneIds,      s.TuneId),
            Idx(DriveTypes,   s.DriveType),
            Idx(Surfaces,     s.Surface),
            Idx(InputDevices, s.InputDevice),
            s.Weight,
            s.WeightDist,
            s.Gears,
            s.TireWF ?? "",
            s.TireWR ?? "",
            Idx(Compounds,    s.Compound),
            s.HasAero ? 1 : 0,
            s.AeroF,
            s.AeroR,
            s.DragCd,
            s.Pi,
            Idx(CarClasses,   s.CarClass),
            Idx(WeightUnits,  s.WeightUnit),
            Idx(SpeedUnits,   s.SpeedUnit),
            Idx(PressUnits,   s.PressureUnit),
            Idx(SpringUnits,  s.SpringsUnit),
            s.FeelBalance,
            s.FeelAggression,
            s.IncludeGearing ? 1 : 0,
            Idx(DragDists,    s.DragDist),
        ];

        // Result → 平铺值数组
        var vals = new List<string>();

        void Add(TuningCategory? cat, params string[] keys)
        {
            foreach (var k in keys)
            {
                var item = cat?.Values?.Find(i => i.Key == k);
                vals.Add(item?.Value ?? "--");
            }
        }

        // 固定段
        Add(r?.Tires,      "Front Pressure", "Rear Pressure");
        Add(r?.Alignment,  "Front Camber", "Rear Camber", "Front Toe", "Rear Toe", "Front Caster");
        Add(r?.Suspension, "Front Spring", "Rear Spring", "Front Ride Height", "Rear Ride Height");
        Add(r?.ARB,        "Front ARB", "Rear ARB");
        Add(r?.Damping,    "Front Rebound", "Rear Rebound", "Front Bump", "Rear Bump");
        Add(r?.Braking,    "Brake Balance", "Brake Pressure");
        Add(r?.Diff,       "Front Accel", "Front Decel", "Rear Accel", "Rear Decel");

        // 条件段
        if (s.DriveType == "AWD")
            Add(r?.Diff, "Center Balance");

        if (s.HasAero)
            Add(r?.Aero, "Front Downforce", "Rear Downforce");

        if (s.IncludeGearing && r?.Gearing != null)
        {
            Add(r.Gearing, "Final Drive");
            for (int i = 1; i <= s.Gears; i++)
            {
                string sfx = i switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
                Add(r.Gearing, $"{i}{sfx} Gear");
            }
        }

        // 打包 + GZip + Base64
        var payload = new
        {
            N = tune.Name,
            C = tune.SelectedCarText ?? "",
            K = tune.CarSearchKeyword ?? "",
            S = stateArr,
            R = vals.ToArray()
        };

        var json  = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.Optimal))
            gz.Write(bytes, 0, bytes.Length);

        return PrefixV1 + Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// 尝试将分享码解码为 SavedTune。
    /// 支持所有历史版本（当前仅 v1）。
    /// </summary>
    public static bool TryDecode(string code, out SavedTune? tune)
    {
        tune = null;
        if (string.IsNullOrWhiteSpace(code)) return false;
        try
        {
            if (code.StartsWith(PrefixV1))
            {
                tune = DecodeV1(code[PrefixV1.Length..]);
                return tune != null;
            }
            // 未来版本: if (code.StartsWith(PrefixV2)) tune = DecodeV2(...)
            return false;
        }
        catch
        {
            return false;
        }
    }

    // ── v1 解码器 ─────────────────────────────────────────────────────────

    private static SavedTune DecodeV1(string b64)
    {
        var compressed = Convert.FromBase64String(b64);
        using var ms   = new MemoryStream(compressed);
        using var gz   = new GZipStream(ms, CompressionMode.Decompress);
        using var sr   = new StreamReader(gz, Encoding.UTF8);
        var doc = JsonNode.Parse(sr.ReadToEnd())!;

        var sArr = doc["S"]!.AsArray();
        var rArr = doc["R"]!.AsArray();
        int ri   = 0;
        string Next() => ri < rArr.Count ? rArr[ri++]!.GetValue<string>() : "--";

        TuningCategory Cat(params string[] keys)
        {
            var c = new TuningCategory();
            foreach (var k in keys)
                c.Values.Add(new TuningItem { Key = k, Value = Next() });
            return c;
        }

        // 解码 State
        var s = new TuningState
        {
            TuneId        = FromIdx(TuneIds,      sArr[0]),
            DriveType     = FromIdx(DriveTypes,   sArr[1]),
            Surface       = FromIdx(Surfaces,     sArr[2]),
            InputDevice   = FromIdx(InputDevices, sArr[3]),
            Weight        = sArr[4]!.GetValue<double>(),
            WeightDist    = sArr[5]!.GetValue<double>(),
            Gears         = sArr[6]!.GetValue<int>(),
            TireWF        = sArr[7]!.GetValue<string>(),
            TireWR        = sArr[8]!.GetValue<string>(),
            Compound      = FromIdx(Compounds,    sArr[9]),
            HasAero       = sArr[10]!.GetValue<int>() == 1,
            AeroF         = sArr[11]!.GetValue<double>(),
            AeroR         = sArr[12]!.GetValue<double>(),
            DragCd        = sArr[13]!.GetValue<double>(),
            Pi            = sArr[14]!.GetValue<int>(),
            CarClass      = FromIdx(CarClasses,   sArr[15]),
            WeightUnit    = FromIdx(WeightUnits,  sArr[16]),
            SpeedUnit     = FromIdx(SpeedUnits,   sArr[17]),
            PressureUnit  = FromIdx(PressUnits,   sArr[18]),
            SpringsUnit   = FromIdx(SpringUnits,  sArr[19]),
            FeelBalance    = sArr[20]!.GetValue<double>(),
            FeelAggression = sArr[21]!.GetValue<double>(),
            IncludeGearing = sArr[22]!.GetValue<int>() == 1,
            DragDist       = FromIdx(DragDists,   sArr[23]),
        };

        // 解码 Result（固定段）
        var r = new TuningResult
        {
            Tires      = Cat("Front Pressure", "Rear Pressure"),
            Alignment  = Cat("Front Camber", "Rear Camber", "Front Toe", "Rear Toe", "Front Caster"),
            Suspension = Cat("Front Spring", "Rear Spring", "Front Ride Height", "Rear Ride Height"),
            ARB        = Cat("Front ARB", "Rear ARB"),
            Damping    = Cat("Front Rebound", "Rear Rebound", "Front Bump", "Rear Bump"),
            Braking    = Cat("Brake Balance", "Brake Pressure"),
            Diff       = Cat("Front Accel", "Front Decel", "Rear Accel", "Rear Decel"),
        };

        // 解码 Result（条件段）
        if (s.DriveType == "AWD")
            r.Diff.Values.Add(new TuningItem { Key = "Center Balance", Value = Next() });

        if (s.HasAero)
            r.Aero = Cat("Front Downforce", "Rear Downforce");

        if (s.IncludeGearing)
        {
            r.Gearing = new TuningCategory();
            r.Gearing.Values.Add(new TuningItem { Key = "Final Drive", Value = Next() });
            for (int i = 1; i <= s.Gears; i++)
            {
                string sfx = i switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
                r.Gearing.Values.Add(new TuningItem { Key = $"{i}{sfx} Gear", Value = Next() });
            }
        }

        return new SavedTune
        {
            Id               = Guid.NewGuid().ToString(),
            Name             = doc["N"]!.GetValue<string>(),
            SelectedCarText  = doc["C"]!.GetValue<string>(),
            CarSearchKeyword = doc["K"]!.GetValue<string>(),
            SavedAt          = DateTime.Now,
            State            = s,
            Result           = r,
        };
    }

    // ── 辅助 ─────────────────────────────────────────────────────────────

    private static int Idx(string[] table, string? val)
    {
        var i = Array.IndexOf(table, val ?? "");
        return i < 0 ? 0 : i;
    }

    private static string FromIdx(string[] table, JsonNode? node)
    {
        var i = node?.GetValue<int>() ?? 0;
        return (i >= 0 && i < table.Length) ? table[i] : table[0];
    }
}
