using System;
using System.Collections.Generic;

namespace QING.Core;

public class TuningState
{
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public string TuneId { get; set; } = "General"; // Race, Touge, Drift, Drag, Rally, Wangan, Rain, General
    public string DriveType { get; set; } = "AWD"; // FWD, RWD, AWD
    public string Surface { get; set; } = "Road"; // Road, Dirt, Snow, Mixed
    public string InputDevice { get; set; } = "controller"; // controller, wheel, keyboard
    
    public double Weight { get; set; } = 3000; // lbs or kg
    public double WeightDist { get; set; } = 50; // Front weight % (e.g. 52)
    public double RedlineRpm { get; set; } = 0;
    public double PeakTorqueRpm { get; set; } = 0;
    public double MaxTorque { get; set; } = 0;
    public double Topspeed { get; set; } = 150;
    public int Gears { get; set; } = 6;
    public string TireWF { get; set; } = "245/35R19";
    public string TireWR { get; set; } = "275/35R19";
    public string Compound { get; set; } = "Street";
    
    public bool HasAero { get; set; } = false;
    public double AeroF { get; set; } = 0;
    public double AeroR { get; set; } = 0;
    public double DragCd { get; set; } = 0.35;
    
    public int Pi { get; set; } = 700;
    public string CarClass { get; set; } = "A"; // D, C, B, A, S1, S2, R, X
    
    // Units
    public string WeightUnit { get; set; } = "lbs"; // lbs, kg
    public string SpeedUnit { get; set; } = "mph"; // mph, kmh
    public string PressureUnit { get; set; } = "psi"; // psi, bar
    public string SpringsUnit { get; set; } = "lbs/in"; // lbs/in, n/mm, kgf/mm
    
    // Feel Adjusters
    public double FeelBalance { get; set; } = 50; // 0-100 (50 is center)
    public double FeelAggression { get; set; } = 50; // 0-100
    
    public bool IncludeGearing { get; set; } = false;
    public string DragDist { get; set; } = "quarter"; // quarter, half, top
}

public class TuningItem
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public class TuningCategory
{
    public List<TuningItem> Values { get; set; } = new();
    public string Tip { get; set; } = "";
}

public class TuningResult
{
    public TuningCategory Tires { get; set; } = new();
    public TuningCategory? Gearing { get; set; }
    public TuningCategory Alignment { get; set; } = new();
    public TuningCategory Suspension { get; set; } = new();
    public TuningCategory ARB { get; set; } = new();
    public TuningCategory Damping { get; set; } = new();
    public TuningCategory Braking { get; set; } = new();
    public TuningCategory Diff { get; set; } = new();
    public TuningCategory? Aero { get; set; }
}

public static class TuningCalculator
{
    private static readonly Dictionary<string, (double f, double r)> FreqMult = new()
    {
        { "Race",    (1.10, 1.01) },
        { "Touge",   (1.08, 0.99) },
        { "Drift",   (0.85, 0.78) },
        { "Rally",   (0.63, 0.58) },
        { "Drag",    (0.95, 0.72) },
        { "Wangan",  (1.04, 0.97) },
        { "Rain",    (0.85, 0.79) },
        { "General", (0.96, 0.91) }
    };

    private const double DampRebound = 0.70;
    private const double DampBump = 0.52;
    private const double HorizonDampMult = 1.10;
    
    public static TuningResult Calculate(TuningState s)
    {
        double wKg = s.WeightUnit == "lbs" ? s.Weight / 2.205 : s.Weight;
        double speedKmh = s.SpeedUnit == "mph" ? s.Topspeed * 1.609 : s.Topspeed;
        double torqueNm = s.WeightUnit == "lbs" ? s.MaxTorque * 1.356 : s.MaxTorque;
        
        double frontPct = s.WeightDist / 100.0;
        double rearPct = 1.0 - frontPct;
        
        // Corner weights (kg)
        double cwFL = wKg * frontPct * 0.5;
        double cwRL = wKg * rearPct * 0.5;
        
        bool isDrift = s.TuneId == "Drift";
        bool isDrag = s.TuneId == "Drag";
        bool isRain = s.TuneId == "Rain";
        bool isRally = s.TuneId == "Rally" || s.Surface == "Dirt" || s.Surface == "Mixed";
        bool isTouge = s.TuneId == "Touge";
        bool isWangan = s.TuneId == "Wangan";
        
        bool isFWD = s.DriveType == "FWD";
        bool isRWD = s.DriveType == "RWD";
        bool isAWD = s.DriveType == "AWD";
        
        bool isWheel = s.InputDevice == "wheel";
        bool isSnow = s.Surface == "Snow";
        
        double pwr2wt = (s.WeightUnit == "lbs" ? s.MaxTorque * 1.356 : s.MaxTorque) / (wKg / 1000.0);
        
        // PI-based natural frequency
        int piNum = Math.Max(100, Math.Min(999, s.Pi));
        double baseFreq = 7.35e-7 * Math.Pow(piNum - 100, 2) + 2.65;
        
        // Mode multipliers
        var mod = FreqMult.TryGetValue(s.TuneId, out var m) ? m : FreqMult["General"];
        double freqF = baseFreq * mod.f;
        double freqR = baseFreq * mod.r;
        
        // Damping multiplier
        double dampMod = HorizonDampMult * (1.0 + (s.FeelAggression - 50.0) / 200.0);
        
        // Spring rates
        Func<double, double, double> calcSpring = (cornerMass, f) =>
        {
            double kNm = cornerMass * Math.Pow(2.0 * Math.PI * f, 2);
            double FORZA_SCALE = 9.0;
            if (s.SpringsUnit == "lbs/in") return Math.Round(kNm / 175.127, 1);
            if (s.SpringsUnit == "n/mm") return Math.Round(kNm / 1000.0 * FORZA_SCALE, 1);
            if (s.SpringsUnit == "kgf/mm") return Math.Round(kNm / 9806.65 * FORZA_SCALE, 2);
            return Math.Round(kNm / 175.127, 1);
        };
        
        double fSpring = calcSpring(cwFL, freqF);
        double rSpring = calcSpring(cwRL, freqR);
        
        // Feel adjuster: balance slider shifts ratio
        double balanceMod = (s.FeelBalance - 50.0) / 200.0; // -0.25 to 0.25
        int decimals = s.SpringsUnit == "kgf/mm" ? 2 : 1;
        fSpring = Math.Round(fSpring * (1.0 + balanceMod), decimals);
        rSpring = Math.Round(rSpring * (1.0 - balanceMod), decimals);
        
        // Ride Height
        double fRideCm = isDrift ? 15.5 : isRally ? 20.0 : isSnow ? 22.0 : isDrag ? 15.0 : 15.0;
        double rRideCm = isDrift ? 15.0 : isRally ? 19.0 : isSnow ? 21.0 : isDrag ? 17.0 : 15.0;
        double fRide = fRideCm;
        double rRide = rRideCm;
        
        // Damping
        double fSpringPhys = s.SpringsUnit == "lbs/in" ? fSpring * 175.127 :
                             s.SpringsUnit == "kgf/mm" ? fSpring * 9806.65 / 9.0 :
                                                         fSpring * 1000.0 / 9.0;
        double rSpringPhys = s.SpringsUnit == "lbs/in" ? rSpring * 175.127 :
                             s.SpringsUnit == "kgf/mm" ? rSpring * 9806.65 / 9.0 :
                                                         rSpring * 1000.0 / 9.0;
        double critDampF = 2.0 * Math.Sqrt(cwFL * fSpringPhys);
        double critDampR = 2.0 * Math.Sqrt(cwRL * rSpringPhys);
        
        double rebRatio = (isDrift ? 0.70 : isRally ? 0.60 : DampRebound) * HorizonDampMult;
        double bumRatio = (isDrift ? 0.45 : isRally ? 0.42 : DampBump) * HorizonDampMult;
        
        Func<double, double> mapDampF = (v) => Math.Round(Math.Max(1.0, Math.Min(20.0, v / critDampF * 10.0 * dampMod)), 1);
        Func<double, double> mapDampR = (v) => Math.Round(Math.Max(1.0, Math.Min(20.0, v / critDampR * 10.0 * dampMod)), 1);
        
        double fRebound = mapDampF(critDampF * rebRatio);
        double rRebound = mapDampR(critDampR * rebRatio);
        double fBump = mapDampF(critDampF * bumRatio);
        double rBump = mapDampR(critDampR * bumRatio);
        
        // ARB
        double pwr2wtNorm = Math.Min(1.0, pwr2wt / 800.0);
        double fARB, rARB;
        
        if (isDrift)
        {
            fARB = 10.0 + (s.FeelAggression / 100.0) * 8.0;
            rARB = 28.0 + (s.FeelAggression / 100.0) * 20.0;
        }
        else if (isDrag)
        {
            fARB = isRWD ? 35.0 : isAWD ? 30.0 : 40.0;
            rARB = isRWD ? 50.0 : isAWD ? 45.0 : 40.0;
        }
        else if (isRally)
        {
            fARB = isFWD ? 10.0 : 8.0;
            rARB = isFWD ? 18.0 : isAWD ? 20.0 : 22.0;
        }
        else if (isRain || isSnow)
        {
            fARB = isFWD ? 8.0 : 5.0;
            rARB = isFWD ? 18.0 : 12.0;
        }
        else
        {
            if (isAWD)
            {
                fARB = 12.0 + Math.Round(pwr2wtNorm * 8.0);
                rARB = 50.0 + Math.Round(pwr2wtNorm * 10.0);
            }
            else if (isFWD)
            {
                fARB = 15.0 + Math.Round(pwr2wtNorm * 10.0);
                rARB = 50.0 + Math.Round(pwr2wtNorm * 10.0);
            }
            else
            {
                fARB = 8.0 + Math.Round(pwr2wtNorm * 14.0);
                rARB = 45.0 + Math.Round(pwr2wtNorm * 18.0);
            }
        }
        
        double arbFeel = (s.FeelAggression - 50.0) / 10.0;
        fARB = Math.Round(Math.Max(1.0, Math.Min(65.0, fARB - arbFeel)), 1);
        rARB = Math.Round(Math.Max(1.0, Math.Min(65.0, rARB + arbFeel)), 1);
        
        // Alignment
        double fCamber = isDrag ? 0.0 : isSnow ? -0.5 : isRain ? -0.8 : isDrift ? -2.5 : isRally ? -1.0 : -1.5;
        double rCamber = isDrag ? 0.0 : isSnow ? -0.3 : isRain ? -0.5 : isDrift ? -1.2 : isRally ? -0.8 : -1.0;
        
        if (isFWD)
        {
            fCamber = Math.Max(fCamber - 0.2, -2.0);
            rCamber = Math.Min(rCamber + 0.3, -0.2);
        }
        if (isRWD)
        {
            fCamber = Math.Max(fCamber - 0.3, -2.0);
        }
        if (isAWD)
        {
            double avg = (fCamber + rCamber) / 2.0;
            fCamber = Math.Round(avg - 0.1, 1);
            rCamber = Math.Round(avg + 0.1, 1);
        }
        
        double fToe = isDrag ? 0.0 : isDrift ? 0.2 : isRally ? 0.0 : -0.1;
        double rToe = isDrag ? 0.0 : isDrift ? -0.2 : isRally ? 0.1 : 0.1;
        if (isFWD)
        {
            fToe = isDrag ? 0.0 : -0.1;
            rToe = isDrag ? 0.0 : 0.2;
        }
        
        double caster = isSnow ? 5.5 : isDrift ? 6.5 : isDrag ? 6.0 : 7.0;
        
        // Tire Pressure
        double fpsi = s.PressureUnit == "bar" ? 1.85 : 26.5;
        double rpsi = fpsi;
        if (isRain || isSnow) { fpsi = s.PressureUnit == "bar" ? 1.75 : 25.5; rpsi = fpsi; }
        if (isRally) { fpsi = s.PressureUnit == "bar" ? 1.95 : 28.5; rpsi = fpsi; }
        if (isDrag) { fpsi = s.PressureUnit == "bar" ? 2.00 : 29.0; rpsi = s.PressureUnit == "bar" ? 1.55 : 22.5; }
        if (isDrift) { fpsi = s.PressureUnit == "bar" ? 2.15 : 31.0; rpsi = s.PressureUnit == "bar" ? 2.00 : 29.0; }
        
        if (s.Compound == "Race Slick" || s.Compound == "Race Semi-Slick")
        {
            fpsi += s.PressureUnit == "bar" ? 0.10 : 1.5;
            rpsi += s.PressureUnit == "bar" ? 0.05 : 0.8;
        }
        if (s.Compound == "Street")
        {
            fpsi -= s.PressureUnit == "bar" ? 0.10 : 1.5;
            rpsi -= s.PressureUnit == "bar" ? 0.10 : 1.5;
        }
        if (s.Compound == "Rally")
        {
            fpsi -= s.PressureUnit == "bar" ? 0.15 : 2.0;
            rpsi -= s.PressureUnit == "bar" ? 0.15 : 2.0;
        }
        if (s.Compound == "Snow")
        {
            fpsi -= s.PressureUnit == "bar" ? 0.20 : 3.0;
            rpsi -= s.PressureUnit == "bar" ? 0.20 : 3.0;
        }
        if (s.Compound == "Drag")
        {
            fpsi += s.PressureUnit == "bar" ? 0.05 : 0.5;
            rpsi -= s.PressureUnit == "bar" ? 0.20 : 3.0;
        }
        
        fpsi = Math.Round(fpsi, s.PressureUnit == "bar" ? 2 : 1);
        rpsi = Math.Round(rpsi, s.PressureUnit == "bar" ? 2 : 1);
        
        // Braking
        double brakeBal = isDrift ? 46.0 : isDrag ? 54.0 : (isRain || isSnow) ? 52.0 : isRally ? 54.0 : 56.0;
        brakeBal += Math.Round((frontPct - 0.5) * 20.0);
        if (isFWD) brakeBal += 4;
        if (isRWD) brakeBal -= 3;
        if (isWheel) brakeBal += 2;
        brakeBal = Math.Max(40.0, Math.Min(65.0, brakeBal));
        
        double brakePressure = isDrift ? 85.0 : isDrag ? 115.0 : (isRain || isSnow) ? 95.0 : isRally ? 95.0 : 100.0;
        int trailRating = isDrift ? 6 : isDrag ? 3 : isRain ? 7 : isRally ? 6 : isWheel ? 9 : 7;
        
        // Diff
        double pN = pwr2wtNorm;
        bool isHighPower = pwr2wt > 600.0;
        double fAccel = 0, fDecel = 0, rAccel = 0, rDecel = 0, center = 0;
        
        if (isFWD)
        {
            fAccel = isDrift ? 80 : isDrag ? 85 : isRally ? 65 : 85;
            fDecel = isDrift ? 0 : isDrag ? 5 : isRally ? 10 : 0;
        }
        else if (isRWD)
        {
            rAccel = isDrift ? 100 : isDrag ? 90 : isRally ? 60 : Math.Round(55.0 + pN * 20.0);
            rDecel = isDrift ? 10 : isDrag ? 5 : isRally ? 20 : Math.Round(10.0 + pN * 8.0);
        }
        else
        {
            fAccel = isDrift ? 30 : isDrag ? 15 : isRally ? 65 : 85;
            fDecel = isDrift ? 0 : isDrag ? 5 : isRally ? 5 : 0;
            rAccel = isDrift ? 85 : isDrag ? 90 : isRally ? 70 : Math.Round(55.0 + pN * 20.0);
            rDecel = isDrift ? 10 : isDrag ? 5 : isRally ? 15 : Math.Round(10.0 + pN * 5.0);
            center = isDrift ? 50 : isDrag ? 20 : isRally ? 55 : Math.Round(70.0 + pN * 8.0);
        }
        
        // Format strings
        Func<double, string> pStr = v => s.PressureUnit == "bar" ? $"{v} bar" : $"{v} psi";
        Func<double, string> sStr = v => $"{v} {s.SpringsUnit}";
        
        var tiresValues = new List<TuningItem>
        {
            new() { Key = "Front Pressure", Value = pStr(fpsi) },
            new() { Key = "Rear Pressure", Value = pStr(rpsi) },
            new() { Key = "Front Width", Value = s.TireWF.Contains("/") ? s.TireWF.Replace("mm", "") : $"{s.TireWF}mm" },
            new() { Key = "Rear Width", Value = s.TireWR.Contains("/") ? s.TireWR : $"{s.TireWR}mm" },
            new() { Key = "Compound", Value = s.Compound }
        };
        string tiresTip = isDrift ? "较低的后轮气压可以使车辆在加油时更可预测地突破抓地力。" : isRain ? "保持较低胎压 — 寒冷潮湿的柏油路面需要更大的接触面。" : "如果您在弯中感到推头，可以将前轮气压降低±0.5 psi。";
        
        var alignmentValues = new List<TuningItem>
        {
            new() { Key = "Front Camber", Value = $"{fCamber:F1}°" },
            new() { Key = "Rear Camber", Value = $"{rCamber:F1}°" },
            new() { Key = "Front Toe", Value = $"{fToe:F1}°" },
            new() { Key = "Rear Toe", Value = $"{rToe:F1}°" },
            new() { Key = "Front Caster", Value = $"{caster:F1}°" }
        };
        string alignmentTip = "每次以0.2°为单位调整外倾角 — 过多的外倾角会造成轮胎偏磨并破坏直道抓地力。";
        
        var suspensionValues = new List<TuningItem>
        {
            new() { Key = "Front Spring", Value = sStr(fSpring) },
            new() { Key = "Rear Spring", Value = sStr(rSpring) },
            new() { Key = "Front Ride Height", Value = s.WeightUnit == "kg" ? $"{fRide:F1} cm" : $"{(fRide/2.54):F1} in" },
            new() { Key = "Rear Ride Height", Value = s.WeightUnit == "kg" ? $"{rRide:F1} cm" : $"{(rRide/2.54):F1} in" }
        };
        string suspensionTip = isRally ? "优先考虑离地间隙而非空力 — 在泥地路面上车高比弹簧硬度更重要。" : "前部车高与后部相同或略低以保证高速稳定性。";
        
        var arbValues = new List<TuningItem>
        {
            new() { Key = "Front ARB", Value = fARB.ToString("F1") },
            new() { Key = "Rear ARB", Value = rARB.ToString("F1") }
        };
        string arbTip = "如果车辆在弯道入口处滑移：调软后防倾杆；如果推头：调软前防倾杆。";
        
        var dampingValues = new List<TuningItem>
        {
            new() { Key = "Front Rebound", Value = fRebound.ToString("F1") },
            new() { Key = "Rear Rebound", Value = rRebound.ToString("F1") },
            new() { Key = "Front Bump", Value = fBump.ToString("F1") },
            new() { Key = "Rear Bump", Value = rBump.ToString("F1") }
        };
        string dampingTip = "回弹阻尼永远高于收缩阻尼。在颠簸路面上下弹跳：调硬收缩阻尼。感到生硬/像木头：调软回弹阻尼。";
        
        var brakingValues = new List<TuningItem>
        {
            new() { Key = "Brake Balance", Value = $"{brakeBal}% F" },
            new() { Key = "Brake Pressure", Value = $"{brakePressure}%" },
            new() { Key = "Trail Brake Rating", Value = $"{trailRating}/10" }
        };
        string brakingTip = isWheel ? "循迹刹车：切弯时逐渐释放刹车 — 不要瞬间丢开。" : "在开启 ABS 时：踩到阈值压力即可，交由游戏来控制锁死。";
        
        var diffValues = new List<TuningItem>();
        if (isFWD)
        {
            diffValues.Add(new() { Key = "Front Accel", Value = $"{fAccel}%" });
            diffValues.Add(new() { Key = "Front Decel", Value = $"{fDecel}%" });
        }
        else if (isRWD)
        {
            diffValues.Add(new() { Key = "Rear Accel", Value = $"{rAccel}%" });
            diffValues.Add(new() { Key = "Rear Decel", Value = $"{rDecel}%" });
        }
        else
        {
            diffValues.Add(new() { Key = "Front Accel", Value = $"{fAccel}%" });
            diffValues.Add(new() { Key = "Front Decel", Value = $"{fDecel}%" });
            diffValues.Add(new() { Key = "Rear Accel", Value = $"{rAccel}%" });
            diffValues.Add(new() { Key = "Rear Decel", Value = $"{rDecel}%" });
            diffValues.Add(new() { Key = "Center Balance", Value = $"{center}% fwd" });
        }
        string diffTip = isDrift ? "高后加速锁有助于维持漂移角度。调整减速锁以控制切弯时的旋转度。" : 
                          isDrag && isRWD && isHighPower ? "警告：对于大马力后驱直线加速车，若抬头过猛，请调硬前防倾杆5点并降低后车高0.5 cm。" : 
                          isDrag ? "起步调校：高后轮加速锁定提供抓地力，低减速锁定避免换挡时锁死。" : 
                          isFWD ? "低前加速锁定减少扭矩转向。高减速锁帮助车辆在弯道入口旋转。" : 
                          "后轮加速锁定控制出弯牵引力。中心分配则可以改变车辆的动力输出性格。";

        // Gearing calculations
        TuningCategory? gearingCategory = null;
        if (s.IncludeGearing && (s.RedlineRpm > 0 && s.PeakTorqueRpm > 0 && s.Topspeed > 0 || isDrag))
        {
            double redline = s.RedlineRpm > 0 ? s.RedlineRpm : (isDrag ? (s.CarClass == "X" ? 10000 : s.CarClass == "S2" ? 9000 : 8000) : 7000);
            double peak = s.PeakTorqueRpm > 0 ? s.PeakTorqueRpm : (isDrag ? (s.CarClass == "X" ? 7500 : s.CarClass == "S2" ? 6500 : 5500) : 5000);
            double topSpeedLimit = s.Topspeed > 0 ? s.Topspeed : (s.SpeedUnit == "mph" ? 120 : 193);
            
            // Rear tire rolling circumference
            double tw = 275, ta = 35, tr = 19;
            try
            {
                var parts = s.TireWR.Split('/', 'R');
                if (parts.Length >= 3)
                {
                    tw = double.Parse(parts[0]);
                    ta = double.Parse(parts[1]);
                    tr = double.Parse(parts[2]);
                }
            }
            catch {}
            
            double sidewallMm = tw * (ta / 100.0);
            double wheelRadiusMm = (tr * 25.4 / 2.0) + sidewallMm;
            double circumferenceM = 2.0 * Math.PI * wheelRadiusMm / 1000.0;
            
            double topKmh = s.SpeedUnit == "mph" ? topSpeedLimit * 1.609 : topSpeedLimit;
            double finalDrive = 3.50;
            double[] ratios;
            
            if (isDrag)
            {
                string dist = s.DragDist ?? "quarter";
                double launchKmh = dist == "half" ? 130.0 : dist == "top" ? topKmh : 96.0;
                int maxGears = dist == "top" ? 6 : dist == "half" ? 5 : 4;
                int effGears = Math.Min(s.Gears, maxGears);
                if (effGears < 2) effGears = 2;
                
                double rawFD = (redline * circumferenceM * 3.6) / (launchKmh * 60.0);
                finalDrive = Math.Round(Math.Max(2.20, Math.Min(8.00, rawFD)), 2);
                
                double targetTotal1 = isAWD ? 15.0 - 1.5 * Math.Sqrt(torqueNm / wKg) :
                                      isFWD ? 13.5 - 2.5 * Math.Sqrt(torqueNm / wKg) :
                                              13.0 - 2.0 * Math.Sqrt(torqueNm / wKg);
                double ratio1 = targetTotal1 / finalDrive;
                ratio1 = Math.Round(Math.Max(2.20, Math.Min(4.80, ratio1)), 2);

                double ratioN = Math.Round((topKmh * 60.0) / (redline * circumferenceM * 3.6) * finalDrive, 2);
                double clampedN = Math.Round(Math.Max(0.70, Math.Min(1.20, ratioN)), 2);
                
                double p = effGears >= 7 ? 1.4 : 1.0;
                ratios = new double[effGears];
                for (int i = 0; i < effGears; i++)
                {
                    double x = effGears > 1 ? (double)i / (effGears - 1) : 0.0;
                    double y = 1.0 - Math.Pow(1.0 - x, p);
                    ratios[i] = Math.Round(ratio1 * Math.Pow(clampedN / ratio1, y), 2);
                }
            }
            else
            {
                double targetRpm = (redline + peak) / 2.0;
                double totalRatio = (targetRpm * circumferenceM * 3.6) / (topKmh * 60.0);
                
                int numGears = s.Gears;
                if (numGears < 2) numGears = 2;
                
                double nominalTopRatio = numGears >= 8 ? 0.65 : numGears >= 6 ? 0.75 : 0.82;
                double rawFD = totalRatio / nominalTopRatio;
                finalDrive = Math.Round(Math.Max(2.20, Math.Min(8.00, rawFD)), 2);
                
                double targetTotal1 = isAWD ? 14.0 - 2.0 * Math.Sqrt(torqueNm / wKg) :
                                      isFWD ? 12.5 - 3.0 * Math.Sqrt(torqueNm / wKg) :
                                              11.5 - 2.5 * Math.Sqrt(torqueNm / wKg);
                double ratio1 = targetTotal1 / finalDrive;
                ratio1 = Math.Round(Math.Max(2.20, Math.Min(4.50, ratio1)), 2);
                
                double ratioN = totalRatio / finalDrive;
                double clampedN = Math.Round(Math.Max(0.50, Math.Min(1.20, ratioN)), 2);
                
                double p = numGears >= 7 ? 1.4 : 1.0;
                ratios = new double[numGears];
                for (int i = 0; i < numGears; i++)
                {
                    double x = numGears > 1 ? (double)i / (numGears - 1) : 0.0;
                    double y = 1.0 - Math.Pow(1.0 - x, p);
                    ratios[i] = Math.Round(ratio1 * Math.Pow(clampedN / ratio1, y), 2);
                }
            }
            
            var gearingValues = new List<TuningItem>
            {
                new() { Key = "Final Drive", Value = finalDrive.ToString("F2") }
            };
            for (int i = 0; i < ratios.Length; i++)
            {
                string suffix = (i + 1) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
                gearingValues.Add(new() { Key = $"{i + 1}{suffix} Gear", Value = ratios[i].ToString("F2") });
            }
            
            gearingCategory = new TuningCategory
            {
                Values = gearingValues,
                Tip = isDrag ? "在1-3档保持较密齿比以加速起步，4档以上适当拉长齿比维持尾速。" : "合理分配各个档位，使出弯加速时转速始终处于发动机最佳出力区间。"
            };
        }

        // Aero calculations
        TuningCategory? aeroCategory = null;
        if (s.HasAero)
        {
            var aeroValues = new List<TuningItem>
            {
                new() { Key = "Front Downforce", Value = $"{s.AeroF} kg" },
                new() { Key = "Rear Downforce", Value = $"{s.AeroR} kg" },
                new() { Key = "Drag Cd", Value = s.DragCd.ToString("F2") }
            };
            double total = s.AeroF + s.AeroR;
            if (total > 0)
            {
                int fPct = (int)Math.Round(s.AeroF / total * 100);
                int rPct = 100 - fPct;
                aeroValues.Add(new() { Key = "Aero Balance", Value = $"{fPct}% F / {rPct}% R" });
            }
            else
            {
                aeroValues.Add(new() { Key = "Aero Balance", Value = "N/A" });
            }
            
            aeroCategory = new TuningCategory
            {
                Values = aeroValues,
                Tip = isDrag ? "直线加速：前压力调到最小，后压力设到最大以保证高速行驶时车尾安定，风阻系数 (Cd) 比平衡更关键。" : 
                      isWangan ? "湾岸线高速巡航：优先提高后压力以稳定车身，再根据手感微调前下压力。" : 
                      "偏置向后的下压力分配（如40前/60后）能在不产生推头的情况下让赛车牢牢贴合地面，如果在高速弯中感觉车尾漂移，应增加后压力。"
            };
        }
        
        return new TuningResult
        {
            Tires = new TuningCategory { Values = tiresValues, Tip = tiresTip },
            Gearing = gearingCategory,
            Alignment = new TuningCategory { Values = alignmentValues, Tip = alignmentTip },
            Suspension = new TuningCategory { Values = suspensionValues, Tip = suspensionTip },
            ARB = new TuningCategory { Values = arbValues, Tip = arbTip },
            Damping = new TuningCategory { Values = dampingValues, Tip = dampingTip },
            Braking = new TuningCategory { Values = brakingValues, Tip = brakingTip },
            Diff = new TuningCategory { Values = diffValues, Tip = diffTip },
            Aero = aeroCategory
        };
    }
}
