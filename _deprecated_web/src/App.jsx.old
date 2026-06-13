import { useState, useEffect, useCallback } from "react";

// ─── MATERIAL DESIGN 3 TOKEN SYSTEM ──────────────────────────────────────────
const MD3 = {
  // Surface hierarchy
  surface:    "#0f1117",
  surfaceVar: "#161820",
  surfaceC1:  "#1c1f2a",
  surfaceC2:  "#22263300",
  surfaceC3:  "#282d3a",
  outline:    "#2e3340",
  outlineVar: "#1e2230",
  // On-surface
  onSurface:      "#e2e4f0",
  onSurfaceVar:   "#9198ac",
  onSurfaceLow:   "#4a5068",
  // Primaries shift per tune mode — set dynamically
  primary:    "#4fc3f7",
  onPrimary:  "#002233",
  primaryC:   "#0d2233",
  // Semantic
  error:      "#f2453d",
  warning:    "#ffb74d",
  success:    "#66bb6a",
  // Elevation overlays (MD3 tonal elevation)
  elev1: "rgba(79,195,247,0.05)",
  elev2: "rgba(79,195,247,0.08)",
  elev3: "rgba(79,195,247,0.11)",
};

// Tune-mode color seeds → MD3 primary
const TUNE_SEEDS = {
  Race:"#2979ff", Drift:"#ef5350", Drag:"#ffd740", Rain:"#69f0ae",
  Rally:"#ff9800", Touge:"#ff6b35", Wangan:"#e040fb", General:"#90a4ae",
};

// ─── CONSTANTS ────────────────────────────────────────────────────────────────
const CAR_DB = {
  "Acura":["Integra Type R '01","NSX '17","RSX-S '02"],
  "Alfa Romeo":["4C '13","Giulia QV '16","GTA '65","Montreal '70"],
  "Aston Martin":["DB11 V12 '16","DB5 '64","DBS Superleggera '18","Vulcan AMR Pro '17","Vantage '19"],
  "Audi":["R8 V10 Plus '16","RS 6 Avant '20","RS3 Sedan '17","S1 '14","TT RS '18","Quattro '83","Sport Quattro '84"],
  "Bentley":["Continental GT Speed '21","Mulsanne Speed '17"],
  "BMW":["M2 Competition '19","M3 E30 '87","M3 E46 '01","M4 GTS '16","M5 CS '22","M8 Competition '20","2002 Turbo '73"],
  "Bugatti":["Chiron '18","Veyron SS '10","Divo '19","Bolide '21"],
  "Cadillac":["ATS-V '16","CT5-V Blackwing '22"],
  "Chevrolet":["Camaro SS '69","Camaro ZL1 '18","Corvette C8 '20","Corvette Grand Sport '17","Corvette Stingray '63"],
  "Dodge":["Challenger SRT Demon '18","Viper ACR '16","Charger SRT Hellcat '15"],
  "Ferrari":["488 Pista '19","812 Superfast '17","F40 '92","F50 '95","LaFerrari '13","SF90 Stradale '20","Enzo '02","250 GTO '62"],
  "Ford":["F-150 Raptor R '23","Focus RS '16","GT '17","GT '05","Mustang Shelby GT500 '20","Mustang Boss 302 '13","Sierra Cosworth RS500 '87"],
  "Honda":["Civic Type R '23","NSX '90","S2000 '09","Beat '91"],
  "Koenigsegg":["Agera RS '17","Jesko '20","Regera '16","One:1"],
  "Lamborghini":["Aventador SVJ '19","Countach '88","Huracan STO '21","Murcielago LP 670 SV '09","Urus '18"],
  "Lancia":["037 Stradale '82","Delta HF Integrale '92","Stratos HF '73"],
  "Land Rover":["Defender 90 '15","Range Rover SVR '18"],
  "Lotus":["Elise Series 1 '96","Exige S '06","Evora GT430 '17"],
  "Maserati":["GranTurismo MC '13","MC20 '21"],
  "Mazda":["MX-5 Miata '16","MX-5 NA '90","RX-7 FD '97","RX-7 Spirit R '02"],
  "McLaren":["600LT '19","720S '17","F1 '93","P1 '13","Senna '18"],
  "Mercedes-AMG":["A 45 S '19","C 63 S Coupe '16","E 63 S '18","GT Black Series '21","GT R '17","SLS AMG '11","One '21"],
  "Mitsubishi":["Eclipse GSX '95","Lancer Evo VI '99","Lancer Evo X '07","3000GT VR-4 '92"],
  "Nissan":["370Z '09","GT-R Premium '17","GT-R Black Edition '12","Silvia S15 '02","Skyline GT-R R32 '89","Skyline GT-R R34 '99","Z '23"],
  "Pagani":["Huayra BC '16","Zonda Cinque '09","Utopia '23"],
  "Porsche":["718 Cayman GT4 '20","911 Carrera RS '73","911 GT2 RS '18","911 GT3 RS '22","918 Spyder '14","Cayenne Turbo S '20","Taycan Turbo S '20"],
  "Renault":["Clio V6 Phase 2 '03","Megane RS Trophy '19"],
  "RUF":["CTR3 '07","CTR Anniversary '17","Yellowbird '87"],
  "Shelby":["Cobra 427 S/C '65","Cobra Daytona Coupe '64"],
  "Subaru":["BRZ '21","Impreza 22B '98","WRX STI '04","WRX STI S209 '19"],
  "Toyota":["GR86 '22","GR Supra '20","GR Yaris '20","MR2 GT '95","Supra RZ '98"],
  "TVR":["Cerbera Speed 12","Sagaris '06"],
  "Volkswagen":["Golf R '14","Golf GTI Clubsport S '16","Polo GTI '18"],
  "Volvo":["240 Turbo '84","850 R '96","S60 Polestar '17"],
};
const MANUFACTURERS = Object.keys(CAR_DB).sort();
const ENGINE_SWAPS  = ["None (Stock)","LS V8 (6.2L NA)","LS V8 (Supercharged)","LS V8 (Twin-Turbo)","2JZ-GTE (Single Turbo)","2JZ-GTE (Twin Turbo)","RB26DETT (Twin Turbo)","RB26 (Single Turbo)","Coyote 5.0 V8 (NA)","Coyote 5.0 V8 (Supercharged)","Barra I6 (Turbo)","SR20DET (Turbo)","K20/K24 (NA)","K24 (Turbo)","EJ257 Boxer (Turbo)","FA20 Boxer (NA)","VR38DETT (Twin Turbo)","CA18DET (Turbo)","4G63T (Turbo)","B16/B18 VTEC (NA)","Ferrari V12 (NA)","Lamborghini V10 (NA)","Dodge Hellcat V8 (SC)","Viper V10 (NA)","Electric Motor (Single)","Electric Motor (Dual AWD)"];
const ASPIRATIONS   = [{id:"na",label:"Naturally Aspirated",icon:"◎",desc:"Linear power — tune for response and high-rev character"},{id:"turbo",label:"Single Turbo",icon:"↺",desc:"Lag below spool, surge on boost — diff accel is critical"},{id:"twin",label:"Twin Turbo",icon:"↺↺",desc:"Lower lag, wider powerband — balance response and traction"},{id:"super",label:"Supercharged",icon:"▲",desc:"Instant torque, no lag — like NA with more traction demand"},{id:"electric",label:"Electric / Hybrid",icon:"⚡",desc:"Instant max torque — conservative diff, brake bias focus"}];
const ENGINE_CHARS  = [{id:"torquey",label:"Torquey",desc:"Strong low-end, wide band"},{id:"peaky",label:"Peaky",desc:"Builds to high RPM, narrow"},{id:"balanced",label:"Balanced",desc:"Even across the rev range"},{id:"laggy",label:"Laggy Turbo",desc:"Strong lag, dramatic on-boost surge"}];
const INPUT_DEVICES = [{id:"controller",label:"Controller",icon:"🎮",desc:"ABS-friendly brake bias, analog-optimised response"},{id:"wheel",label:"Racing Wheel",icon:"🏎",desc:"Progressive bias, trail braking tuned for linear pedal feel"},{id:"keyboard",label:"Keyboard",icon:"⌨",desc:"Wide stability margins — digital inputs need forgiveness"}];
const TUNE_MODES    = [{id:"Race",label:"Race",icon:"🏁"},{id:"Drift",label:"Drift",icon:"💨"},{id:"Drag",label:"Drag",icon:"⚡"},{id:"Rally",label:"Rally",icon:"🪨"},{id:"Touge",label:"Touge",icon:"⛰"},{id:"Wangan",label:"Wangan",icon:"🌃"},{id:"General",label:"General",icon:"🔧"},{id:"Rain",label:"Rain",icon:"🌧"}];
const CLASSES       = ["D","C","B","A","S1","S2","X"];
// FH5 verified PI ranges — update PI_VERSION when Playground patches class boundaries
const PI_VERSION    = "FH5_1.0";
const PI_RANGES     = {D:[100,500],C:[501,600],B:[601,700],A:[701,800],S1:[801,900],S2:[901,998],X:[999,999]};
// Per-class spring rate multipliers — high PI cars can handle stiffer springs
const CLASS_STIFFNESS = {D:0.70,C:0.80,B:0.90,A:1.00,S1:1.15,S2:1.30,X:1.45};
// Per-class ARB multipliers
const CLASS_ARB       = {D:0.75,C:0.82,B:0.90,A:1.00,S1:1.12,S2:1.22,X:1.35};
const DRIVE_TYPES   = ["FWD","RWD","AWD"];
const SURFACES      = ["Road","Dirt","Snow","Mixed"];
const TIRE_COMPOUNDS= ["Street","Sport","Race Semi-Slick","Race Slick","Rally","Snow","Drag"];
const TIRE_WF       = ["195","205","215","225","235","245","255","265","275","285","295","305","315"];
const TIRE_WR       = ["205","215","225","235","245","255","265","275","285","295","305","315","325","335","345","355"];
const TUNE_PAGES    = ["Tires","Gearing","Alignment","Suspension","ARB","Damping","Aero","Braking","Diff"];
const MAIN_TABS     = ["car","engine","setup","specs","tune"];
const AI_PROVIDERS  = [{id:"none",label:"Offline Only",icon:"⚙",color:"#90a4ae",free:true,desc:"Full tune via formula engine. Works offline, always free.",keyLabel:null,docsUrl:null},{id:"gemini",label:"Google Gemini",icon:"✦",color:"#4285f4",free:true,desc:"Free tier: 1,500 requests/day — best free option.",keyLabel:"Gemini API Key",keyHint:"AIza...",docsUrl:"https://aistudio.google.com/app/apikey"},{id:"grok",label:"xAI Grok",icon:"𝕏",color:"#e7e7e7",free:true,desc:"Free tier available. Fast, strong reasoning.",keyLabel:"Grok API Key",keyHint:"xai-...",docsUrl:"https://console.x.ai"},{id:"openai",label:"OpenAI GPT-4o",icon:"◈",color:"#10a37f",free:false,desc:"GPT-4o-mini is ~$0.0002/tune. Reliable JSON.",keyLabel:"OpenAI API Key",keyHint:"sk-...",docsUrl:"https://platform.openai.com/api-keys"},{id:"claude",label:"Claude Haiku",icon:"◇",color:"#4fc3f7",free:false,desc:"Best analysis quality. ~$0.0003/tune.",keyLabel:"Anthropic Key",keyHint:"sk-ant-api03-...",docsUrl:"https://console.anthropic.com"}];
const PROBLEMS      = [{id:"understeer",label:"Understeer",icon:"↖",desc:"Car pushes wide, won't turn in",subs:[{id:"us_entry",label:"On corner entry"},{id:"us_mid",label:"Mid-corner"},{id:"us_exit",label:"On exit / throttle"},{id:"us_braking",label:"While trail braking"},{id:"us_high",label:"High speed only"}]},{id:"oversteer",label:"Oversteer",icon:"↗",desc:"Rear steps out or snaps",subs:[{id:"os_entry",label:"Corner entry / braking"},{id:"os_mid",label:"Mid-corner snap"},{id:"os_exit",label:"On throttle exit"},{id:"os_accel",label:"Hard acceleration"},{id:"os_hi",label:"High speed instability"}]},{id:"braking",label:"Braking Instability",icon:"⊗",desc:"Dives, locks up, or pulls",subs:[{id:"br_lock",label:"Front locking up"},{id:"br_rear",label:"Rear locking / spinning"},{id:"br_dive",label:"Excessive nose dive"},{id:"br_turn",label:"Can't trail brake into corners"},{id:"br_late",label:"Braking distance too long"}]},{id:"sluggish",label:"Unresponsive / Slow",icon:"◎",desc:"Car feels numb or sluggish",subs:[{id:"ur_steer",label:"Steering feels numb"},{id:"ur_roll",label:"Too much body roll"},{id:"ur_trac",label:"Poor traction off the line"},{id:"ur_boost",label:"Turbo lag / boost hesitation"},{id:"ur_gear",label:"Wrong gear at wrong speed"}]},{id:"twitchy",label:"Twitchy / Sliding",icon:"≋",desc:"Car feels nervous or unpredictable",subs:[{id:"tw_str",label:"Nervous on straights"},{id:"tw_bump",label:"Bouncy over bumps"},{id:"tw_snap",label:"Snaps unexpectedly"},{id:"tw_rear",label:"Rear constantly sliding"},{id:"tw_stiff",label:"Too stiff / harsh ride"}]}];
const UNIT_PRESETS = {
  imperial: { weight:"lbs", springs:"lbs/in", pressure:"psi", speed:"mph",  power:"hp", label:"Imperial", sub:"lbs · lbs/in · PSI · mph · hp" },
  metric:   { weight:"kg",  springs:"kgf/mm", pressure:"bar", speed:"km/h", power:"ps", label:"Metric",   sub:"kg · kgf/mm · bar · km/h · PS"  },
};
// 1 PS (Pferdestärke) = 0.98632 hp — Forza Japan shows PS in metric mode
const PS_TO_HP = 0.98632;
const HP_TO_PS = 1.01387;
// Individual options kept for legacy/conversion use
const UNIT_OPTIONS  = {weight:[{v:"lbs",l:"LBS"},{v:"kg",l:"KG"}],springs:[{v:"lbs/in",l:"LBS/IN"},{v:"n/mm",l:"N/MM"},{v:"kgf/mm",l:"KGF/MM"}],pressure:[{v:"psi",l:"PSI"},{v:"bar",l:"BAR"}],speed:[{v:"mph",l:"MPH"},{v:"km/h",l:"KM/H"}]};

// ─── CAR KNOWLEDGE DATABASE ───────────────────────────────────────────────────
// Stock defaults per car: drive type, aspiration, engine character, weight(lbs),
// hp, torque(lb-ft), top speed(mph), redline RPM, gears
// Users can override everything — this just pre-fills sensible starting values.
const CAR_KNOWLEDGE = {
  // Acura
  "Integra Type R '01":   {drive:"FWD",asp:"na",char:"peaky",   w:2641,hp:195, tq:131,  spd:137,rpm:8400,g:5},
  "NSX '17":              {drive:"AWD",asp:"twin",char:"balanced",w:3307,hp:573,tq:476,  spd:191,rpm:7500,g:9},
  "RSX-S '02":            {drive:"FWD",asp:"na",char:"peaky",   w:2822,hp:201,tq:141,   spd:135,rpm:8000,g:6},
  // Alfa Romeo
  "4C '13":               {drive:"RWD",asp:"turbo",char:"peaky",  w:2315,hp:237,tq:258, spd:160,rpm:6500,g:6},
  "Giulia QV '16":        {drive:"RWD",asp:"twin",char:"torquey", w:3790,hp:505,tq:443, spd:191,rpm:7000,g:8},
  // Aston Martin
  "DB11 V12 '16":         {drive:"RWD",asp:"twin",char:"torquey", w:3946,hp:600,tq:516, spd:200,rpm:6500,g:8},
  "DB5 '64":              {drive:"RWD",asp:"na",char:"torquey",   w:3306,hp:286,tq:280, spd:145,rpm:5500,g:4},
  "Vulcan AMR Pro '17":   {drive:"RWD",asp:"na",char:"peaky",    w:2756,hp:820,tq:575, spd:210,rpm:7500,g:6},
  "Vantage '19":          {drive:"RWD",asp:"twin",char:"torquey", w:3638,hp:503,tq:505, spd:195,rpm:6500,g:8},
  // Audi
  "R8 V10 Plus '16":      {drive:"AWD",asp:"na",char:"peaky",    w:3583,hp:610,tq:413, spd:205,rpm:8250,g:7},
  "RS 6 Avant '20":       {drive:"AWD",asp:"twin",char:"torquey", w:4938,hp:591,tq:590, spd:190,rpm:6000,g:8},
  "RS3 Sedan '17":        {drive:"AWD",asp:"turbo",char:"torquey",w:3946,hp:400,tq:354, spd:155,rpm:5500,g:7},
  "S1 '14":               {drive:"AWD",asp:"turbo",char:"torquey",w:2734,hp:231,tq:273, spd:150,rpm:5500,g:6},
  "TT RS '18":            {drive:"AWD",asp:"turbo",char:"torquey",w:3461,hp:394,tq:354, spd:174,rpm:5850,g:7},
  "Quattro '83":          {drive:"AWD",asp:"turbo",char:"torquey",w:2866,hp:200,tq:210, spd:137,rpm:5500,g:5},
  "Sport Quattro '84":    {drive:"AWD",asp:"turbo",char:"torquey",w:2866,hp:306,tq:258, spd:155,rpm:5500,g:5},
  // BMW
  "M2 Competition '19":   {drive:"RWD",asp:"twin",char:"balanced",w:3572,hp:405,tq:406, spd:155,rpm:7000,g:7},
  "M3 E30 '87":           {drive:"RWD",asp:"na",char:"peaky",    w:2756,hp:200,tq:170, spd:146,rpm:7200,g:5},
  "M3 E46 '01":           {drive:"RWD",asp:"na",char:"peaky",    w:3263,hp:338,tq:262, spd:155,rpm:8000,g:6},
  "M4 GTS '16":           {drive:"RWD",asp:"twin",char:"peaky",  w:3495,hp:493,tq:443, spd:186,rpm:7300,g:7},
  "M5 CS '22":            {drive:"AWD",asp:"twin",char:"torquey", w:4277,hp:627,tq:553, spd:190,rpm:6250,g:8},
  "M8 Competition '20":   {drive:"AWD",asp:"twin",char:"torquey", w:4277,hp:617,tq:553, spd:190,rpm:6250,g:8},
  "2002 Turbo '73":       {drive:"RWD",asp:"turbo",char:"laggy",  w:2183,hp:170,tq:173, spd:130,rpm:5800,g:4},
  // Bugatti
  "Chiron '18":           {drive:"AWD",asp:"twin",char:"torquey", w:4398,hp:1479,tq:1180,spd:261,rpm:6700,g:7},
  "Veyron SS '10":        {drive:"AWD",asp:"twin",char:"torquey", w:4189,hp:1184,tq:1106,spd:258,rpm:6400,g:7},
  "Bolide '21":           {drive:"AWD",asp:"twin",char:"peaky",  w:2734,hp:1824,tq:1364,spd:310,rpm:7000,g:7},
  // Chevrolet
  "Camaro SS '69":        {drive:"RWD",asp:"na",char:"torquey",  w:3675,hp:396,tq:415, spd:140,rpm:5500,g:4},
  "Camaro ZL1 '18":       {drive:"RWD",asp:"super",char:"torquey",w:3917,hp:650,tq:650, spd:202,rpm:6500,g:6},
  "Corvette C8 '20":      {drive:"RWD",asp:"na",char:"balanced", w:3366,hp:490,tq:470, spd:194,rpm:8500,g:8},
  "Corvette Grand Sport '17":{drive:"RWD",asp:"na",char:"torquey",w:3524,hp:460,tq:465,spd:185,rpm:6600,g:7},
  "Corvette Stingray '63":{drive:"RWD",asp:"na",char:"torquey",  w:3019,hp:360,tq:352, spd:145,rpm:5500,g:4},
  // Dodge
  "Challenger SRT Demon '18":{drive:"RWD",asp:"super",char:"torquey",w:4254,hp:840,tq:770,spd:168,rpm:6500,g:8},
  "Viper ACR '16":        {drive:"RWD",asp:"na",char:"torquey",  w:3374,hp:645,tq:600, spd:177,rpm:6200,g:6},
  "Charger SRT Hellcat '15":{drive:"RWD",asp:"super",char:"torquey",w:4575,hp:707,tq:650,spd:204,rpm:6200,g:8},
  // Ferrari
  "488 Pista '19":        {drive:"RWD",asp:"twin",char:"peaky",  w:2888,hp:711,tq:568, spd:211,rpm:8000,g:7},
  "812 Superfast '17":    {drive:"RWD",asp:"na",char:"peaky",    w:3550,hp:789,tq:530, spd:211,rpm:8500,g:7},
  "F40 '92":              {drive:"RWD",asp:"twin",char:"laggy",  w:2425,hp:478,tq:424, spd:201,rpm:7000,g:5},
  "LaFerrari '13":        {drive:"RWD",asp:"na",char:"peaky",    w:3042,hp:949,tq:664, spd:217,rpm:9250,g:7},
  "SF90 Stradale '20":    {drive:"AWD",asp:"twin",char:"balanced",w:3461,hp:986,tq:590, spd:211,rpm:8000,g:8},
  "Enzo '02":             {drive:"RWD",asp:"na",char:"peaky",    w:3263,hp:651,tq:485, spd:217,rpm:8200,g:6},
  "250 GTO '62":          {drive:"RWD",asp:"na",char:"peaky",    w:2050,hp:300,tq:220, spd:174,rpm:7500,g:5},
  // Ford
  "Focus RS '16":         {drive:"AWD",asp:"turbo",char:"torquey",w:3196,hp:350,tq:350, spd:165,rpm:6000,g:6},
  "GT '17":               {drive:"RWD",asp:"twin",char:"peaky",  w:3054,hp:647,tq:550, spd:216,rpm:6500,g:7},
  "Mustang Shelby GT500 '20":{drive:"RWD",asp:"super",char:"torquey",w:3900,hp:760,tq:625,spd:180,rpm:7500,g:7},
  "Mustang Boss 302 '13": {drive:"RWD",asp:"na",char:"peaky",    w:3606,hp:444,tq:380, spd:155,rpm:7500,g:6},
  // Honda
  "Civic Type R '23":     {drive:"FWD",asp:"turbo",char:"peaky", w:3306,hp:315,tq:310, spd:169,rpm:6500,g:6},
  "NSX '90":              {drive:"RWD",asp:"na",char:"peaky",    w:2712,hp:270,tq:210, spd:168,rpm:8000,g:5},
  "S2000 '09":            {drive:"RWD",asp:"na",char:"peaky",    w:2822,hp:240,tq:162, spd:150,rpm:9000,g:6},
  // Koenigsegg
  "Agera RS '17":         {drive:"RWD",asp:"twin",char:"torquey", w:3130,hp:1360,tq:1011,spd:278,rpm:7800,g:7},
  "Jesko '20":            {drive:"RWD",asp:"twin",char:"peaky",  w:3131,hp:1600,tq:1106,spd:300,rpm:8500,g:9},
  "Regera '16":           {drive:"RWD",asp:"twin",char:"torquey", w:3571,hp:1500,tq:2000,spd:255,rpm:7800,g:1},
  // Lamborghini
  "Aventador SVJ '19":    {drive:"AWD",asp:"na",char:"peaky",    w:3500,hp:770,tq:531, spd:217,rpm:8500,g:7},
  "Countach '88":         {drive:"RWD",asp:"na",char:"peaky",    w:3131,hp:455,tq:369, spd:183,rpm:7200,g:5},
  "Huracan STO '21":      {drive:"RWD",asp:"na",char:"peaky",    w:2952,hp:631,tq:417, spd:193,rpm:8500,g:7},
  "Urus '18":             {drive:"AWD",asp:"twin",char:"torquey", w:5141,hp:641,tq:627, spd:190,rpm:6000,g:8},
  // Lotus
  "Elise Series 1 '96":   {drive:"RWD",asp:"na",char:"peaky",   w:1521,hp:120,tq:114,  spd:126,rpm:7800,g:5},
  "Exige S '06":          {drive:"RWD",asp:"super",char:"peaky", w:2007,hp:220,tq:160,  spd:148,rpm:8000,g:6},
  // Mazda
  "MX-5 Miata '16":       {drive:"RWD",asp:"na",char:"peaky",   w:2332,hp:155,tq:148,  spd:130,rpm:7500,g:6},
  "MX-5 NA '90":          {drive:"RWD",asp:"na",char:"peaky",   w:2182,hp:116,tq:100,  spd:120,rpm:7200,g:5},
  "RX-7 FD '97":          {drive:"RWD",asp:"twin",char:"peaky", w:2865,hp:276,tq:231,  spd:156,rpm:8000,g:5},
  "RX-7 Spirit R '02":    {drive:"RWD",asp:"twin",char:"peaky", w:2932,hp:280,tq:238,  spd:156,rpm:8000,g:5},
  // McLaren
  "600LT '19":            {drive:"RWD",asp:"twin",char:"peaky",  w:2749,hp:592,tq:457, spd:204,rpm:7500,g:7},
  "720S '17":             {drive:"RWD",asp:"twin",char:"peaky",  w:2829,hp:710,tq:568, spd:212,rpm:7500,g:7},
  "F1 '93":               {drive:"RWD",asp:"na",char:"peaky",    w:2510,hp:627,tq:479, spd:240,rpm:7500,g:6},
  "P1 '13":               {drive:"RWD",asp:"twin",char:"torquey", w:3075,hp:903,tq:664, spd:217,rpm:7500,g:7},
  "Senna '18":            {drive:"RWD",asp:"twin",char:"peaky",  w:2641,hp:789,tq:590, spd:208,rpm:8000,g:7},
  // Mercedes-AMG
  "GT Black Series '21":  {drive:"RWD",asp:"twin",char:"peaky",  w:3373,hp:720,tq:590, spd:202,rpm:7200,g:7},
  "GT R '17":             {drive:"RWD",asp:"twin",char:"torquey", w:3571,hp:577,tq:516, spd:198,rpm:6500,g:7},
  "SLS AMG '11":          {drive:"RWD",asp:"na",char:"peaky",    w:3571,hp:563,tq:479, spd:197,rpm:6800,g:7},
  "One '21":              {drive:"RWD",asp:"turbo",char:"peaky", w:3267,hp:1063,tq:848,spd:219,rpm:11000,g:8},
  // Mitsubishi
  "Lancer Evo VI '99":    {drive:"AWD",asp:"turbo",char:"laggy", w:2866,hp:276,tq:260, spd:150,rpm:6500,g:5},
  "Lancer Evo X '07":     {drive:"AWD",asp:"turbo",char:"torquey",w:3384,hp:291,tq:300,spd:155,rpm:6500,g:6},
  "3000GT VR-4 '92":      {drive:"AWD",asp:"twin",char:"torquey", w:3792,hp:320,tq:315, spd:155,rpm:6500,g:6},
  // Nissan
  "370Z '09":             {drive:"RWD",asp:"na",char:"balanced", w:3232,hp:332,tq:270, spd:155,rpm:7500,g:7},
  "GT-R Premium '17":     {drive:"AWD",asp:"twin",char:"torquey", w:3858,hp:565,tq:467, spd:196,rpm:6800,g:6},
  "GT-R Black Edition '12":{drive:"AWD",asp:"twin",char:"torquey",w:3836,hp:545,tq:463,spd:193,rpm:6800,g:6},
  "Silvia S15 '02":       {drive:"RWD",asp:"turbo",char:"torquey",w:2734,hp:247,tq:224, spd:149,rpm:6800,g:6},
  "Skyline GT-R R32 '89": {drive:"AWD",asp:"twin",char:"torquey", w:3042,hp:276,tq:266, spd:156,rpm:7000,g:5},
  "Skyline GT-R R34 '99": {drive:"AWD",asp:"twin",char:"torquey", w:3107,hp:276,tq:289, spd:156,rpm:6800,g:6},
  "Z '23":                {drive:"RWD",asp:"twin",char:"balanced",w:3306,hp:400,tq:350, spd:155,rpm:6400,g:9},
  // Pagani
  "Huayra BC '16":        {drive:"RWD",asp:"twin",char:"torquey", w:2778,hp:789,tq:738, spd:238,rpm:6500,g:7},
  "Utopia '23":           {drive:"RWD",asp:"twin",char:"torquey", w:2866,hp:857,tq:811, spd:230,rpm:6500,g:7},
  // Porsche
  "718 Cayman GT4 '20":   {drive:"RWD",asp:"na",char:"peaky",    w:3065,hp:414,tq:309, spd:188,rpm:8000,g:6},
  "911 Carrera RS '73":   {drive:"RWD",asp:"na",char:"peaky",    w:2094,hp:210,tq:188, spd:149,rpm:7200,g:5},
  "911 GT2 RS '18":       {drive:"RWD",asp:"twin",char:"torquey", w:3241,hp:700,tq:553, spd:211,rpm:7000,g:7},
  "911 GT3 RS '22":       {drive:"RWD",asp:"na",char:"peaky",    w:3153,hp:518,tq:346, spd:185,rpm:9000,g:7},
  "918 Spyder '14":       {drive:"AWD",asp:"na",char:"peaky",    w:3682,hp:887,tq:944, spd:214,rpm:9150,g:7},
  "Taycan Turbo S '20":   {drive:"AWD",asp:"electric",char:"torquey",w:5390,hp:750,tq:774,spd:162,rpm:16000,g:2},
  // Subaru
  "BRZ '21":              {drive:"RWD",asp:"na",char:"balanced", w:2811,hp:228,tq:184, spd:143,rpm:7400,g:6},
  "Impreza 22B '98":      {drive:"AWD",asp:"turbo",char:"torquey",w:2954,hp:276,tq:253, spd:150,rpm:6500,g:5},
  "WRX STI '04":          {drive:"AWD",asp:"turbo",char:"torquey",w:3373,hp:300,tq:300, spd:158,rpm:6500,g:6},
  "WRX STI S209 '19":     {drive:"AWD",asp:"turbo",char:"torquey",w:3494,hp:341,tq:330, spd:155,rpm:6000,g:6},
  // Toyota
  "GR86 '22":             {drive:"RWD",asp:"na",char:"balanced", w:2818,hp:228,tq:184, spd:143,rpm:7400,g:6},
  "GR Supra '20":         {drive:"RWD",asp:"turbo",char:"torquey",w:3397,hp:382,tq:368, spd:155,rpm:6500,g:8},
  "GR Yaris '20":         {drive:"AWD",asp:"turbo",char:"torquey",w:2756,hp:257,tq:266, spd:143,rpm:6500,g:6},
  "MR2 GT '95":           {drive:"RWD",asp:"turbo",char:"peaky", w:2678,hp:245,tq:224, spd:149,rpm:7000,g:5},
  "Supra RZ '98":         {drive:"RWD",asp:"twin",char:"torquey", w:3395,hp:320,tq:315, spd:156,rpm:6500,g:6},
  // Volkswagen
  "Golf R '14":           {drive:"AWD",asp:"turbo",char:"torquey",w:3153,hp:296,tq:280, spd:155,rpm:6200,g:6},
};

// Returns known data for a model, or null
const getCarKnowledge = (model) => CAR_KNOWLEDGE[model] || null;


// ─── DRIVETRAIN VALIDATION ────────────────────────────────────────────────────
// Some drivetrain+car combos are impossible in Forza — catch and correct them.
// EV swaps are not in FH5/FH6. Certain cars can't go FWD (physics/game limitation).
const DRIVE_LOCK = {
  // Cars that are chassis-locked to RWD — can't be converted to FWD in game
  "GT-R Premium '17":     ["RWD","AWD"],
  "GT-R Black Edition '12":["RWD","AWD"],
  "Skyline GT-R R32 '89": ["RWD","AWD"],
  "Skyline GT-R R34 '99": ["RWD","AWD"],
  "Aventador SVJ '19":    ["RWD","AWD"],
  "Huracan STO '21":      ["RWD","AWD"],
  "SF90 Stradale '20":    ["RWD","AWD"],
  "Chiron '18":           ["RWD","AWD"],
  "Veyron SS '10":        ["RWD","AWD"],
  "Jesko '20":            ["RWD"],
  "Agera RS '17":         ["RWD"],
  "Taycan Turbo S '20":   ["AWD"],
};
// EV swap is not a real option in Forza — flag it
const INVALID_SWAPS = ["Electric Motor (Single)","Electric Motor (Dual AWD)"];

// Returns { valid: bool, correctedDrive: string|null, warning: string|null }
function validateConfig(model, driveType, engineSwap) {
  const warnings = [];
  let correctedDrive = null;

  // EV swap check
  if(INVALID_SWAPS.includes(engineSwap)) {
    warnings.push("Electric motor swaps are not available in Forza Horizon. Swap reset to Stock.");
  }

  // Drive type lock check
  const allowed = DRIVE_LOCK[model];
  if(allowed && !allowed.includes(driveType)) {
    correctedDrive = allowed[0];
    warnings.push(`${model} cannot run ${driveType} in-game. Corrected to ${correctedDrive}.`);
  }

  // Class X with FWD is technically possible but almost never optimal — warn only
  return { valid: warnings.length===0, correctedDrive, warnings };
}

// ─── LOCAL STORAGE ────────────────────────────────────────────────────────────
const LS = {
  get:(k,d)=>{try{const v=localStorage.getItem(k);return v!=null?JSON.parse(v):d;}catch{return d;}},
  set:(k,v)=>{try{localStorage.setItem(k,JSON.stringify(v));}catch{}},
};

// ─── PHYSICS ENGINE ───────────────────────────────────────────────────────────
// Real suspension mathematics — not lookup tables
const clamp = (v,mn,mx) => Math.max(mn,Math.min(mx,v));
const round1 = v => Math.round(v*10)/10;
const round2 = v => Math.round(v*100)/100;

function calcTune(s) {
  const { tuneId, driveType, surface, aspiration, inputDevice,
          weight, hp, torque, topspeed, gears, rpm,
          tireWF, tireWR, compound,
          hasAero, aeroF, aeroR, dragCd, units } = s;

  // Unit normalization to SI
  const weightKg   = units.weight==="lbs" ? weight/2.205 : weight;
  // If user entered PS, convert to HP for physics (1PS = 0.98632HP)
  const hpForCalc  = (units.power==="ps") ? hp*PS_TO_HP : hp;
  const speedKmh   = units.speed==="mph"  ? topspeed*1.609 : topspeed;
  const torqueNm   = units.weight==="lbs" ? torque*1.356   : torque;
  const pUnit      = units.pressure;
  const sUnit      = units.springs;
  const redlineRPM = rpm || (aspiration==="electric"?16000:aspiration==="na"||aspiration==="super"?7800:6500);

  // Flags
  const isDrift   = tuneId==="Drift";
  const isDrag    = tuneId==="Drag";
  const isRain    = tuneId==="Rain";
  const isRally   = tuneId==="Rally";
  const isTouge   = tuneId==="Touge";
  const isWangan  = tuneId==="Wangan";
  const isFWD     = driveType==="FWD";
  const isRWD     = driveType==="RWD";
  const isAWD     = driveType==="AWD";
  const isDirt    = surface==="Dirt"||surface==="Mixed";
  const isSnow    = surface==="Snow";
  const isElec    = aspiration==="electric";
  const isWheel   = inputDevice==="wheel";
  const pwr2wt    = hpForCalc/(weightKg/1000); // hp/tonne
  const p2wNorm   = clamp(pwr2wt/800,0,1);

  // Weight distribution estimate from drivetrain
  const frontWtPct = isFWD ? 0.63 : isRWD ? 0.52 : 0.57; // approximate
  const frontWt    = weightKg * frontWtPct;
  const rearWt     = weightKg * (1-frontWtPct);

  // ── TIRES ──────────────────────────────────────────────────────────────────
  // Base pressure from compound + surface contact patch needs
  const pBase = pUnit==="bar" ? 1.9 : 27.5;
  let fp = isWangan?(pUnit==="bar"?2.05:29.8):pBase;
  let rp = isWangan?(pUnit==="bar"?2.05:29.8):pBase;
  // Surface adjustments — wet/cold = more contact area needed = lower pressure
  if(isSnow)            { fp = pUnit==="bar"?1.55:22.5; rp=fp; }
  else if(isRain)       { fp = pUnit==="bar"?1.70:24.7; rp=fp; }
  else if(isDirt)       { fp = pUnit==="bar"?1.65:24.0; rp=fp; }
  // Tune adjustments
  if(isDrag)            { fp = pUnit==="bar"?1.90:27.5; rp = pUnit==="bar"?1.50:21.8; } // stagger
  if(isDrift)           { fp = pUnit==="bar"?2.05:29.8; rp = pUnit==="bar"?1.72:25.0; }
  // Compound fine-tune
  const compAdj = {Street:-1.5,Sport:-0.5,"Race Semi-Slick":0.5,"Race Slick":1.2,Rally:-0.5,Snow:-2.5,Drag:0.8};
  const adj = pUnit==="bar" ? (compAdj[compound]||0)/14.5 : (compAdj[compound]||0);
  fp = round1(clamp(fp+adj, pUnit==="bar"?1.2:17, pUnit==="bar"?3.2:47));
  rp = round1(clamp(rp+(adj*0.7), pUnit==="bar"?1.2:17, pUnit==="bar"?3.2:47));

  // ── GEARING — geometric progression with physics-based final drive ─────────
  // Estimated tire rolling radius from width (approximation)
  const tireRadius   = (parseFloat(tireWR)/1000)*0.3 + 0.26; // rough: width*aspect+rim
  // Target RPM at top speed (typical Forza engine: 6500–8500 RPM at top gear)
  // Final drive: FD = (speedKmh/3.6 × 60) / (RPM × 2π × tireRadius)
  let finalDrive = (speedKmh/3.6*60) / (redlineRPM * 2*Math.PI * tireRadius / 60);
  finalDrive = round2(clamp(finalDrive, isDrag?2.8:isWangan?2.5:3.0, isRally?4.8:4.5));
  // Adjust for tune type
  if(isDrag)  finalDrive = round2(finalDrive*1.12); // shorter for launch
  if(isWangan) finalDrive = round2(finalDrive*0.88); // longer for highway top speed
  if(isRally) finalDrive = round2(finalDrive*0.95); // slightly longer
  if(isDrift) finalDrive = round2(finalDrive*1.05); // keep mid-range accessible

  // Geometric gear ratio progression
  // First gear from stall torque multiplication needed
  const firstGear = clamp(3.2 + p2wNorm*0.8, isDrag?3.8:3.2, 4.6);
  // Last gear = overdrive if race/general, 1:1 if drag
  const lastGear  = isDrag ? 1.0 : isRally ? 0.82 : isWangan ? 0.62 : 0.72;
  // Geometric ratio: r_n = first * (last/first)^(n/(gears-1))
  const gearRatios = Array.from({length:gears},(_,i)=>{
    const ratio = firstGear * Math.pow(lastGear/firstGear, i/(gears-1));
    // Tighten lower gears for drag, loosen for drift entry
    const adj2 = isDrag ? (i<3?1.06:1.0) : isDrift ? (i===0?0.94:1.0) : isWangan ? (i>gears-3?0.93:1.0) : 1.0;
    return round2(ratio*adj2);
  });
  // Estimated speeds at redline per gear (for display)
  const gearSpeeds = gearRatios.map(gr=>{
    const spd = (redlineRPM * 2*Math.PI * tireRadius * 60) / (gr * finalDrive * 60) * 3.6;
    return units.speed==="mph" ? Math.round(spd/1.609) : Math.round(spd);
  });

  // ── ALIGNMENT ───────────────────────────────────────────────────────────────
  // Camber from lateral load target — more negative = more lateral G capacity
  const lateralG  = isDrag?0.8:isRally?0.9:isSnow?0.6:isRain?0.75:isDrift?1.1:1.0;
  let fCamber = isDrag ? 0.0 : round1(-(lateralG * 1.8 + (isFWD?0.4:0)));
  let rCamber = isDrag ? 0.0 : round1(-(lateralG * 1.3 - (isRWD?0.2:0)));
  fCamber = clamp(fCamber, -4.0, 0.0);
  rCamber = clamp(rCamber, -3.0, 0.0);
  // Toe — stability vs rotation
  const fToe = isDrag?0.0:isFWD?round1(-0.1-lateralG*0.05):isDrift?0.2:0.1;
  const rToe = isDrag?0.0:isDrift?-0.2:round1(0.1+lateralG*0.1);
  // Caster — steering feel, trail braking self-centering
  const isRace  = tuneId==="Race"||tuneId==="General";
  const caster  = isDrift?7.0:isWheel?6.0:isRace?5.5:isRally?4.5:5.0;

  // ── SUSPENSION — natural frequency method ──────────────────────────────────
  // Target frequencies (Hz): race 2.0–2.5, street 1.2–1.5, rally 0.9–1.3
  const fFreq = isDrag?2.8:isDrift?2.4:isRally?1.1:isSnow?1.0:isRain?1.5:2.0;
  const rFreq = isDrift?1.6:isRally?1.0:isSnow?0.95:isRain?1.4:fFreq-0.1;
  // Spring rate (N/mm) = (2π×f)² × cornerMass(kg) / 1000
  const cornerMassF = (frontWt/2);  // per corner
  const cornerMassR = (rearWt/2);
  let fSpringNmm = Math.pow(2*Math.PI*fFreq,2)*cornerMassF/1000;
  let rSpringNmm = Math.pow(2*Math.PI*rFreq,2)*cornerMassR/1000;
  // Convert to output unit
  const springConv  = sUnit==="lbs/in"?5.711:sUnit==="kgf/mm"?0.102:1.0;
  const classMult   = CLASS_STIFFNESS[s.carClass||"A"] || 1.0;
  let fSpring = round1(fSpringNmm*springConv*classMult);
  let rSpring = round1(rSpringNmm*springConv*classMult);
  // Ride height — lower for race, higher for rally/offroad
  const fRide = round1(isDrift?3.3:isDrag?2.9:isRally?5.8:isSnow?6.5:isRain?3.8:isTouge?4.0:3.5);
  const rRide = round1(isDrift?3.1:isDrag?2.6:isRally?5.5:isSnow?6.2:isRain?3.6:isTouge?3.8:3.3);

  // ── ARB ─────────────────────────────────────────────────────────────────────
  // ARB controls roll stiffness distribution — affects handling balance
  const rollStiffBase = (fSpringNmm+rSpringNmm)*0.5;
  let fARB = clamp(isDrift?28+p2wNorm*18:isDrag?10:isRally?16:isRain?14:isWangan?16+p2wNorm*8:22+p2wNorm*14, 5, 65);
  let rARB = clamp(isDrift?12+p2wNorm*8:isDrag?8:isRally?18:isRain?16:isWangan?14+p2wNorm*6:16+p2wNorm*10, 5, 65);
  if(isFWD){ fARB=clamp(fARB+5,5,65); rARB=clamp(rARB-4,5,65); }
  if(isRWD){ fARB=clamp(fARB-3,5,65); rARB=clamp(rARB+4,5,65); }
  // Scale ARB by PI class
  const arbMult = CLASS_ARB[s.carClass||"A"] || 1.0;
  fARB=clamp(fARB*arbMult,5,65); rARB=clamp(rARB*arbMult,5,65);
  // Touge: soften ARB 15% for mid-corner bumps and elevation drops
  if(isTouge) { fARB=round1(fARB*0.85); rARB=round1(rARB*0.85); }
  fARB=round1(fARB); rARB=round1(rARB);

  // ── DAMPING — critical damping ratio method ─────────────────────────────────
  // Critical damping: Cc = 2 × sqrt(k × m) per corner
  const dampRatioF  = isDrift?0.75:isRally?0.55:isRain?0.60:isDrag?0.70:0.68;
  const dampRatioR  = isDrift?0.60:isRally?0.50:isRain?0.58:isDrag?0.65:0.65;
  // In Forza scale 1–20: derived from ratio × normalized spring×mass factor
  const fReboundRaw = dampRatioF * Math.sqrt(fSpringNmm*cornerMassF) * 0.0045;
  const rReboundRaw = dampRatioR * Math.sqrt(rSpringNmm*cornerMassR) * 0.0045;
  const fRebound = round1(clamp(fReboundRaw+6.0, 4.0, 16.0));
  const rRebound = round1(clamp(rReboundRaw+5.5, 4.0, 16.0));
  const fBump    = round1(clamp(fRebound*0.58, 3.0, 11.0));
  const rBump    = round1(clamp(rRebound*0.58, 3.0, 11.0));

  // ── BRAKING — traction circle + weight transfer ─────────────────────────────
  // Ideal front bias: frontWeightPct + decel load transfer
  const decelG         = isDrift?0.6:isDrag?0.5:isSnow?0.4:0.85;
  const wtTransferF    = (decelG * 0.45 * weightKg * 0.55) / weightKg; // fraction going forward
  let brakeBal = Math.round((frontWtPct + wtTransferF*0.35) * 100);
  if(isWheel)  brakeBal = clamp(brakeBal+2,40,65); // wheel = more front, trail brake
  if(isDrift)  brakeBal = clamp(brakeBal-8,40,65); // need rear rotation
  if(isFWD)    brakeBal = clamp(brakeBal+4,40,65);
  if(isRWD)    brakeBal = clamp(brakeBal-3,40,65);
  brakeBal = clamp(brakeBal,40,65);
  const brakePressure  = isDrift?82:isDrag?108:isSnow?85:isRain?90:100;
  const trailRating    = isDrift?5:isDrag?3:isRain?7:isRally?6:isWheel?9:7;

  // ── DIFFERENTIAL ────────────────────────────────────────────────────────────
  // Accel from power delivery character, Decel from braking stability need
  const powerAggression = clamp(p2wNorm, 0.3, 1.0);
  let fAccel=0,fDecel=0,rAccel=0,rDecel=0,center=0;
  if(isFWD){
    fAccel = Math.round(clamp(isDrift?82:isDrag?88:55+powerAggression*20, 20, 95));
    fDecel = Math.round(clamp(isDrift?0:isDrag?12:15+powerAggression*10, 0, 50));
  } else if(isRWD){
    rAccel = Math.round(clamp(isDrift?82:isDrag?92:50+powerAggression*25, 20, 95));
    rDecel = Math.round(clamp(isDrift?30:isDrag?5:isTouge?26:18+powerAggression*12, 0, 50));
  } else {
    fAccel = Math.round(clamp(isDrift?30:isDrag?18:35+powerAggression*10, 10, 70));
    fDecel = Math.round(clamp(isDrift?0:isDrag?5:12+powerAggression*8, 0, 40));
    rAccel = Math.round(clamp(isDrift?78:isDrag?90:55+powerAggression*20, 20, 95));
    rDecel = Math.round(clamp(isDrift?25:isDrag?5:18+powerAggression*12, 0, 50));
    center = Math.round(clamp(isDrift?32:isDrag?18:isDirt?55:42+powerAggression*8, 15, 75));
  }

  const pStr  = v => pUnit==="bar"?v+" bar":v+" PSI";
  const sStr  = v => v+" "+sUnit;

  const diffValues = isFWD?[
    {key:"Front Accel",value:fAccel+"%",note:"Traction vs torque steer balance"},
    {key:"Front Decel",value:fDecel+"%",note:"Trail-off stability"},
  ]:isRWD?[
    {key:"Rear Accel", value:rAccel+"%",note:isDrift?"High for rotation control":"Power traction on exit"},
    {key:"Rear Decel", value:rDecel+"%",note:"Engine braking stability into corners"},
  ]:[
    {key:"Front Accel",value:fAccel+"%",note:"Forward torque distribution"},
    {key:"Front Decel",value:fDecel+"%",note:"Lift-off understeer control"},
    {key:"Rear Accel", value:rAccel+"%",note:"Primary traction driver"},
    {key:"Rear Decel", value:rDecel+"%",note:"Rotation under trail braking"},
    {key:"Center Balance",value:center+"% fwd",note:"Front/rear torque split balance"},
  ];

  return {
    _meta: { gearSpeeds, speedUnit:units.speed, finalDrive },
    Tires:{values:[
      {key:"Front Pressure",value:pStr(fp),note:"Compound+surface optimised contact patch"},
      {key:"Rear Pressure", value:pStr(rp),note:isDrag?"Lower rear for launch traction":isDrift?"Lower rear for predictable rotation":"Balanced grip"},
      {key:"Front Width",   value:tireWF+"mm",note:"Lateral grip width"},
      {key:"Rear Width",    value:tireWR+"mm",note:isRWD?"Wider for power traction":"Contact patch match"},
      {key:"Compound",      value:compound,   note:"Selected for "+surface+" surface grip needs"},
    ],tip:isDrift?"±0.5 PSI rear changes rotation character significantly on entry.":isRain?"Lower than dry — cold wet rubber needs more surface contact.":"Adjust ±0.5 PSI front if corner mid-point feels loose or washes wide."},

    Gearing:{values:[
      {key:"Final Drive",  value:String(finalDrive),note:isDrag?"Short final for launch torque multiplication":"Matched to top speed target"},
      ...gearRatios.map((r,i)=>({key:`${i+1}${["st","nd","rd"][i]||"th"} Gear`,value:`${r}  (${gearSpeeds[i]} ${units.speed})`,note:i===0?"Launch gear — "+Math.round(gearSpeeds[0])+" "+units.speed+" redline":i===gears-1?"Top gear — "+Math.round(gearSpeeds[gears-1])+" "+units.speed+" redline":"Step ratio: "+round2(gearRatios[i-1]/r)+"× step from previous"})),
    ],tip:isDrag?"Tight spacing in gears 1–3 maximises acceleration. Shift at peak power RPM, not redline.":isWangan?"Long final drive with even spacing — keeps you in the powerband at sustained highway speeds without hitting redline.":"Geometric spacing keeps the engine in its powerband through every corner."},

    Alignment:{values:[
      {key:"Front Camber",value:fCamber.toFixed(1)+"°",note:isDrag?"Zero — straight-line grip is the only priority":"Negative camber compensates for load-induced tire deformation"},
      {key:"Rear Camber", value:rCamber.toFixed(1)+"°",note:"Rear contact patch under cornering and traction load"},
      {key:"Front Toe",   value:fToe.toFixed(1)+"°",   note:isFWD?"Slight toe-out sharpens FWD turn-in":"Neutral — stability priority on entry"},
      {key:"Rear Toe",    value:rToe.toFixed(1)+"°",   note:isDrift?"Slight toe-out encourages rotation":"Mild toe-in for high speed straight stability"},
      {key:"Front Caster",value:caster.toFixed(1)+"°", note:isWheel?"Higher caster = better FFB trail feel":"Moderate caster for controller steering weight"},
    ],tip:"Adjust camber in 0.2° steps — too much causes uneven tire wear and loses straight-line grip."},

    Suspension:{values:[
      {key:"Front Spring",       value:sStr(fSpring),note:`Natural freq ${fFreq.toFixed(1)} Hz — ${isDrift?"stiff for roll control":isRally?"soft for compliance":"race stiffness"}`},
      {key:"Rear Spring",        value:sStr(rSpring),note:`Natural freq ${rFreq.toFixed(1)} Hz — matched to front balance ratio`},
      {key:"Front Ride Height",  value:fRide.toFixed(1)+" in",note:isRally?"Higher for ground clearance":"Lower CG improves handling, watch for bottoming"},
      {key:"Rear Ride Height",   value:rRide.toFixed(1)+" in",note:isDrag?"Slight rake helps launch weight transfer":"Rear slightly lower or equal to front"},
    ],tip:isRally?"Prioritise clearance over low CoG — a bottomed car is slower than a high one.":"Front should be equal or slightly lower than rear for neutral high-speed balance."},

    ARB:{values:[
      {key:"Front ARB",value:fARB.toFixed(1),note:isDrift?"High front resists roll, promotes oversteer entry":isFWD?"Higher front reduces understeer tendency":"Controls weight transfer rate through transitions"},
      {key:"Rear ARB", value:rARB.toFixed(1),note:isDrift?"Softer rear allows rotation on entry":"Higher rear = more stable, less rotation on corner entry"},
    ],tip:isTouge?"ARBs softened 15% for mountain passes — handles elevation drops and mid-corner bumps without bouncing.":"Balance: snap oversteer → soften rear. Persistent understeer → soften front. Adjust in steps of 3."},

    Damping:{values:[
      {key:"Front Rebound",value:fRebound.toFixed(1),note:`Damping ratio ${(dampRatioF*100).toFixed(0)}% — controls front extension speed`},
      {key:"Rear Rebound", value:rRebound.toFixed(1),note:"Higher than bump — prevents rear bounce under power"},
      {key:"Front Bump",   value:fBump.toFixed(1),   note:"Compression over bumps and under braking — "+round1(fBump/fRebound*100)+"% of rebound"},
      {key:"Rear Bump",    value:rBump.toFixed(1),   note:"Surface compliance — softer than rebound to absorb inputs"},
    ],tip:"Rebound always > bump. If car bounces, increase bump. If it feels planted but wooden, reduce rebound."},

    Aero:{values:[
      {key:"Front Downforce",  value:hasAero?aeroF+"kg":"N/A",note:hasAero?"Reduces understeer at speed — adds drag":"No aero pack — rely on mechanical balance"},
      {key:"Rear Downforce",   value:hasAero?aeroR+"kg":"N/A",note:hasAero?"Higher rear keeps car planted — prevents oversteer":"Spring/ARB must compensate for high-speed stability"},
      {key:"Drag Coefficient", value:dragCd+" Cd",            note:isDrag?"Minimise — every 0.01 Cd costs ~2 km/h at trap":"Aero balance affects handling more than outright speed"},
    ],tip:hasAero?"Rear downforce should be 30–60% higher than front. Equal downforce usually causes oversteer.":"Without aero, reduce top ARB settings by 3–5 to compensate for no high-speed stability."},

    Braking:{values:[
      {key:"Brake Balance",     value:brakeBal+"% F",    note:"Traction circle: brake load = less lateral grip — "+brakeBal+"% front supports trail entry"},
      {key:"Brake Pressure",    value:brakePressure+"%", note:isWheel?"Linear for progressive trail braking":"ABS threshold — max pressure before lockup"},
      {key:"Trail Brake Rating",value:trailRating+"/10", note:"How well this tune rewards trail braking technique"},
    ],tip:isWheel?"Trail brake: maintain 20–40% pedal as you turn in, release as you reach apex.":"Under ABS: hold threshold and steer simultaneously — don't release to steer, both at once."},

    Diff:{values:diffValues,tip:isDrift?"Accel diff keeps the slide on throttle. Decel controls entry rotation — reduce if too snappy.":isTouge?"Higher rear decel gives engine braking stability on steep downhills — reduces snap oversteer into hairpins.":isFWD?"High front accel causes torque steer. Start low, increase until you feel wheelspin, then back off 5%.":"Rear accel controls exit traction. If spinning out on exit, reduce by 5–10%."},
  };
}

// ─── AI CALL ──────────────────────────────────────────────────────────────────
async function callAI(providerId,apiKey,system,user){
  if(providerId==="gemini"){const url=`https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=${apiKey}`;const r=await fetch(url,{method:"POST",headers:{"Content-Type":"application/json"},body:JSON.stringify({contents:[{role:"user",parts:[{text:system+"\n\n"+user}]}],generationConfig:{temperature:0.3,maxOutputTokens:1200}})});if(!r.ok)throw new Error("Gemini "+r.status);const d=await r.json();return d.candidates?.[0]?.content?.parts?.[0]?.text||"";}
  if(providerId==="grok"){const r=await fetch("https://api.x.ai/v1/chat/completions",{method:"POST",headers:{"Content-Type":"application/json","Authorization":"Bearer "+apiKey},body:JSON.stringify({model:"grok-3-mini",messages:[{role:"system",content:system},{role:"user",content:user}],temperature:0.3,max_tokens:1200})});if(!r.ok)throw new Error("Grok "+r.status);const d=await r.json();return d.choices?.[0]?.message?.content||"";}
  if(providerId==="openai"){const r=await fetch("https://api.openai.com/v1/chat/completions",{method:"POST",headers:{"Content-Type":"application/json","Authorization":"Bearer "+apiKey},body:JSON.stringify({model:"gpt-4o-mini",messages:[{role:"system",content:system},{role:"user",content:user}],temperature:0.3,max_tokens:1200})});if(!r.ok)throw new Error("OpenAI "+r.status);const d=await r.json();return d.choices?.[0]?.message?.content||"";}
  if(providerId==="claude"){const r=await fetch("https://api.anthropic.com/v1/messages",{method:"POST",headers:{"Content-Type":"application/json","x-api-key":apiKey,"anthropic-version":"2023-06-01"},body:JSON.stringify({model:"claude-haiku-4-5-20251001",max_tokens:1200,system,messages:[{role:"user",content:user}]})});if(!r.ok)throw new Error("Claude "+r.status);const d=await r.json();return d.content?.[0]?.text||"";}
  throw new Error("Unknown provider");
}

function buildEnhancePrompt(s,tune){
  const tm=TUNE_MODES.find(t=>t.id===s.tuneId);
  const asp=ASPIRATIONS.find(a=>a.id===s.aspiration);
  return{system:`You are a Forza Horizon expert tuner. Enrich the provided tune JSON with car-specific expert notes. Return ONLY the same JSON structure with improved "note" and "tip" fields. Do not change numeric values.`,
  user:`${s.make} ${s.model} | ${s.driveType} | ${tm?.label} | ${s.surface}\n${asp?.label} | ${s.engineSwap!=="None (Stock)"?s.engineSwap+" swap":""} | ${s.hp}hp ${s.weight}${s.units.weight}\nInput: ${s.inputDevice}\n\n${JSON.stringify(tune,null,2)}`};
}

// ─── OFFLINE WIZARD FIXES ─────────────────────────────────────────────────────
const OFFLINE_FIXES = {
  understeer:{diagnosis:"Front tires losing grip before rears — common causes: too-stiff front ARB, insufficient front negative camber, or high entry speed.",fixes:[{setting:"Front ARB",change:"Reduce by 4–6",why:"Softer front ARB allows more weight transfer to front tires in corners"},{setting:"Front Camber",change:"Add 0.3° negative",why:"More negative camber improves contact patch under cornering load"},{setting:"Rear ARB",change:"Increase by 3",why:"Stiffer rear shifts balance, encourages front rotation"}],tip:"Try trail braking deeper — releasing brake progressively while turning shifts weight forward and aids rotation."},
  oversteer:{diagnosis:"Rear tires losing traction — too-stiff rear ARB, aggressive diff accel, or not enough rear camber.",fixes:[{setting:"Rear ARB",change:"Reduce by 4–5",why:"Softer rear allows weight to stay on rear tires longer through corner"},{setting:"Rear Accel Diff",change:"Reduce by 10–15%",why:"Less locking on throttle prevents rear stepping out under power"},{setting:"Rear Camber",change:"Add 0.2° negative",why:"Improves rear contact patch under cornering load"}],tip:"Progressive throttle application prevents snap oversteer — feed power in gradually past the apex."},
  braking:{diagnosis:"Brake balance or pressure mismatch — lockup or instability under threshold braking.",fixes:[{setting:"Brake Balance",change:"Add 3–5% rear",why:"Less front-biased balance reduces front lockup tendency"},{setting:"Brake Pressure",change:"Reduce by 10–15%",why:"More modulation range — prevents locking before you can steer"},{setting:"Front Bump",change:"Increase by 0.5",why:"Stiffer front compression reduces nose dive and braking instability"}],tip:"Trail braking: maintain 20–30% brake as you begin to turn — don't fully release before steering."},
  sluggish:{diagnosis:"Car too soft or diff too open — slow weight transfer and mushy responses.",fixes:[{setting:"Front + Rear ARB",change:"Increase both by 4",why:"Stiffer ARB quickens responses and reduces body roll delay"},{setting:"Rear Accel Diff",change:"Increase by 10%",why:"More diff lock gets power down faster with less wheelspin delay"},{setting:"Spring Rates",change:"Increase both by 10%",why:"Stiffer springs speed up weight transfer and sharpen responses"}],tip:"If on a controller, check in-game steering linearity — a sluggish car could be a dead zone issue, not a tune issue."},
  twitchy:{diagnosis:"Over-stiffened setup or mismatched damping — inputs transfer directly to instability.",fixes:[{setting:"Front + Rear ARB",change:"Reduce both by 4–5",why:"Softer ARB smooths transitions and eliminates snap responses"},{setting:"Bump Damping",change:"Reduce front + rear by 0.5",why:"Softer bump lets car absorb surface inputs rather than transmitting them"},{setting:"Rear Toe",change:"Add 0.1° toe-in",why:"Mild rear toe-in adds passive straight-line stability"}],tip:"Twitchy on straights usually means overinflated tires — try dropping pressure by 1.5 PSI front and rear."},
};

// ─── SHARE CARD GENERATOR ────────────────────────────────────────────────────
function buildShareCard(s,pages) {
  const tm   = TUNE_MODES.find(t=>t.id===s.tuneId);
  const seed = TUNE_SEEDS[s.tuneId]||"#4fc3f7";
  const tires = pages.Tires?.values||[];
  const gear  = pages.Gearing?.values||[];
  const brakes= pages.Braking?.values||[];
  const diff  = pages.Diff?.values||[];
  const html  = `<!DOCTYPE html><html><head><meta charset="utf-8"><style>
    *{margin:0;padding:0;box-sizing:border-box}
    body{width:390px;height:844px;background:#0f1117;font-family:'Roboto',sans-serif;color:#e2e4f0;overflow:hidden}
    .header{background:linear-gradient(135deg,${seed}22,${seed}08);border-bottom:1px solid ${seed}44;padding:24px 20px 16px}
    .logo{font-size:10px;letter-spacing:4px;color:${seed}88;margin-bottom:4px}
    .title{font-size:26px;font-weight:900;letter-spacing:1px}
    .title span{color:${seed}}
    .subtitle{font-size:13px;color:#9198ac;margin-top:4px}
    .badge{display:inline-block;background:${seed}22;border:1px solid ${seed}55;border-radius:20px;padding:3px 10px;font-size:11px;color:${seed};margin-top:8px}
    .grid{display:grid;grid-template-columns:1fr 1fr;gap:10px;padding:16px 20px}
    .card{background:#1c1f2a;border-radius:12px;padding:12px 14px;border:1px solid #2e3340}
    .card-label{font-size:9px;letter-spacing:2px;color:#4a5068;text-transform:uppercase;margin-bottom:8px}
    .row{display:flex;justify-content:space-between;align-items:center;padding:3px 0;border-bottom:1px solid #1e2230}
    .row:last-child{border-bottom:none}
    .row-key{font-size:11px;color:#9198ac}
    .row-val{font-size:12px;font-family:'Roboto Mono',monospace;color:${seed};font-weight:700}
    .footer{position:absolute;bottom:0;left:0;right:0;padding:14px 20px;display:flex;align-items:center;justify-content:space-between;background:#0f1117;border-top:1px solid #1e2230}
    .footer-text{font-size:10px;color:#4a5068;letter-spacing:1px}
    .qr{width:36px;height:36px;background:#1c1f2a;border-radius:6px;display:flex;align-items:center;justify-content:center;font-size:20px}
  </style><link href="https://fonts.googleapis.com/css2?family=Roboto:wght@400;700;900&family=Roboto+Mono:wght@700&display=swap" rel="stylesheet"></head><body>
    <div class="header">
      <div class="logo">TUNELAB • FORZA HORIZON</div>
      <div class="title">TUNE<span>LAB</span></div>
      <div class="subtitle">${s.make} ${s.model}</div>
      <div class="badge">${tm?.icon} ${tm?.label} &nbsp;·&nbsp; ${s.carClass} ${s.pi}PI &nbsp;·&nbsp; ${s.driveType}</div>
    </div>
    <div class="grid">
      <div class="card"><div class="card-label">Tires</div>${tires.slice(0,3).map(r=>`<div class="row"><span class="row-key">${r.key}</span><span class="row-val">${r.value}</span></div>`).join("")}</div>
      <div class="card"><div class="card-label">Braking</div>${brakes.map(r=>`<div class="row"><span class="row-key">${r.key}</span><span class="row-val">${r.value}</span></div>`).join("")}</div>
      <div class="card" style="grid-column:1/-1"><div class="card-label">Gearing</div><div style="display:grid;grid-template-columns:repeat(4,1fr);gap:4px">${gear.slice(0,8).map(r=>`<div style="text-align:center;padding:4px;background:#0f1117;border-radius:6px"><div style="font-size:9px;color:#4a5068">${r.key.replace(" Gear","")}</div><div style="font-size:11px;font-family:'Roboto Mono';color:${seed};font-weight:700">${r.value.split(" ")[0]}</div></div>`).join("")}</div></div>
      <div class="card" style="grid-column:1/-1"><div class="card-label">Differential</div>${diff.map(r=>`<div class="row"><span class="row-key">${r.key}</span><span class="row-val">${r.value}</span></div>`).join("")}</div>
    </div>
    <div class="footer"><span class="footer-text">TUNELAB.APP • FREE ON PLAY STORE</span><div class="qr">📱</div></div>
  </body></html>`;
  return html;
}

// ─── STORAGE ──────────────────────────────────────────────────────────────────
const tuneColor = id => TUNE_SEEDS[id]||"#4fc3f7";
const loadSaves = ()  => LS.get("tl_saves_v1",[]);
const writeSaves= d   => LS.set("tl_saves_v1",d);
const toKg = (v,u) => u==="lbs"?v/2.205:v;
const toKmh= (v,u) => u==="mph"?v*1.609:v;

function formatTuneText(s,pages){
  const tm=TUNE_MODES.find(t=>t.id===s.tuneId);
  const lines=[`╔════════════════════════════════╗`,`  TUNELAB — ${s.make} ${s.model}`,`  ${tm?.icon} ${tm?.label} | ${s.carClass} ${s.pi}PI | ${s.driveType}`,`  Surface: ${s.surface}`,`╚════════════════════════════════╝`];
  TUNE_PAGES.forEach(pg=>{const d=pages[pg];if(!d)return;lines.push(`\n── ${pg.toUpperCase()} ──`);(d.values||[]).forEach(r=>lines.push(`  ${r.key.padEnd(22)} ${r.value}`));if(d.tip)lines.push(`  💡 ${d.tip}`);});
  lines.push(`\nTuneLab • tunelab.app`);
  return lines.join("\n");
}


// ─── PSI EXPLAINER COMPONENT ─────────────────────────────────────────────────
// Shows after the Tires tune card — explains what PSI means and how to adjust
function PsiExplainer({pages, color, units}) {
  const [open, setOpen] = useState(false);
  const tireData = pages?.Tires?.values || [];
  const fpsiRow = tireData.find(r=>r.key==="Front Pressure");
  const rpsiRow = tireData.find(r=>r.key==="Rear Pressure");
  const pUnit = units.pressure;
  // Parse the numeric PSI value from the string
  const parseVal = str => parseFloat(str)||0;
  const fp = parseVal(fpsiRow?.value||"28");
  const rp = parseVal(rpsiRow?.value||"28");
  const pMin = pUnit==="bar"?1.2:17; const pMax = pUnit==="bar"?3.2:47;
  const pNorm = v => Math.round(((v-pMin)/(pMax-pMin))*100);

  const rangeBar = (label, val, pct) => (
    <div style={{marginBottom:14}}>
      <div style={{display:"flex",justifyContent:"space-between",marginBottom:6}}>
        <span style={{fontFamily:"Roboto,sans-serif",fontSize:12,color:MD3.onSurface,fontWeight:500}}>{label}</span>
        <span style={{fontFamily:"Roboto Mono,monospace",fontSize:13,color,fontWeight:700}}>{val}</span>
      </div>
      <div style={{position:"relative",height:8,background:MD3.outlineVar,borderRadius:4,overflow:"visible"}}>
        <div style={{position:"absolute",left:0,top:0,height:"100%",width:`${pct}%`,background:`linear-gradient(to right,${color}88,${color})`,borderRadius:4}}/>
        <div style={{position:"absolute",top:-4,left:`${pct}%`,transform:"translateX(-50%)",width:16,height:16,borderRadius:"50%",background:color,border:`2px solid ${MD3.surface}`,boxShadow:`0 0 6px ${color}88`}}/>
      </div>
      <div style={{display:"flex",justifyContent:"space-between",marginTop:6}}>
        <span style={{fontFamily:"Roboto,sans-serif",fontSize:10,color:MD3.onSurfaceLow}}>Low — softer contact patch{pUnit==="bar"?" (1.2 bar)":" (17 PSI)"}</span>
        <span style={{fontFamily:"Roboto,sans-serif",fontSize:10,color:MD3.onSurfaceLow}}>High — stiffer, less flex{pUnit==="bar"?" (3.2 bar)":" (47 PSI)"}</span>
      </div>
    </div>
  );

  return (
    <div style={{marginTop:10,background:MD3.surfaceC1,border:`1px solid ${MD3.outline}`,borderRadius:16,overflow:"hidden"}}>
      <button onClick={()=>setOpen(o=>!o)} style={{width:"100%",padding:"12px 16px",background:"transparent",border:"none",display:"flex",alignItems:"center",justifyContent:"space-between",cursor:"pointer"}}>
        <div style={{display:"flex",alignItems:"center",gap:8}}>
          <span style={{fontSize:16}}>🎓</span>
          <span style={{fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:600,color:MD3.onSurface}}>What do these pressures mean?</span>
        </div>
        <span style={{fontFamily:"Roboto,sans-serif",fontSize:18,color:MD3.onSurfaceLow,transition:"transform 0.2s",transform:open?"rotate(180deg)":"none"}}>⌄</span>
      </button>
      {open&&<div style={{padding:"0 16px 16px"}}>
        {rangeBar("Front — "+fpsiRow?.value, fpsiRow?.value, pNorm(fp))}
        {rangeBar("Rear — "+rpsiRow?.value, rpsiRow?.value, pNorm(rp))}
        <div style={{background:MD3.surfaceVar,borderRadius:12,padding:"12px 14px",marginTop:4}}>
          <div style={{fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:700,color:MD3.onSurface,marginBottom:8}}>How to read this in-game</div>
          <div style={{display:"flex",flexDirection:"column",gap:6}}>
            {[
              {icon:"🔼",label:"Higher pressure",effect:"More rigid sidewall, smaller contact patch. Better at high speed, resists rolling. Can feel twitchy on bumps."},
              {icon:"🔽",label:"Lower pressure",effect:"Softer sidewall, larger contact patch. More grip in corners, better over rough surfaces. Can feel floaty at high speed."},
              {icon:"🔧",label:"After your test drive",effect:"If the car feels slippery mid-corner, try dropping rear pressure by 0.5. If it bounces on straights, raise front by 0.5."},
            ].map((r,i)=><div key={i} style={{display:"flex",gap:10,alignItems:"flex-start"}}>
              <span style={{fontSize:14,flexShrink:0,marginTop:1}}>{r.icon}</span>
              <div><span style={{fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:600,color:MD3.onSurface}}>{r.label}: </span><span style={{fontFamily:"Roboto,sans-serif",fontSize:12,color:MD3.onSurfaceVar,lineHeight:1.5}}>{r.effect}</span></div>
            </div>)}
          </div>
        </div>
      </div>}
    </div>
  );
}


// ─── ABOUT SCREEN ─────────────────────────────────────────────────────────────
function AboutScreen({onClose}) {
  const [fbStep,setFbStep]=useState("idle");
  const [fbType,setFbType]=useState("bug");
  const [fbText,setFbText]=useState("");
  const sendFb=()=>{
    const sub=encodeURIComponent(`TuneLab Feedback — ${fbType}`);
    const bod=encodeURIComponent(`Type: ${fbType}

${fbText}

---
Sent from TuneLab for FH6`);
    window.location.href=`mailto:tunelab.dev@gmail.com?subject=${sub}&body=${bod}`;
    setFbStep("sent");
  };
  const ghUrl=`https://github.com/YOUR_GITHUB/tunelab/issues/new?labels=${encodeURIComponent(fbType)}&title=${encodeURIComponent(`[${fbType}] `)}`;
  return (
    <div style={{position:"fixed",inset:0,background:MD3.surface,zIndex:400,maxWidth:1200,margin:"0 auto",display:"flex",flexDirection:"column",fontFamily:"Roboto,sans-serif",overflowY:"auto"}}>
      <div style={{background:MD3.surfaceVar,borderBottom:`1px solid ${MD3.outline}`,padding:"14px 18px",display:"flex",alignItems:"center",gap:14,flexShrink:0}}>
        <button onClick={onClose} style={{background:"transparent",border:"none",color:MD3.onSurface,fontSize:22,cursor:"pointer",padding:0}}>←</button>
        <span style={{fontFamily:"Roboto,sans-serif",fontSize:16,fontWeight:700,color:MD3.onSurface}}>About &amp; Support</span>
      </div>
      <div style={{padding:"20px 18px 50px",maxWidth:600,margin:"0 auto",width:"100%"}}>

        {/* App identity */}
        <div style={{background:MD3.surfaceC1,border:"1px solid #4fc3f733",borderRadius:16,padding:"20px",marginBottom:20,textAlign:"center"}}>
          <div style={{fontFamily:"Roboto,sans-serif",fontSize:32,fontWeight:900,color:"#4fc3f7",marginBottom:4}}>Tune<span style={{color:MD3.onSurface}}>Lab</span></div>
          <div style={{fontFamily:"Roboto,sans-serif",fontSize:13,color:MD3.onSurfaceVar,marginBottom:10}}>AI-assisted Forza Horizon tuning calculator</div>
          <div style={{display:"flex",gap:8,justifyContent:"center",flexWrap:"wrap"}}>
            {["Free forever","No ads","Offline capable","Open source"].map(tag=>(
              <span key={tag} style={{background:MD3.success+"18",border:`1px solid ${MD3.success}33`,borderRadius:12,padding:"2px 10px",fontFamily:"Roboto,sans-serif",fontSize:11,color:MD3.success}}>{tag}</span>
            ))}
          </div>
        </div>

        {/* Donation */}
        <div style={{background:MD3.surfaceC1,border:"1px solid #ffd74033",borderRadius:16,padding:"16px 18px",marginBottom:16}}>
          <div style={{fontFamily:"Roboto,sans-serif",fontSize:14,fontWeight:700,color:MD3.onSurface,marginBottom:6}}>☕ Support TuneLab</div>
          <div style={{fontFamily:"Roboto,sans-serif",fontSize:13,color:MD3.onSurfaceVar,lineHeight:1.6,marginBottom:14}}>
            TuneLab is completely free — no ads, no paywalls. If it helped you build a great tune, consider buying me a coffee to keep the servers running.
          </div>
          <a href="https://ko-fi.com/tunelabs" target="_blank" rel="noreferrer" style={{display:"block",padding:"13px",background:"#ffd74018",border:"1px solid #ffd74044",borderRadius:12,color:"#ffd740",fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:700,textDecoration:"none",textAlign:"center",marginBottom:8}}>
            ☕ Ko-fi — Buy Me a Coffee
          </a>
        </div>

        {/* Community */}
        <div style={{background:MD3.surfaceC1,border:`1px solid ${MD3.outline}`,borderRadius:16,padding:"16px 18px",marginBottom:16}}>
          <div style={{fontFamily:"Roboto,sans-serif",fontSize:14,fontWeight:700,color:MD3.onSurface,marginBottom:12}}>Community</div>
          <div style={{display:"flex",flexDirection:"column",gap:8}}>
            {[
              {icon:"💬",label:"Discord Server",desc:"Share tunes, get help, vote on features",url:"https://discord.gg/tunelab",color:"#5865f2"},
              {icon:"🐙",label:"GitHub",desc:"Report bugs, request features, view source",url:"https://github.com/YOUR_GITHUB/tunelab",color:"#e2e4f0"},
              {icon:"📱",label:"Rate on Play Store",desc:"Help others find TuneLab",url:"https://play.google.com/store",color:MD3.success},
            ].map(item=>(
              <a key={item.label} href={item.url} target="_blank" rel="noreferrer" style={{display:"flex",alignItems:"center",gap:12,padding:"12px 14px",background:MD3.surfaceVar,borderRadius:12,textDecoration:"none",border:`1px solid ${MD3.outline}`}}>
                <span style={{fontSize:20}}>{item.icon}</span>
                <div style={{flex:1}}>
                  <div style={{fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:600,color:item.color}}>{item.label}</div>
                  <div style={{fontFamily:"Roboto,sans-serif",fontSize:11,color:MD3.onSurfaceVar}}>{item.desc}</div>
                </div>
                <span style={{color:MD3.onSurfaceLow,fontSize:16}}>›</span>
              </a>
            ))}
          </div>
        </div>

        {/* Feedback */}
        <div style={{background:MD3.surfaceC1,border:`1px solid ${MD3.error}33`,borderRadius:16,padding:"16px 18px",marginBottom:16}}>
          <div style={{fontFamily:"Roboto,sans-serif",fontSize:14,fontWeight:700,color:MD3.onSurface,marginBottom:6}}>Feedback &amp; Bug Reports</div>
          {fbStep==="idle"&&<>
            <div style={{fontFamily:"Roboto,sans-serif",fontSize:12,color:MD3.onSurfaceVar,lineHeight:1.55,marginBottom:12}}>
              Found a bug? Have an idea? Two ways to reach us:
            </div>
            <div style={{display:"grid",gridTemplateColumns:"1fr 1fr",gap:8}}>
              <button onClick={()=>setFbStep("form")} style={{padding:"11px",background:MD3.error+"18",border:`1px solid ${MD3.error}44`,borderRadius:12,color:MD3.error,fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:700,cursor:"pointer"}}>✉ Email Us</button>
              <a href={ghUrl} target="_blank" rel="noreferrer" style={{display:"block",padding:"11px",background:"#e2e4f018",border:"1px solid #e2e4f044",borderRadius:12,color:"#e2e4f0",fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:700,textDecoration:"none",textAlign:"center"}}>🐙 GitHub Issue</a>
            </div>
          </>}
          {fbStep==="form"&&<div>
            <div style={{display:"flex",gap:6,flexWrap:"wrap",marginBottom:12}}>
              {["bug","feature","tune feedback","other"].map(t=>(
                <button key={t} onClick={()=>setFbType(t)} style={{padding:"5px 12px",borderRadius:16,border:`1px solid ${fbType===t?MD3.error:MD3.outline}`,background:fbType===t?MD3.error+"22":"transparent",color:fbType===t?MD3.error:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontSize:11,fontWeight:600,cursor:"pointer",textTransform:"capitalize"}}>{t}</button>
              ))}
            </div>
            <textarea value={fbText} onChange={e=>setFbText(e.target.value)} placeholder="Describe the issue or idea…" rows={4} style={{width:"100%",background:MD3.surfaceVar,border:`1px solid ${MD3.outline}`,borderRadius:8,padding:"10px 12px",color:MD3.onSurface,fontFamily:"Roboto,sans-serif",fontSize:13,resize:"vertical",outline:"none",marginBottom:10}}/>
            <div style={{display:"grid",gridTemplateColumns:"1fr 1fr",gap:8}}>
              <button onClick={()=>setFbStep("idle")} style={{padding:"11px",background:"transparent",border:`1px solid ${MD3.outline}`,borderRadius:8,color:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontSize:12,cursor:"pointer"}}>Cancel</button>
              <button onClick={sendFb} style={{padding:"11px",background:MD3.error+"22",border:`1px solid ${MD3.error}55`,borderRadius:8,color:MD3.error,fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:700,cursor:"pointer"}}>Send ✉</button>
            </div>
          </div>}
          {fbStep==="sent"&&<div style={{textAlign:"center",padding:"10px 0"}}>
            <div style={{fontSize:28,marginBottom:8}}>✅</div>
            <div style={{fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:700,color:MD3.success}}>Feedback sent — thanks!</div>
            <button onClick={()=>setFbStep("idle")} style={{marginTop:12,padding:"8px 18px",background:"transparent",border:`1px solid ${MD3.outline}`,borderRadius:20,color:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontSize:12,cursor:"pointer"}}>Close</button>
          </div>}
        </div>

        {/* Privacy */}
        <div style={{background:MD3.surfaceC1,border:`1px solid ${MD3.outline}`,borderRadius:12,padding:"12px 16px",marginBottom:12}}>
          <div style={{fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:700,color:MD3.onSurface,marginBottom:6}}>Privacy</div>
          <div style={{fontFamily:"Roboto,sans-serif",fontSize:12,color:MD3.onSurfaceVar,lineHeight:1.6}}>
            TuneLab stores all data locally on your device. No account required. No data is sent to our servers. AI API keys are stored only on your device and sent only to the AI provider you choose.
            {" "}<a href="https://github.com/YOUR_GITHUB/tunelab/blob/main/PRIVACY.md" target="_blank" rel="noreferrer" style={{color:"#4fc3f7"}}>Full privacy policy →</a>
          </div>
        </div>

        <div style={{textAlign:"center",fontFamily:"Roboto,sans-serif",fontSize:11,color:MD3.onSurfaceLow,lineHeight:1.7,marginTop:8}}>
          TuneLab is not affiliated with Xbox, Turn 10, or Playground Games.<br/>
          Forza Horizon™ is a registered trademark of Microsoft Corporation.
        </div>
      </div>
    </div>
  );
}

// ─── QUICK-ADJUST OVERLAY ─────────────────────────────────────────────────────
// Lets users nudge individual tune values without full regeneration
function QuickAdjust({tunePages, setTunePages, activePage, color, onClose}) {
  const page = tunePages[activePage];
  if(!page) return null;
  const [localVals, setLocalVals] = useState(page.values.map(r=>({...r})));
  const isDirty = JSON.stringify(localVals) !== JSON.stringify(page.values);

  const nudgeVal = (idx, delta) => {
    setLocalVals(prev => prev.map((row,i) => {
      if(i!==idx) return row;
      const current = parseFloat(row.value) || 0;
      if(isNaN(current)) return row; // non-numeric, skip
      const decimals = row.value.includes(".") ? row.value.split(".")[1]?.replace(/[^0-9]/g,"").length||0 : 0;
      const newVal = +(current + delta).toFixed(decimals);
      const unit = row.value.replace(/[\d.\-]/g,"").trim();
      return {...row, value: newVal + (unit ? " "+unit : "")};
    }));
  };

  const apply = () => {
    setTunePages(p => ({...p, [activePage]: {...page, values: localVals}}));
    onClose();
  };

  return (
    <div style={{position:"fixed",inset:0,background:"#000b",zIndex:500,display:"flex",flexDirection:"column",justifyContent:"flex-end",maxWidth:1200,margin:"0 auto"}}>
      <div style={{background:MD3.surfaceVar,borderRadius:"20px 20px 0 0",border:`1px solid ${MD3.outline}`,maxHeight:"80vh",display:"flex",flexDirection:"column"}}>
        <div style={{width:36,height:4,borderRadius:2,background:MD3.outline,margin:"12px auto 0"}}/>
        <div style={{padding:"12px 18px",display:"flex",alignItems:"center",justifyContent:"space-between",borderBottom:`1px solid ${MD3.outlineVar}`,flexShrink:0}}>
          <span style={{fontFamily:"Roboto,sans-serif",fontSize:15,fontWeight:700,color:MD3.onSurface}}>Quick Adjust — {activePage}</span>
          <button onClick={onClose} style={{background:"transparent",border:"none",color:MD3.onSurfaceVar,fontSize:20,cursor:"pointer"}}>✕</button>
        </div>
        <div style={{flex:1,overflowY:"auto",padding:"12px 16px"}}>
          <div style={{fontFamily:"Roboto,sans-serif",fontSize:11,color:MD3.onSurfaceLow,marginBottom:14,lineHeight:1.5}}>
            Tap ＋ / − to nudge individual values. This adjusts the tune directly without regenerating.
          </div>
          {localVals.map((row,i)=>{
            const isNum = !isNaN(parseFloat(row.value));
            const numVal = parseFloat(row.value);
            const unit = row.value.replace(/[\d.\-]/g,"").trim();
            const step = unit.includes("°") ? 0.1 : unit.includes("PSI")||unit.includes("bar") ? 0.5 : unit.includes("%") ? 1 : unit.includes("rpm") ? 100 : 0.1;
            return (
              <div key={i} style={{padding:"10px 0",borderBottom:`1px solid ${MD3.outlineVar}`,display:"flex",alignItems:"center",gap:10}}>
                <div style={{flex:1}}>
                  <div style={{fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:500,color:MD3.onSurface}}>{row.key}</div>
                  {row.note&&<div style={{fontFamily:"Roboto,sans-serif",fontSize:10,color:MD3.onSurfaceLow,marginTop:2}}>{row.note}</div>}
                </div>
                {isNum ? (
                  <div style={{display:"flex",alignItems:"center",gap:8}}>
                    <button onClick={()=>nudgeVal(i,-step)} style={{width:32,height:32,borderRadius:"50%",border:`1px solid ${MD3.outline}`,background:MD3.surfaceC1,color:MD3.onSurface,fontSize:18,cursor:"pointer",display:"flex",alignItems:"center",justifyContent:"center",fontWeight:700}}>−</button>
                    <span style={{fontFamily:"Roboto Mono,monospace",fontSize:14,color,fontWeight:700,minWidth:70,textAlign:"center"}}>{row.value}</span>
                    <button onClick={()=>nudgeVal(i,+step)} style={{width:32,height:32,borderRadius:"50%",border:`1px solid ${MD3.outline}`,background:MD3.surfaceC1,color:MD3.onSurface,fontSize:18,cursor:"pointer",display:"flex",alignItems:"center",justifyContent:"center",fontWeight:700}}>+</button>
                  </div>
                ) : (
                  <span style={{fontFamily:"Roboto Mono,monospace",fontSize:13,color,fontWeight:700}}>{row.value}</span>
                )}
              </div>
            );
          })}
        </div>
        <div style={{padding:"12px 16px",borderTop:`1px solid ${MD3.outlineVar}`,display:"grid",gridTemplateColumns:"1fr 1fr",gap:10,flexShrink:0}}>
          <button onClick={()=>setLocalVals(page.values.map(r=>({...r})))} style={{padding:"12px",background:"transparent",border:`1px solid ${MD3.outline}`,borderRadius:12,color:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontSize:13,cursor:"pointer"}}>Reset</button>
          <button onClick={apply} style={{padding:"12px",background:isDirty?color+"22":"transparent",border:`1.5px solid ${isDirty?color:MD3.outline}`,borderRadius:12,color:isDirty?color:MD3.onSurfaceLow,fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:700,cursor:isDirty?"pointer":"default"}}>Apply Changes</button>
        </div>
      </div>
    </div>
  );
}

// ─── TUNE RATING ──────────────────────────────────────────────────────────────
function TuneRating({tuneId, saveId, onRate}) {
  const key = `tl_rating_${saveId||tuneId}`;
  const [rating, setRating] = useState(()=>LS.get(key,0));
  const [note,   setNote  ] = useState(()=>LS.get(key+"_note",""));
  const [open,   setOpen  ] = useState(false);
  const color = TUNE_SEEDS[tuneId]||"#4fc3f7";

  const doRate = (r) => {
    // Clicking the same star again clears the rating
    const next = r === rating ? 0 : r;
    setRating(next); LS.set(key, next);
    if(onRate) onRate(next);
  };
  const saveNote = () => { LS.set(key+"_note",note); setOpen(false); };

  return (
    <div style={{background:MD3.surfaceC1,border:`1px solid ${MD3.outline}`,borderRadius:16,padding:"14px 16px",marginTop:10}}>
      <div style={{display:"flex",alignItems:"center",justifyContent:"space-between",marginBottom:8}}>
        <span style={{fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:600,color:MD3.onSurface}}>Rate this tune</span>
        {rating>0&&<span style={{fontFamily:"Roboto,sans-serif",fontSize:11,color:MD3.onSurfaceLow}}>Saved locally</span>}
      </div>
      <div style={{display:"flex",gap:6,marginBottom:8}}>
        {[1,2,3,4,5].map(s=>(
          <button key={s} onClick={()=>doRate(s)} style={{flex:1,padding:"10px 0",borderRadius:10,border:`1px solid ${s<=rating?color:MD3.outline}`,background:s<=rating?color+"22":"transparent",color:s<=rating?color:MD3.onSurfaceLow,fontSize:18,cursor:"pointer",transition:"all 0.12s"}}>
            {s<=rating?"★":"☆"}
          </button>
        ))}
      </div>
      <button onClick={()=>setOpen(o=>!o)} style={{width:"100%",padding:"8px",background:"transparent",border:`1px solid ${MD3.outline}`,borderRadius:8,color:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontSize:12,cursor:"pointer"}}>
        {note ? `📝 ${note.slice(0,40)}${note.length>40?"…":""}` : "Add a note about this tune…"}
      </button>
      {open&&<div style={{marginTop:8}}>
        <textarea value={note} onChange={e=>setNote(e.target.value)} placeholder="e.g. Great in S1 races, a bit loose in rain…" rows={3} style={{width:"100%",background:MD3.surfaceVar,border:`1px solid ${MD3.outline}`,borderRadius:8,padding:"8px 12px",color:MD3.onSurface,fontFamily:"Roboto,sans-serif",fontSize:12,resize:"none",outline:"none",marginBottom:8}}/>
        <button onClick={saveNote} style={{width:"100%",padding:"9px",background:color+"22",border:`1px solid ${color}44`,borderRadius:8,color,fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:700,cursor:"pointer"}}>Save Note</button>
      </div>}
    </div>
  );
}

// ─── AUTO-HISTORY ─────────────────────────────────────────────────────────────
// Last 10 generated tunes saved automatically — no naming required
const HISTORY_KEY = "tl_history_v1";
const loadHistory = () => LS.get(HISTORY_KEY,[]);
const pushHistory = (appState, tunePages) => {
  const entry = {
    id: Date.now(),
    auto: true,
    name: `${appState.make} ${appState.model} — ${TUNE_MODES.find(t=>t.id===appState.tuneId)?.label||""}`,
    date: new Date().toLocaleString(),
    appState: {...appState},
    tunePages: {...tunePages},
  };
  const hist = [entry, ...loadHistory().slice(0,9)];
  LS.set(HISTORY_KEY, hist);
};


// ─── RESET SHEET ──────────────────────────────────────────────────────────────
function ResetSheet({onResetValues, onResetTune, onResetAll, hasSnapshot, tuneGenerated, onClose}) {
  const [confirming, setConfirming] = useState(null); // null | "tune" | "all"

  const options = [
    {
      id:"values",
      icon:"↩",
      label:"Restore calculated values",
      desc:"Undo any Quick Adjust nudges — go back to the formula output",
      color:MD3.success,
      available: hasSnapshot,
      action: () => { onResetValues(); onClose(); },
      needsConfirm: false,
    },
    {
      id:"tune",
      icon:"🔄",
      label:"New build",
      desc:"Clear the tune and reset tabs — keep your car and specs selected",
      color:MD3.warning,
      available: tuneGenerated,
      action: () => { onResetTune(); onClose(); },
      needsConfirm: true,
    },
    {
      id:"all",
      icon:"⚠",
      label:"Full reset",
      desc:"Wipe everything — car, specs, tune, saved session. Cannot be undone.",
      color:MD3.error,
      available: true,
      action: () => { onResetAll(); onClose(); },
      needsConfirm: true,
    },
  ];

  return (
    <div style={{position:"fixed",inset:0,background:"#000a",zIndex:500,maxWidth:1200,margin:"0 auto",display:"flex",flexDirection:"column",justifyContent:"flex-end"}}>
      <div onClick={onClose} style={{position:"absolute",inset:0,zIndex:0}}/>
      <div style={{position:"relative",zIndex:1,background:MD3.surfaceVar,borderRadius:"20px 20px 0 0",border:`1px solid ${MD3.outline}`,paddingBottom:24}}>
        <div style={{width:36,height:4,borderRadius:2,background:MD3.outline,margin:"12px auto 0"}}/>
        <div style={{padding:"14px 20px 12px",display:"flex",alignItems:"center",justifyContent:"space-between",borderBottom:`1px solid ${MD3.outlineVar}`}}>
          <span style={{fontFamily:"Roboto,sans-serif",fontSize:16,fontWeight:700,color:MD3.onSurface}}>Reset Options</span>
          <button onClick={onClose} style={{background:"transparent",border:"none",color:MD3.onSurfaceVar,fontSize:20,cursor:"pointer"}}>✕</button>
        </div>
        <div style={{padding:"12px 20px"}}>
          {options.map(opt=>(
            <div key={opt.id} style={{marginBottom:10}}>
              {confirming===opt.id
                ? <div style={{background:opt.color+"14",border:`1.5px solid ${opt.color}44`,borderRadius:14,padding:"14px 16px"}}>
                    <div style={{fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:600,color:opt.color,marginBottom:10}}>Are you sure? This cannot be undone.</div>
                    <div style={{display:"grid",gridTemplateColumns:"1fr 1fr",gap:8}}>
                      <button onClick={()=>setConfirming(null)} style={{padding:"10px",background:"transparent",border:`1px solid ${MD3.outline}`,borderRadius:10,color:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontSize:13,cursor:"pointer"}}>Cancel</button>
                      <button onClick={opt.action} style={{padding:"10px",background:opt.color+"22",border:`1.5px solid ${opt.color}55`,borderRadius:10,color:opt.color,fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:700,cursor:"pointer"}}>Confirm</button>
                    </div>
                  </div>
                : <button
                    onClick={()=>{ if(!opt.available) return; if(opt.needsConfirm) setConfirming(opt.id); else opt.action(); }}
                    disabled={!opt.available}
                    style={{width:"100%",padding:"14px 16px",borderRadius:14,textAlign:"left",border:`1px solid ${opt.available?opt.color+"44":MD3.outlineVar}`,background:opt.available?opt.color+"0e":"transparent",cursor:opt.available?"pointer":"not-allowed",opacity:opt.available?1:0.4,transition:"all 0.12s",display:"flex",alignItems:"center",gap:12}}
                  >
                    <span style={{fontSize:20,flexShrink:0}}>{opt.icon}</span>
                    <div>
                      <div style={{fontFamily:"Roboto,sans-serif",fontSize:14,fontWeight:600,color:opt.available?opt.color:MD3.onSurfaceLow,marginBottom:3}}>{opt.label}</div>
                      <div style={{fontFamily:"Roboto,sans-serif",fontSize:12,color:MD3.onSurfaceVar,lineHeight:1.4}}>{opt.desc}{!opt.available?" — not available yet":""}</div>
                    </div>
                  </button>
              }
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

// ─── ONBOARDING SLIDES ────────────────────────────────────────────────────────
const SLIDES=[
  {icon:"🏁",title:"Welcome to TuneLab",body:"The AI-assisted Forza Horizon tuning calculator. Build setups in seconds — no guesswork, real physics.",color:"#4fc3f7"},
  {icon:"⚙",title:"How It Works",body:"Fill in your car, engine, and track details. TuneLab calculates a full tune using real suspension math — tires, gearing, springs, diffs, everything.",color:"#69f0ae"},
  {icon:"✦",title:"AI Is Optional",body:"Connect Gemini (free), Grok, or another AI provider for richer analysis. The offline calculator is always free and always works.",color:"#4285f4"},
  {icon:"🔧",title:"Tuning Wizard",body:"After your test drive, tap the Wizard to diagnose any handling issue. It gives specific fixes based on your car and setup.",color:"#ff9800"},
];

// ─── MD3 COMPONENTS ───────────────────────────────────────────────────────────
const s = (extra={}) => ({fontFamily:"'Roboto',sans-serif",...extra});

function Chip({label,selected,onClick,color,icon,small}){
  return <button onClick={onClick} style={{display:"inline-flex",alignItems:"center",gap:4,padding:small?"4px 10px":"6px 14px",borderRadius:20,border:`1.5px solid ${selected?color:MD3.outline}`,background:selected?color+"1e":"transparent",color:selected?color:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontWeight:500,fontSize:small?11:13,cursor:"pointer",transition:"all 0.15s",whiteSpace:"nowrap"}}>{icon&&<span style={{fontSize:small?10:12}}>{icon}</span>}{label}</button>;
}
function Label({children,color=MD3.onSurfaceVar,mt=0}){
  return <div style={{fontFamily:"Roboto,sans-serif",fontSize:11,fontWeight:500,color,letterSpacing:"0.08em",textTransform:"uppercase",marginBottom:8,marginTop:mt}}>{children}</div>;
}
function SectionTitle({children,color,mt=16}){
  return <div style={{display:"flex",alignItems:"center",gap:10,marginBottom:14,marginTop:mt}}><div style={{width:3,height:18,background:color,borderRadius:2}}/><span style={{fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:700,color,letterSpacing:"0.04em",textTransform:"uppercase"}}>{children}</span></div>;
}
function NumField({label,value,onChange,unit="",min,max,step=1,hint}){
  const [f,setF]=useState(false);
  return <div style={{marginBottom:12}}>
    <Label>{label}</Label>
    <div style={{display:"flex",alignItems:"center",gap:8}}>
      <input type="number" value={value} min={min} max={max} step={step}
        onChange={e=>{const v=parseFloat(e.target.value);if(!isNaN(v))onChange(v);}}
        onFocus={()=>setF(true)} onBlur={()=>setF(false)}
        style={{flex:1,background:f?MD3.surfaceC1:MD3.surfaceVar,border:`1.5px solid ${f?"#4fc3f7":MD3.outline}`,borderRadius:8,padding:"10px 12px",color:MD3.onSurface,fontFamily:"Roboto Mono,monospace",fontSize:14,outline:"none",transition:"border-color 0.15s"}}
      />
      {unit&&<span style={{fontFamily:"Roboto,sans-serif",fontSize:12,color:MD3.onSurfaceVar,minWidth:40}}>{unit}</span>}
    </div>
    {hint&&<div style={{fontFamily:"Roboto,sans-serif",fontSize:11,color:MD3.onSurfaceLow,marginTop:4,lineHeight:1.5}}>{hint}</div>}
  </div>;
}
function Select({label,value,onChange,options,hint}){
  return <div style={{marginBottom:12}}>
    {label&&<Label>{label}</Label>}
    <select value={value} onChange={e=>onChange(e.target.value)} style={{width:"100%",background:MD3.surfaceVar,border:`1.5px solid ${MD3.outline}`,borderRadius:8,padding:"10px 12px",color:MD3.onSurface,fontFamily:"Roboto,sans-serif",fontSize:13,outline:"none",cursor:"pointer"}}>
      {options.map(o=><option key={o.v??o} value={o.v??o}>{o.l??o}</option>)}
    </select>
    {hint&&<div style={{fontFamily:"Roboto,sans-serif",fontSize:11,color:MD3.onSurfaceLow,marginTop:4,lineHeight:1.5}}>{hint}</div>}
  </div>;
}
function CardOption({options,value,onChange,color}){
  return <div style={{display:"flex",flexDirection:"column",gap:6,marginBottom:14}}>
    {options.map(o=><button key={o.id} onClick={()=>onChange(o.id)} style={{padding:"12px 16px",borderRadius:12,textAlign:"left",border:`1.5px solid ${value===o.id?color:MD3.outline}`,background:value===o.id?color+"18":MD3.surfaceC1,cursor:"pointer",transition:"all 0.12s"}}>
      <div style={{display:"flex",alignItems:"center",gap:8,marginBottom:2}}>
        {o.icon&&<span style={{fontSize:16}}>{o.icon}</span>}
        <span style={{fontFamily:"Roboto,sans-serif",fontSize:14,fontWeight:600,color:value===o.id?color:MD3.onSurface}}>{o.label}</span>
        {value===o.id&&<span style={{marginLeft:"auto",fontFamily:"Roboto Mono,monospace",fontSize:9,color,letterSpacing:"0.1em"}}>✓</span>}
      </div>
      <div style={{fontFamily:"Roboto,sans-serif",fontSize:12,color:MD3.onSurfaceVar,paddingLeft:o.icon?24:0,lineHeight:1.5}}>{o.desc}</div>
    </button>)}
  </div>;
}
function Toast({msg,onDone}){
  useEffect(()=>{const t=setTimeout(onDone,2600);return()=>clearTimeout(t);},[]);
  return <div style={{position:"fixed",bottom:100,left:"50%",transform:"translateX(-50%)",zIndex:999,background:MD3.surfaceC3,border:`1px solid ${MD3.outline}`,borderRadius:24,padding:"10px 22px",fontFamily:"Roboto,sans-serif",fontSize:13,color:MD3.onSurface,boxShadow:"0 4px 20px #0009",pointerEvents:"none",whiteSpace:"nowrap"}}>{msg}</div>;
}
function TuneRow({k,v,color,note}){
  return <div style={{padding:"10px 16px",borderBottom:`1px solid ${MD3.outlineVar}`,display:"flex",alignItems:"flex-start",justifyContent:"space-between",gap:12}}>
    <div style={{flex:1}}>
      <div style={{fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:500,color:MD3.onSurface}}>{k}</div>
      {note&&<div style={{fontFamily:"Roboto,sans-serif",fontSize:11,color:MD3.onSurfaceLow,marginTop:2,lineHeight:1.45}}>{note}</div>}
    </div>
    <div style={{fontFamily:"Roboto Mono,monospace",fontSize:13,color,fontWeight:700,whiteSpace:"nowrap",paddingTop:1}}>{v}</div>
  </div>;
}
function TuneCard({data,color,name}){
  return <div style={{background:MD3.surfaceC1,border:`1px solid ${color}44`,borderRadius:16,overflow:"hidden",boxShadow:`0 2px 12px ${color}12`}}>
    <div style={{padding:"12px 16px",borderBottom:`1px solid ${MD3.outlineVar}`,display:"flex",alignItems:"center",gap:8,background:color+"0e"}}>
      <div style={{width:6,height:6,borderRadius:"50%",background:color,boxShadow:`0 0 8px ${color}`}}/>
      <span style={{fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:700,color,letterSpacing:"0.06em",textTransform:"uppercase"}}>{name}</span>
    </div>
    {(data.values||[]).map((r,i)=><TuneRow key={i} k={r.key} v={r.value} color={color} note={r.note}/>)}
    {data.tip&&<div style={{padding:"10px 16px",background:color+"0a"}}><span style={{fontFamily:"Roboto,sans-serif",fontSize:12,color:color+"cc",lineHeight:1.55}}>💡 {data.tip}</span></div>}
  </div>;
}

// ─── ONBOARDING ───────────────────────────────────────────────────────────────
function Onboarding({onDone}){
  const [slide,setSlide]=useState(0);
  const sl=SLIDES[slide];
  return <div style={{minHeight:"100vh",background:MD3.surface,display:"flex",flexDirection:"column",alignItems:"center",justifyContent:"space-between",padding:"60px 32px 40px",maxWidth:480,margin:"0 auto"}}>
    <style>{`@import url('https://fonts.googleapis.com/css2?family=Roboto:wght@400;500;700;900&family=Roboto+Mono:wght@700&display=swap');*{box-sizing:border-box}`}</style>
    <div style={{textAlign:"center",flex:1,display:"flex",flexDirection:"column",alignItems:"center",justifyContent:"center"}}>
      <div style={{fontSize:72,marginBottom:24}}>{sl.icon}</div>
      <div style={{fontFamily:"Roboto,sans-serif",fontSize:26,fontWeight:900,color:MD3.onSurface,marginBottom:16,lineHeight:1.2}}>{sl.title}</div>
      <div style={{fontFamily:"Roboto,sans-serif",fontSize:15,color:MD3.onSurfaceVar,lineHeight:1.7,maxWidth:300}}>{sl.body}</div>
    </div>
    <div style={{width:"100%"}}>
      <div style={{display:"flex",justifyContent:"center",gap:8,marginBottom:32}}>
        {SLIDES.map((_,i)=><div key={i} style={{width:i===slide?24:6,height:6,borderRadius:3,background:i===slide?sl.color:MD3.outline,transition:"all 0.3s"}}/>)}
      </div>
      {slide<SLIDES.length-1
        ?<button onClick={()=>setSlide(s=>s+1)} style={{width:"100%",padding:"16px",borderRadius:100,background:sl.color,border:"none",color:"#000",fontFamily:"Roboto,sans-serif",fontSize:15,fontWeight:700,cursor:"pointer",marginBottom:12}}>Next</button>
        :<button onClick={onDone} style={{width:"100%",padding:"16px",borderRadius:100,background:sl.color,border:"none",color:"#000",fontFamily:"Roboto,sans-serif",fontSize:15,fontWeight:700,cursor:"pointer",marginBottom:12}}>Let&apos;s go →</button>
      }
      <button onClick={onDone} style={{width:"100%",padding:"12px",borderRadius:100,background:"transparent",border:`1px solid ${MD3.outline}`,color:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontSize:13,cursor:"pointer"}}>
        I know what I&apos;m doing — skip
      </button>
    </div>
  </div>;
}

// ─── UNITS SCREEN ─────────────────────────────────────────────────────────────
function UnitsScreen({onDone, currentUnits}){
  const [preset,setPreset]=useState(()=>{
    // Detect current preset from units object
    if(currentUnits?.weight==="kg") return "metric";
    return "imperial";
  });
  const accent="#4fc3f7";

  return <div style={{position:"fixed",inset:0,background:"#0009",zIndex:500,maxWidth:1200,margin:"0 auto",display:"flex",flexDirection:"column",justifyContent:"flex-end"}}>
    <style>{`@import url('https://fonts.googleapis.com/css2?family=Roboto:wght@400;500;700;900&family=Roboto+Mono:wght@700&display=swap');*{box-sizing:border-box}`}</style>

    {/* Backdrop tap to dismiss */}
    <div onClick={()=>onDone(UNIT_PRESETS[preset])} style={{position:"absolute",inset:0,zIndex:0}}/>

    {/* Sheet */}
    <div style={{position:"relative",zIndex:1,background:MD3.surfaceVar,borderRadius:"20px 20px 0 0",border:`1px solid ${MD3.outline}`,padding:"0 0 24px"}}>

      {/* Handle */}
      <div style={{width:36,height:4,borderRadius:2,background:MD3.outline,margin:"12px auto 0"}}/>

      {/* Header */}
      <div style={{padding:"14px 20px 16px",display:"flex",alignItems:"center",justifyContent:"space-between",borderBottom:`1px solid ${MD3.outlineVar}`}}>
        <div style={{fontFamily:"Roboto,sans-serif",fontSize:16,fontWeight:700,color:MD3.onSurface}}>Display Units</div>
        <button onClick={()=>onDone(UNIT_PRESETS[preset])} style={{background:accent+"22",border:`1.5px solid ${accent}55`,borderRadius:20,padding:"7px 20px",fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:700,color:accent,cursor:"pointer"}}>
          Done
        </button>
      </div>

      <div style={{padding:"16px 20px"}}>
        <div style={{fontFamily:"Roboto,sans-serif",fontSize:13,color:MD3.onSurfaceVar,marginBottom:16,lineHeight:1.55}}>
          Match the units in your Forza Horizon tuning menu. Values already entered will be converted automatically.
        </div>

        {/* Two cards */}
        <div style={{display:"flex",flexDirection:"column",gap:10,marginBottom:16}}>
          {Object.entries(UNIT_PRESETS).map(([key,val])=>(
            <button key={key} onClick={()=>setPreset(key)} style={{
              width:"100%",padding:"16px 18px",borderRadius:14,textAlign:"left",cursor:"pointer",
              border:`2px solid ${preset===key?accent:MD3.outline}`,
              background:preset===key?accent+"18":MD3.surfaceC1,
              transition:"all 0.15s",display:"flex",alignItems:"center",gap:14,
            }}>
              <div style={{width:20,height:20,borderRadius:"50%",border:`2px solid ${preset===key?accent:MD3.outline}`,background:preset===key?accent:"transparent",flexShrink:0,display:"flex",alignItems:"center",justifyContent:"center"}}>
                {preset===key&&<div style={{width:8,height:8,borderRadius:"50%",background:MD3.surface}}/>}
              </div>
              <div>
                <div style={{fontFamily:"Roboto,sans-serif",fontSize:15,fontWeight:700,color:preset===key?accent:MD3.onSurface,marginBottom:3}}>{val.label}</div>
                <div style={{fontFamily:"Roboto Mono,monospace",fontSize:11,color:preset===key?accent+"bb":MD3.onSurfaceLow}}>{val.sub}</div>
              </div>
            </button>
          ))}
        </div>

        <div style={{background:MD3.surfaceC1,borderRadius:10,padding:"10px 14px",border:`1px solid ${MD3.outline}`}}>
          <div style={{fontFamily:"Roboto,sans-serif",fontSize:11,color:MD3.onSurfaceVar,lineHeight:1.55}}>
            <strong style={{color:MD3.onSurface}}>Not sure?</strong> Check your Forza tuning screen — if spring rates show lbs/in, choose Imperial. If bar and kg, choose Metric. PS is used in Japanese/EU menus instead of hp.
          </div>
        </div>
      </div>
    </div>
  </div>;
}


// ─── AI SCREEN ────────────────────────────────────────────────────────────────
function AIScreen({onClose}){
  const [provider,setProvider]=useState(LS.get("tl_provider","none"));
  const [keys,setKeys]=useState(LS.get("tl_keys",{}));
  const [show,setShow]=useState({});
  const [saved,setSaved]=useState(false);
  const prov=AI_PROVIDERS.find(p=>p.id===provider)||AI_PROVIDERS[0];
  const save=()=>{LS.set("tl_provider",provider);LS.set("tl_keys",keys);setSaved(true);setTimeout(()=>setSaved(false),2000);};
  return <div style={{position:"fixed",inset:0,background:MD3.surface,zIndex:400,maxWidth:480,margin:"0 auto",display:"flex",flexDirection:"column",fontFamily:"Roboto,sans-serif",overflowY:"auto"}}>
    <div style={{background:MD3.surfaceVar,borderBottom:`1px solid ${MD3.outline}`,padding:"14px 18px",display:"flex",alignItems:"center",gap:14,flexShrink:0}}>
      <button onClick={onClose} style={{background:"transparent",border:"none",color:MD3.onSurface,fontSize:22,cursor:"pointer",padding:0}}>←</button>
      <span style={{fontFamily:"Roboto,sans-serif",fontSize:16,fontWeight:700,color:MD3.onSurface}}>AI Provider</span>
      <button onClick={save} style={{marginLeft:"auto",background:saved?"#66bb6a22":"#4fc3f722",border:`1px solid ${saved?"#66bb6a55":"#4fc3f755"}`,borderRadius:20,padding:"6px 18px",fontFamily:"Roboto,sans-serif",fontSize:13,color:saved?"#66bb6a":"#4fc3f7",fontWeight:700,cursor:"pointer"}}>{saved?"✓ Saved":"Save"}</button>
    </div>
    <div style={{padding:"20px 18px 40px"}}>
      <p style={{fontFamily:"Roboto,sans-serif",fontSize:13,color:MD3.onSurfaceVar,lineHeight:1.65,marginBottom:20}}>TuneLab works fully <strong style={{color:MD3.onSurface}}>offline</strong> — the physics engine calculates your tune instantly. Connect an AI for richer analysis and smarter wizard diagnoses.</p>
      <SectionTitle color="#4fc3f7" mt={0}>Choose Provider</SectionTitle>
      {AI_PROVIDERS.map(p=><button key={p.id} onClick={()=>setProvider(p.id)} style={{width:"100%",padding:"14px 16px",borderRadius:12,textAlign:"left",border:`1.5px solid ${provider===p.id?p.color:MD3.outline}`,background:provider===p.id?p.color+"18":MD3.surfaceC1,cursor:"pointer",marginBottom:8,transition:"all 0.12s",display:"block"}}>
        <div style={{display:"flex",alignItems:"center",gap:10,marginBottom:4}}>
          <span style={{fontSize:16,color:provider===p.id?p.color:MD3.onSurfaceVar}}>{p.icon}</span>
          <span style={{fontFamily:"Roboto,sans-serif",fontSize:14,fontWeight:600,color:provider===p.id?p.color:MD3.onSurface}}>{p.label}</span>
          {p.free&&<span style={{background:"#66bb6a22",border:"1px solid #66bb6a44",borderRadius:12,padding:"1px 8px",fontFamily:"Roboto,sans-serif",fontSize:10,color:"#66bb6a",fontWeight:700,marginLeft:4}}>FREE</span>}
          {provider===p.id&&<span style={{marginLeft:"auto",fontFamily:"Roboto Mono,monospace",fontSize:9,color:p.color}}>ACTIVE ✓</span>}
        </div>
        <div style={{fontFamily:"Roboto,sans-serif",fontSize:12,color:MD3.onSurfaceVar,paddingLeft:26,lineHeight:1.45}}>{p.desc}</div>
      </button>)}
      {prov.keyLabel&&<><SectionTitle color={prov.color} mt={20}>API Key — {prov.label}</SectionTitle>
        <div style={{background:MD3.surfaceC1,border:`1px solid ${prov.color}33`,borderRadius:12,padding:"16px"}}>
          <p style={{fontFamily:"Roboto,sans-serif",fontSize:12,color:MD3.onSurfaceVar,lineHeight:1.6,marginBottom:12}}>Get your key at <span style={{color:prov.color}}>{prov.docsUrl}</span>. Stored only on your device.</p>
          <div style={{position:"relative"}}>
            <input type={show[prov.id]?"text":"password"} value={keys[prov.id]||""} onChange={e=>setKeys(k=>({...k,[prov.id]:e.target.value}))} placeholder={prov.keyHint} style={{width:"100%",background:MD3.surfaceVar,border:`1.5px solid ${MD3.outline}`,borderRadius:8,padding:"10px 42px 10px 12px",color:MD3.onSurface,fontFamily:"Roboto Mono,monospace",fontSize:13,outline:"none"}}/>
            <button onClick={()=>setShow(s=>({...s,[prov.id]:!s[prov.id]}))} style={{position:"absolute",right:10,top:"50%",transform:"translateY(-50%)",background:"transparent",border:"none",color:MD3.onSurfaceVar,cursor:"pointer",fontSize:14}}>{show[prov.id]?"🙈":"👁"}</button>
          </div>
          {keys[prov.id]&&<p style={{fontFamily:"Roboto,sans-serif",fontSize:11,color:"#66bb6a",marginTop:6}}>✓ Key entered — tap Save to activate</p>}
        </div>
      </>}
      <div style={{background:MD3.surfaceC1,border:`1px solid ${MD3.outline}`,borderRadius:12,overflow:"hidden",marginTop:20}}>
        <div style={{padding:"12px 16px",borderBottom:`1px solid ${MD3.outlineVar}`,fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:700,color:MD3.onSurfaceVar,letterSpacing:"0.06em",textTransform:"uppercase"}}>Cost per tune</div>
        {[{l:"Offline only",c:"$0.000",n:"Always free"},{l:"Gemini Flash",c:"$0.000",n:"Free tier 1500/day"},{l:"Grok Mini",c:"~$0.000",n:"Free tier avail."},{l:"GPT-4o-mini",c:"~$0.0002",n:"Very cheap"},{l:"Claude Haiku",c:"~$0.0003",n:"Best quality"}].map((r,i)=><div key={i} style={{padding:"10px 16px",borderBottom:i<4?`1px solid ${MD3.outlineVar}`:"none",display:"flex",justifyContent:"space-between",alignItems:"center"}}>
          <div><div style={{fontFamily:"Roboto,sans-serif",fontSize:13,color:MD3.onSurface,fontWeight:500}}>{r.l}</div><div style={{fontFamily:"Roboto,sans-serif",fontSize:11,color:MD3.onSurfaceLow}}>{r.n}</div></div>
          <div style={{fontFamily:"Roboto Mono,monospace",fontSize:12,color:i===0?"#66bb6a":MD3.warning,fontWeight:700}}>{r.c}</div>
        </div>)}
      </div>
      <a href="https://ko-fi.com/tunelabs" target="_blank" rel="noreferrer" style={{display:"block",marginTop:16,padding:"14px",background:"#ffd74018",border:"1px solid #ffd74044",borderRadius:12,color:"#ffd740",fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:700,textDecoration:"none",textAlign:"center"}}>☕ Support free access — Ko-fi</a>
    </div>
  </div>;
}

// ─── SAVE DRAWER ──────────────────────────────────────────────────────────────
function SaveDrawer({appState,tunePages,onLoad,onClose}){
  const [saves,setSaves]=useState(loadSaves());
  const [name,setName]=useState(`${appState.make} ${appState.model}`);
  const [toast,setToast]=useState(null);
  const color=tuneColor(appState.tuneId);
  const doSave=()=>{if(!Object.keys(tunePages).length){setToast("Generate a tune first");return;}const e={id:Date.now(),name,date:new Date().toLocaleDateString(),appState:{...appState},tunePages:{...tunePages}};const u=[e,...saves.slice(0,19)];writeSaves(u);setSaves(u);setToast("Saved!");};
  const doDelete=id=>{const u=saves.filter(s=>s.id!==id);writeSaves(u);setSaves(u);};
  const doCopy=e=>{const t=formatTuneText(e.appState,e.tunePages);navigator.clipboard?.writeText(t).then(()=>setToast("Copied!"));};
  const doShare=e=>{const t=formatTuneText(e.appState,e.tunePages);navigator.share?.({title:"TuneLab Tune",text:t}).catch(()=>{})||navigator.clipboard?.writeText(t).then(()=>setToast("Copied!"));};
  const doExport=e=>{const b=new Blob([JSON.stringify(e,null,2)],{type:"application/json"});const u=URL.createObjectURL(b);const a=document.createElement("a");a.href=u;a.download=`tunelab_${e.name.replace(/\s+/g,"_")}.json`;a.click();URL.revokeObjectURL(u);setToast("Exported!");};
  return <div style={{position:"fixed",inset:0,background:"#000a",zIndex:300,display:"flex",flexDirection:"column",justifyContent:"flex-end",maxWidth:480,margin:"0 auto"}}>
    {toast&&<Toast msg={toast} onDone={()=>setToast(null)}/>}
    <div style={{background:MD3.surfaceVar,borderRadius:"20px 20px 0 0",border:`1px solid ${MD3.outline}`,maxHeight:"85vh",display:"flex",flexDirection:"column"}}>
      <div style={{width:36,height:4,borderRadius:2,background:MD3.outline,margin:"12px auto 0"}}/>
      <div style={{padding:"12px 18px",display:"flex",alignItems:"center",justifyContent:"space-between",borderBottom:`1px solid ${MD3.outlineVar}`,flexShrink:0}}>
        <span style={{fontFamily:"Roboto,sans-serif",fontSize:16,fontWeight:700,color:MD3.onSurface}}>Saved Tunes</span>
        <button onClick={onClose} style={{background:"transparent",border:"none",color:MD3.onSurfaceVar,fontSize:20,cursor:"pointer"}}>✕</button>
      </div>
      <div style={{flex:1,overflowY:"auto",padding:"16px 18px 24px"}}>
        <SectionTitle color={color} mt={0}>Save Current</SectionTitle>
        <input value={name} onChange={e=>setName(e.target.value)} placeholder="Tune name…" style={{width:"100%",background:MD3.surfaceC1,border:`1.5px solid ${MD3.outline}`,borderRadius:8,padding:"10px 12px",color:MD3.onSurface,fontFamily:"Roboto,sans-serif",fontSize:13,outline:"none",marginBottom:10}}/>
        <button onClick={doSave} style={{width:"100%",padding:"13px",background:color+"22",border:`1.5px solid ${color}55`,borderRadius:12,color,fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:700,cursor:"pointer"}}>💾 Save Tune</button>
        {saves.length>0&&<><SectionTitle color={color} mt={20}>Library ({saves.length})</SectionTitle>
          {saves.map(sv=>{const sc=tuneColor(sv.appState?.tuneId||"Race");const tm=TUNE_MODES.find(t=>t.id===sv.appState?.tuneId);return <div key={sv.id} style={{background:MD3.surfaceC1,border:`1px solid ${MD3.outline}`,borderRadius:12,padding:"12px 14px",marginBottom:8}}>
            <div style={{display:"flex",justifyContent:"space-between",marginBottom:8}}>
              <div><div style={{fontFamily:"Roboto,sans-serif",fontSize:14,fontWeight:600,color:MD3.onSurface}}>{sv.name}</div><div style={{fontFamily:"Roboto,sans-serif",fontSize:11,color:MD3.onSurfaceVar,marginTop:2}}>{tm?.icon} {tm?.label} · {sv.appState?.carClass} {sv.appState?.pi}PI · {sv.date}</div></div>
              <button onClick={()=>doDelete(sv.id)} style={{background:"transparent",border:"none",color:MD3.onSurfaceLow,fontSize:16,cursor:"pointer",padding:0}}>✕</button>
            </div>
            <div style={{display:"grid",gridTemplateColumns:"1fr 1fr 1fr",gap:6,marginBottom:6}}>
              {[{l:"Load",fn:()=>{onLoad(sv);onClose();},c:sc},{l:"Share",fn:()=>doShare(sv),c:MD3.success},{l:"Copy",fn:()=>doCopy(sv),c:MD3.onSurfaceVar}].map(b=><button key={b.l} onClick={b.fn} style={{padding:"8px",background:b.c+"18",border:`1px solid ${b.c}44`,borderRadius:8,color:b.c,fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:600,cursor:"pointer"}}>{b.l}</button>)}
            </div>
            <button onClick={()=>doExport(sv)} style={{width:"100%",padding:"7px",background:"transparent",border:`1px solid ${MD3.outline}`,borderRadius:8,color:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontSize:11,cursor:"pointer"}}>⬇ Export JSON</button>
          </div>;})}
        </>}
        {saves.length===0&&<div style={{textAlign:"center",padding:"30px 0",fontFamily:"Roboto,sans-serif",fontSize:13,color:MD3.onSurfaceLow}}>No saved tunes yet</div>}
      </div>
    </div>
  </div>;
}

// ─── WIZARD ───────────────────────────────────────────────────────────────────
function Wizard({ctx,onClose}){
  const [step,setStep]=useState("problem");const [selP,setSP]=useState(null);const [fix,setFix]=useState(null);const [loading,setLoading]=useState(false);const [err,setErr]=useState(null);
  const provider=LS.get("tl_provider","none");const keys=LS.get("tl_keys",{});const hasAI=provider!=="none"&&!!keys[provider];const prov=AI_PROVIDERS.find(p=>p.id===provider);
  const fixColor=hasAI?prov?.color||MD3.primary:MD3.warning;
  const getFix=async(problem,sub)=>{
    setStep("fixing");setLoading(true);setErr(null);
    try{
      if(hasAI){
        const sys=`You are a Forza Horizon tuning expert. Return ONLY raw JSON.`;
        const usr=`Issue: "${problem.label}" — "${sub.label}"\n${ctx.make} ${ctx.model} | ${ctx.driveType} | ${ctx.tuneLabel} | ${ctx.surface}\nEngine: ${ctx.aspiration} ${ctx.engineSwap!=="None (Stock)"?"("+ctx.engineSwap+")":""} | Input: ${ctx.inputDevice}\nFH6 traction circle physics — braking reduces lateral grip.\nReturn: {"diagnosis":"1-2 sentences","fixes":[{"setting":"Name","change":"What","why":"Why"},{"setting":"Name","change":"What","why":"Why"},{"setting":"Name","change":"What","why":"Why"}],"tip":"One tip"}`;
        const raw=await callAI(provider,keys[provider],sys,usr);
        const m=raw.match(/\{[\s\S]*\}/);if(!m)throw new Error("parse");
        setFix(JSON.parse(m[0]));
      }else{setFix(OFFLINE_FIXES[problem.id]||OFFLINE_FIXES.understeer);}
      setStep("result");
    }catch(e){setFix(OFFLINE_FIXES[problem.id]||OFFLINE_FIXES.understeer);setStep("result");}
    setLoading(false);
  };
  const goBack=()=>{if(step==="sub"){setStep("problem");setSP(null);}if(step==="result"||step==="fixing"){setStep("sub");setFix(null);}};
  const rowS={padding:"14px 16px",display:"flex",alignItems:"center",justifyContent:"space-between",cursor:"pointer",borderBottom:`1px solid ${MD3.outlineVar}`};
  return <div style={{position:"fixed",inset:0,background:MD3.surface,zIndex:200,maxWidth:480,margin:"0 auto",display:"flex",flexDirection:"column",fontFamily:"Roboto,sans-serif"}}>
    <div style={{background:MD3.surfaceVar,borderBottom:`1px solid ${MD3.outline}`,padding:"14px 18px",display:"flex",alignItems:"center",gap:14,flexShrink:0}}>
      <button onClick={step==="problem"?onClose:goBack} style={{background:"transparent",border:"none",color:MD3.onSurface,fontSize:22,cursor:"pointer",padding:0}}>←</button>
      <span style={{fontFamily:"Roboto,sans-serif",fontSize:16,fontWeight:700,color:MD3.onSurface}}>Tuning Wizard</span>
      <div style={{marginLeft:"auto",display:"flex",alignItems:"center",gap:8}}>
        <span style={{fontFamily:"Roboto,sans-serif",fontSize:11,color:hasAI?MD3.success:MD3.warning}}>{hasAI?`✦ ${prov?.label}`:"⚙ Offline"}</span>
        <button onClick={onClose} style={{background:"transparent",border:"none",color:MD3.onSurfaceVar,fontSize:20,cursor:"pointer"}}>✕</button>
      </div>
    </div>
    <div style={{flex:1,overflowY:"auto",padding:"20px 18px 40px"}}>
      {step==="problem"&&<>
        <p style={{fontSize:13,color:MD3.onSurfaceVar,lineHeight:1.65,marginBottom:8}}>Debug any handling issue — works with any tune, not just ones built in TuneLab.</p>
        <p style={{fontSize:15,fontWeight:600,color:MD3.onSurface,marginBottom:18}}>What are you experiencing?</p>
        <div style={{borderRadius:12,overflow:"hidden",border:`1px solid ${MD3.outline}`}}>
          {PROBLEMS.map((p,i)=><div key={p.id} onClick={()=>{setSP(p);setStep("sub");}} style={{...rowS,borderBottom:i===PROBLEMS.length-1?"none":`1px solid ${MD3.outlineVar}`}}>
            <div><div style={{fontSize:15,fontWeight:500,color:MD3.onSurface}}>{p.icon} {p.label}</div><div style={{fontSize:12,color:MD3.onSurfaceVar,marginTop:2}}>{p.desc}</div></div>
            <span style={{color:MD3.onSurfaceLow,fontSize:20}}>›</span>
          </div>)}
        </div>
      </>}
      {step==="sub"&&selP&&<>
        <p style={{fontSize:16,fontWeight:600,color:MD3.onSurface,marginBottom:4}}>{selP.icon} {selP.label}</p>
        <p style={{fontSize:13,color:MD3.onSurfaceVar,marginBottom:18}}>When does this happen?</p>
        {err&&<div style={{color:MD3.error,fontSize:12,marginBottom:14,padding:"10px 14px",background:MD3.error+"14",borderRadius:8}}>{err}</div>}
        <div style={{borderRadius:12,overflow:"hidden",border:`1px solid ${MD3.outline}`}}>
          {selP.subs.map((sub,i)=><div key={sub.id} onClick={()=>getFix(selP,sub)} style={{...rowS,borderBottom:i===selP.subs.length-1?"none":`1px solid ${MD3.outlineVar}`}}>
            <span style={{fontSize:14,fontWeight:500,color:MD3.onSurface}}>{sub.label}</span>
            <span style={{color:MD3.onSurfaceLow,fontSize:20}}>›</span>
          </div>)}
        </div>
      </>}
      {step==="fixing"&&<div style={{textAlign:"center",paddingTop:80}}>
        <div style={{fontFamily:"Roboto,sans-serif",fontSize:14,fontWeight:600,color:fixColor,marginBottom:20}}>{hasAI?"Consulting AI…":"Checking knowledge base…"}</div>
        <div style={{display:"flex",justifyContent:"center",gap:6}}>{[0,1,2,3,4].map(i=><div key={i} style={{width:8,height:8,borderRadius:"50%",background:fixColor,animation:`pulse 1.2s ${i*0.15}s infinite`}}/>)}</div>
      </div>}
      {step==="result"&&fix&&<>
        {!hasAI&&<div style={{background:MD3.warning+"14",border:`1px solid ${MD3.warning}33`,borderRadius:12,padding:"10px 14px",marginBottom:14,display:"flex",gap:8,alignItems:"center"}}>
          <span style={{fontSize:16}}>⚙</span>
          <div><div style={{fontFamily:"Roboto,sans-serif",fontSize:11,fontWeight:700,color:MD3.warning,marginBottom:2}}>OFFLINE ANALYSIS</div><div style={{fontFamily:"Roboto,sans-serif",fontSize:11,color:MD3.onSurfaceVar}}>Connect an AI provider for car-specific diagnosis</div></div>
        </div>}
        <div style={{marginBottom:14}}>
          <div style={{fontFamily:"Roboto,sans-serif",fontSize:11,fontWeight:700,color:fixColor,letterSpacing:"0.06em",textTransform:"uppercase",marginBottom:8}}>Diagnosis</div>
          <div style={{fontFamily:"Roboto,sans-serif",fontSize:14,color:MD3.onSurface,lineHeight:1.65,background:MD3.surfaceC1,padding:"12px 14px",borderRadius:12,border:`1px solid ${MD3.outline}`}}>{fix.diagnosis}</div>
        </div>
        <div style={{marginBottom:14}}>
          <div style={{fontFamily:"Roboto,sans-serif",fontSize:11,fontWeight:700,color:fixColor,letterSpacing:"0.06em",textTransform:"uppercase",marginBottom:8}}>Recommended Changes</div>
          <div style={{borderRadius:12,overflow:"hidden",border:`1px solid ${MD3.outline}`}}>
            {(fix.fixes||[]).map((f,i)=><div key={i} style={{padding:"13px 14px",borderBottom:i===fix.fixes.length-1?"none":`1px solid ${MD3.outlineVar}`,background:i%2===0?MD3.surfaceC1:MD3.surfaceVar}}>
              <div style={{display:"flex",justifyContent:"space-between",gap:8,marginBottom:4}}>
                <span style={{fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:600,color:MD3.onSurface}}>{f.setting}</span>
                <span style={{fontFamily:"Roboto Mono,monospace",fontSize:12,color:fixColor,whiteSpace:"nowrap"}}>{f.change}</span>
              </div>
              <div style={{fontFamily:"Roboto,sans-serif",fontSize:12,color:MD3.onSurfaceVar,lineHeight:1.5}}>{f.why}</div>
            </div>)}
          </div>
        </div>
        {fix.tip&&<div style={{background:fixColor+"0d",border:`1px solid ${fixColor}33`,borderRadius:12,padding:"12px 14px",marginBottom:16}}>
          <div style={{fontFamily:"Roboto,sans-serif",fontSize:11,fontWeight:700,color:fixColor,letterSpacing:"0.06em",textTransform:"uppercase",marginBottom:6}}>Driving Tip</div>
          <div style={{fontFamily:"Roboto,sans-serif",fontSize:13,color:MD3.onSurface,lineHeight:1.6}}>💡 {fix.tip}</div>
        </div>}
        <button onClick={()=>{setStep("problem");setSP(null);setFix(null);setErr(null);}} style={{width:"100%",padding:"13px",background:"transparent",border:`1px solid ${MD3.outline}`,borderRadius:12,color:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontSize:13,cursor:"pointer"}}>← Diagnose another issue</button>
      </>}
    </div>
  </div>;
}

// ─── MAIN APP ─────────────────────────────────────────────────────────────────
export default function ForzaTuner(){
  // First-launch flags
  const [showOnboarding,setShowOnboarding]=useState(()=>!LS.get("tl_seen_onboarding",false));
  const [showUnits,setShowUnits]=useState(()=>!LS.get("tl_units_set",false));

  const [units,setUnits]=useState(()=>LS.get("tl_units",{weight:"lbs",springs:"lbs/in",pressure:"psi",speed:"mph"}));
  const [overlay,setOverlay]=useState(null);
  const [showAbout, setShowAbout] = useState(false);
  const [showQA,    setShowQA]    = useState(false);
  const [showReset, setShowReset] = useState(false);
  const [toast,setToast]=useState(null);

  // Car
  const [make,setMake]=useState("Nissan");
  const [model,setModel]=useState("GT-R Premium '17");
  // Engine
  const [engineSwap,setEngineSwap]=useState("None (Stock)");
  const [aspiration,setAspiration]=useState("turbo");
  const [engineChar,setEngineChar]=useState("balanced");
  const [inputDevice,setInputDevice]=useState("controller");
  // Tune config
  const [tuneId,setTuneId]=useState("Race");
  const [driveType,setDriveType]=useState("AWD");
  const [surface,setSurface]=useState("Road");
  const [carClass,setCarClass]=useState("S1");
  const [pi,setPi]=useState(850);
  // Tires
  const [compound,setCompound]=useState("Race Slick");
  const [tireWF,setTireWF]=useState("255");
  const [tireWR,setTireWR]=useState("275");
  const [psiF,setPsiF]=useState(28.5);
  const [psiR,setPsiR]=useState(28.5);
  // Aero
  const [hasAero,setHasAero]=useState(true);
  const [aeroF,setAeroF]=useState(150);
  const [aeroR,setAeroR]=useState(220);
  const [dragCd,setDragCd]=useState(0.32);
  // Specs
  const [weight,setWeight]=useState(3836);
  const [hp,setHp]=useState(620);
  const [torque,setTorque]=useState(547);
  const [topspeed,setTopspeed]=useState(180);
  const [gears,setGears]=useState(6);
  const [rpm,setRpm]=useState(6800);
  // UI state
  const [tab,setTab]=useState("car");
  const [visited,setVisited]=useState(new Set(["car"]));
  const [tunePage,setTunePage]=useState("Tires");
  const [tunePages,setTunePages]=useState({});
  const [loading,setLoading]=useState(false);
  const [aiEnhancing,setAiEnhancing]=useState(false);
  const [error,setError]=useState(null);
  const [tuneGenerated,setTuneGenerated]=useState(false);
  // Window width for responsive layout
  const [winW,setWinW]=useState(()=>window.innerWidth);
  useEffect(()=>{const h=()=>setWinW(window.innerWidth);window.addEventListener("resize",h);return()=>window.removeEventListener("resize",h);},[]);

  const isWide   = winW >= 700;  // DeX / tablet two-column
  const goTab    = t=>{setTab(t);setVisited(v=>new Set([...v,t]));};
  const allVisited = MAIN_TABS.every(t=>visited.has(t));
  const progress   = MAIN_TABS.filter(t=>visited.has(t)).length/MAIN_TABS.length;
  const color      = tuneColor(tuneId);
  const tuneMode   = TUNE_MODES.find(t=>t.id===tuneId);
  const piRange    = PI_RANGES[carClass];
  const pUnit      = units.pressure;
  const spUnit     = units.speed;
  const wUnit      = units.weight;
  const sUnit      = units.springs;
  const provider   = LS.get("tl_provider","none");
  const keys       = LS.get("tl_keys",{});
  const hasAI      = provider!=="none"&&!!keys[provider];
  const provInfo   = AI_PROVIDERS.find(p=>p.id===provider);

  const getState = ()=>({make,model,tuneId,driveType,surface,carClass,pi,engineSwap,aspiration,engineChar,inputDevice,compound,tireWF,tireWR,hasAero,aeroF,aeroR,dragCd,weight,hp,torque,topspeed,gears,rpm,units});

  // Auto-fill car defaults from knowledge database
  const applyCarDefaults = (modelName) => {
    const k = getCarKnowledge(modelName);
    if(!k) return;
    if(k.drive) setDriveType(k.drive);
    if(k.asp)   setAspiration(k.asp);
    if(k.char)  setEngineChar(k.char);
    if(k.w)     setWeight(units.weight==="kg" ? Math.round(k.w/2.205) : k.w);
    if(k.hp)    setHp(units.power==="ps" ? Math.round(k.hp*HP_TO_PS) : k.hp);
    if(k.tq)    setTorque(units.weight==="lbs" ? k.tq : Math.round(k.tq*1.356));
    if(k.spd)   setTopspeed(units.speed==="km/h" ? Math.round(k.spd*1.609) : k.spd);
    if(k.rpm)   setRpm(k.rpm);
    if(k.g)     setGears(k.g);
  };

  // Run validation when model, drive, or swap changes
  const getConfigWarnings = () => validateConfig(model, driveType, engineSwap).warnings;

  // ── RESET HANDLERS ──────────────────────────────────────────────────────────
  // Level 1: Restore tune pages to last calculated output (undo Quick Adjust nudges)
  const [calcSnapshot, setCalcSnapshot] = useState({});
  const resetTuneValues = () => {
    if(Object.keys(calcSnapshot).length) {
      setTunePages({...calcSnapshot});
      setToast("Tune values restored to calculated output");
    }
  };

  // Level 2: Clear the generated tune, reset progress — keep car/specs
  const resetTune = () => {
    setTunePages({});
    setTuneGenerated(false);
    setCalcSnapshot({});
    setVisited(new Set(["car"]));
    setTab("car");
    setError(null);
    setToast("Tune cleared — ready for a new build");
  };

  // Level 3: Full factory reset — everything back to defaults
  const resetAll = () => {
    setMake("Nissan"); setModel("GT-R Premium '17");
    setEngineSwap("None (Stock)"); setAspiration("turbo");
    setEngineChar("balanced"); setInputDevice("controller");
    setTuneId("Race"); setDriveType("AWD");
    setSurface("Road"); setCarClass("S1"); setPi(850);
    setCompound("Race Slick"); setTireWF("255"); setTireWR("275");
    setPsiF(28.5); setPsiR(28.5);
    setHasAero(true); setAeroF(150); setAeroR(220); setDragCd(0.32);
    setWeight(3836); setHp(620); setTorque(547); setTopspeed(180);
    setGears(6); setRpm(6800);
    setTunePages({}); setTuneGenerated(false); setCalcSnapshot({});
    setVisited(new Set(["car"])); setTab("car"); setError(null);
    setToast("Full reset complete");
  };

  const loadTune = entry=>{
    const s=entry.appState;
    setMake(s.make||"Nissan");setModel(s.model||"GT-R Premium '17");setTuneId(s.tuneId||"Race");setDriveType(s.driveType||"AWD");setSurface(s.surface||"Road");setCarClass(s.carClass||"S1");setPi(s.pi||850);setEngineSwap(s.engineSwap||"None (Stock)");setAspiration(s.aspiration||"turbo");setEngineChar(s.engineChar||"balanced");setInputDevice(s.inputDevice||"controller");setCompound(s.compound||"Race Slick");setTireWF(s.tireWF||"255");setTireWR(s.tireWR||"275");setPsiF(s.psiF||28.5);setPsiR(s.psiR||28.5);setHasAero(s.hasAero!==undefined?s.hasAero:true);setAeroF(s.aeroF||150);setAeroR(s.aeroR||220);setDragCd(s.dragCd||0.32);setWeight(s.weight||3836);setHp(s.hp||620);setTorque(s.torque||547);setTopspeed(s.topspeed||180);setGears(s.gears||6);
    if(s.rpm) setRpm(s.rpm); if(s.units)setUnits(s.units);
    setTunePages(entry.tunePages||{});setTuneGenerated(true);setVisited(new Set(MAIN_TABS));setTab("tune");setTunePage("Tires");setToast("Tune loaded!");
  };

  const handleGenerate=async()=>{
    if(!allVisited||loading)return;
    setLoading(true);setError(null);setTunePages({});setTuneGenerated(false);goTab("tune");setTunePage("Tires");
    const st=getState();
    const offline=calcTune(st);
    // Strip _meta before storing/displaying
    const {_meta,...displayTune}=offline;
    setTunePages(displayTune);setTuneGenerated(true);setCalcSnapshot({...displayTune});setLoading(false);
    pushHistory(st, displayTune);
    // AI enhancement
    if(hasAI){
      setAiEnhancing(true);
      try{
        const {system,user}=buildEnhancePrompt(st,displayTune);
        const raw=await callAI(provider,keys[provider],system,user);
        const match=raw.match(/\{[\s\S]*\}/);
        if(match){
          const enhanced=JSON.parse(match[0]);
          const merged={};
          TUNE_PAGES.forEach(pg=>{
            if(!displayTune[pg])return;
            const ai=enhanced[pg];
            merged[pg]={values:displayTune[pg].values.map((row,i)=>({...row,note:ai?.values?.[i]?.note||row.note})),tip:ai?.tip||displayTune[pg].tip};
          });
          setTunePages(merged);setToast(`✦ Enhanced by ${provInfo?.label||"AI"}`);
        }
      }catch(e){setToast("Tune ready — AI enhancement skipped");}
      setAiEnhancing(false);
    }
  };

  const handleShareCard=()=>{
    const html=buildShareCard(getState(),tunePages);
    const blob=new Blob([html],{type:"text/html"});
    const url=URL.createObjectURL(blob);
    const a=document.createElement("a");a.href=url;a.download=`tunelab_${make}_${model}.html`.replace(/\s+/g,"_");a.click();
    URL.revokeObjectURL(url);setToast("Share card downloaded!");
  };

  // First-launch flow
  if(showOnboarding) return <Onboarding onDone={()=>{LS.set("tl_seen_onboarding",true);setShowOnboarding(false);}}/>;
  
  const TABS=[{id:"car",label:"Car"},{id:"engine",label:"Engine"},{id:"setup",label:"Setup"},{id:"specs",label:"Specs"},{id:"tune",label:"Tune"}];

  // DeX / wide layout: sidebar nav + content pane
  const wideLayout = isWide;

  return (
    <div style={{minHeight:"100vh",background:MD3.surface,color:MD3.onSurface,margin:"0 auto",fontFamily:"Roboto,sans-serif",position:"relative",display:wideLayout?"flex":"block",maxWidth:wideLayout?1200:480}}>
      <style>{`
        @import url('https://fonts.googleapis.com/css2?family=Roboto:wght@400;500;700;900&family=Roboto+Mono:wght@700&display=swap');
        *{box-sizing:border-box} select,input,textarea{color-scheme:dark}
        ::-webkit-scrollbar{width:4px;height:4px} ::-webkit-scrollbar-thumb{background:${MD3.outline};border-radius:4px}
        @keyframes pulse{0%,100%{opacity:1}50%{opacity:0.3}}
      `}</style>

      {/* OVERLAYS — all rendered at root level of main app */}
      {overlay==="wizard"&&<Wizard ctx={{make,model,driveType,tuneLabel:tuneMode?.label,surface,aspiration:ASPIRATIONS.find(a=>a.id===aspiration)?.label,engineSwap,engineChar,inputDevice:INPUT_DEVICES.find(d=>d.id===inputDevice)?.label}} onClose={()=>setOverlay(null)}/>}
      {overlay==="save"&&<SaveDrawer appState={getState()} tunePages={tunePages} onLoad={loadTune} onClose={()=>setOverlay(null)}/>}
      {overlay==="ai"&&<AIScreen onClose={()=>setOverlay(null)}/>}
      {showAbout&&<AboutScreen onClose={()=>setShowAbout(false)}/>}
      {showReset&&<ResetSheet
        onResetValues={resetTuneValues}
        onResetTune={resetTune}
        onResetAll={resetAll}
        hasSnapshot={Object.keys(calcSnapshot).length>0}
        tuneGenerated={tuneGenerated}
        onClose={()=>setShowReset(false)}
      />}
      {showQA&&<QuickAdjust tunePages={tunePages} setTunePages={setTunePages} activePage={tunePage} color={color} onClose={()=>setShowQA(false)}/>}
      {showUnits&&<UnitsScreen currentUnits={units} onDone={u=>{
        const from=units; const to=u;
        if(from.weight!==to.weight){
          if(to.weight==="kg")  { setWeight(v=>Math.round(v/2.205)); setTorque(v=>Math.round(v*1.356)); }
          if(to.weight==="lbs") { setWeight(v=>Math.round(v*2.205)); setTorque(v=>Math.round(v/1.356)); }
        }
        if(from.speed!==to.speed){
          if(to.speed==="km/h") setTopspeed(v=>Math.round(v*1.609));
          if(to.speed==="mph")  setTopspeed(v=>Math.round(v/1.609));
        }
        if(from.pressure!==to.pressure){
          if(to.pressure==="bar"){ setPsiF(v=>+((v/14.504).toFixed(2))); setPsiR(v=>+((v/14.504).toFixed(2))); }
          if(to.pressure==="psi"){ setPsiF(v=>+((v*14.504).toFixed(1))); setPsiR(v=>+((v*14.504).toFixed(1))); }
        }
        if(from.power!==to.power){
          if(to.power==="ps") setHp(v=>Math.round(v*HP_TO_PS));
          if(to.power==="hp") setHp(v=>Math.round(v*PS_TO_HP));
        }
        setUnits(u); LS.set("tl_units",u); LS.set("tl_units_set",true); setShowUnits(false);
      }}/>}
      {toast&&<Toast msg={toast} onDone={()=>setToast(null)}/>}

      {/* WIDE: LEFT NAVIGATION RAIL */}
      {wideLayout&&<div style={{width:240,background:MD3.surfaceVar,borderRight:`1px solid ${MD3.outline}`,display:"flex",flexDirection:"column",padding:"20px 12px",flexShrink:0,minHeight:"100vh"}}>
        <div style={{marginBottom:24,padding:"0 8px"}}>
          <div style={{fontFamily:"Roboto,sans-serif",fontSize:8,color:MD3.onSurfaceLow,letterSpacing:"0.3em",marginBottom:2}}>FORZA HORIZON</div>
          <div style={{fontFamily:"Roboto,sans-serif",fontSize:20,fontWeight:900,color}}>Tune<span style={{color:MD3.onSurface}}>Lab</span></div>
        </div>
        {TABS.map(t=>{const vis=visited.has(t.id);return <button key={t.id} onClick={()=>goTab(t.id)} style={{width:"100%",padding:"12px 16px",borderRadius:12,border:"none",background:tab===t.id?color+"22":"transparent",color:tab===t.id?color:vis?MD3.onSurface:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontSize:14,fontWeight:tab===t.id?700:500,cursor:"pointer",textAlign:"left",marginBottom:4,display:"flex",alignItems:"center",gap:10,transition:"all 0.15s"}}>
          <div style={{width:6,height:6,borderRadius:"50%",background:vis?color:MD3.outline,flexShrink:0}}/>
          {t.label}
          {tab===t.id&&<div style={{marginLeft:"auto",width:4,height:4,borderRadius:"50%",background:color}}/>}
        </button>;})}
        <div style={{flex:1}}/>
        <button onClick={()=>setOverlay("ai")} style={{width:"100%",padding:"10px 16px",borderRadius:12,border:`1px solid ${hasAI?provInfo?.color+"44":MD3.outline}`,background:hasAI?provInfo?.color+"18":"transparent",color:hasAI?provInfo?.color:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:600,cursor:"pointer",textAlign:"left",marginBottom:8}}>
          {hasAI?`✦ ${provInfo?.label}`:"⚙ AI Provider"}
        </button>
        <button onClick={()=>setOverlay("save")} style={{width:"100%",padding:"10px 16px",borderRadius:12,border:`1px solid ${MD3.outline}`,background:"transparent",color:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:600,cursor:"pointer",textAlign:"left",marginBottom:8}}>
          💾 Save / Load
        </button>
        <button onClick={()=>{if(!tuneGenerated){setShowUnits(true);}}} style={{width:"100%",padding:"10px 16px",borderRadius:12,border:`1px solid ${tuneGenerated?MD3.outlineVar:MD3.outline}`,background:"transparent",color:tuneGenerated?MD3.onSurfaceLow:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:600,cursor:tuneGenerated?"not-allowed":"pointer",textAlign:"left",marginBottom:8,opacity:tuneGenerated?0.5:1}} title={tuneGenerated?"Units locked after tune is generated — start a new tune to change":"Change display units"}>
          ⚙ Units{tuneGenerated?" 🔒":""}
        </button>
        <button onClick={()=>setShowAbout(true)} style={{width:"100%",padding:"10px 16px",borderRadius:12,border:`1px solid ${MD3.outline}`,background:"transparent",color:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:600,cursor:"pointer",textAlign:"left",marginBottom:8}}>
          ℹ About &amp; Support
        </button>
        <button onClick={()=>setShowReset(true)} style={{width:"100%",padding:"10px 16px",borderRadius:12,border:`1px solid ${MD3.error}44`,background:MD3.error+"0a",color:MD3.error,fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:600,cursor:"pointer",textAlign:"left"}}>
          ↺ Reset / Clear
        </button>
      </div>}

      {/* MAIN CONTENT */}
      <div style={{flex:1,display:"flex",flexDirection:"column",minHeight:"100vh",maxWidth:wideLayout?960:480,margin:"0 auto",width:"100%"}}>

        {/* MOBILE HEADER */}
        {!wideLayout&&<div style={{padding:"12px 16px 8px",background:`linear-gradient(180deg,${color}0c 0%,transparent 100%)`,borderBottom:`1px solid ${MD3.outline}`,position:"sticky",top:0,zIndex:10,backdropFilter:"blur(12px)"}}>
          <div style={{display:"flex",alignItems:"center",justifyContent:"space-between",marginBottom:8}}>
            <div>
              <div style={{fontFamily:"Roboto,sans-serif",fontSize:7,color:MD3.onSurfaceLow,letterSpacing:"0.3em",marginBottom:1}}>FORZA HORIZON</div>
              <div style={{fontFamily:"Roboto,sans-serif",fontSize:20,fontWeight:900,color,lineHeight:1}}>Tune<span style={{color:MD3.onSurface}}>Lab</span></div>
            </div>
            <div style={{display:"flex",gap:6,alignItems:"center"}}>
              <button onClick={()=>setOverlay("ai")} style={{background:hasAI?provInfo?.color+"18":"transparent",border:`1px solid ${hasAI?provInfo?.color+"44":MD3.outline}`,borderRadius:20,padding:"5px 10px",color:hasAI?provInfo?.color:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontSize:11,cursor:"pointer",fontWeight:600,display:"flex",alignItems:"center",gap:4}}>
                <span>{hasAI?provInfo?.icon:"⚙"}</span>{hasAI?provInfo?.label.split(" ")[0]:"AI"}
              </button>
              <button onClick={()=>setOverlay("save")} style={{background:"transparent",border:`1px solid ${MD3.outline}`,borderRadius:20,padding:"5px 10px",color:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontSize:13,cursor:"pointer"}}>💾</button>
              {!tuneGenerated&&<button onClick={()=>setShowUnits(true)} style={{background:"transparent",border:`1px solid ${MD3.outline}`,borderRadius:20,padding:"5px 10px",color:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontSize:13,cursor:"pointer"}}>⚙</button>}
              <button onClick={()=>setShowAbout(true)} style={{background:"transparent",border:`1px solid ${MD3.outline}`,borderRadius:20,padding:"5px 10px",color:MD3.onSurfaceVar,fontFamily:"Roboto,sans-serif",fontSize:13,cursor:"pointer"}}>ℹ</button>
              <button onClick={()=>setShowReset(true)} style={{background:MD3.error+"14",border:`1px solid ${MD3.error}33`,borderRadius:20,padding:"5px 10px",color:MD3.error,fontFamily:"Roboto,sans-serif",fontSize:12,cursor:"pointer"}}>↺</button>
              {tuneGenerated&&<button onClick={()=>setOverlay("wizard")} style={{background:MD3.error+"18",border:`1px solid ${MD3.error}44`,borderRadius:20,padding:"5px 10px",color:MD3.error,fontFamily:"Roboto,sans-serif",fontSize:13,cursor:"pointer"}}>🔧</button>}
            </div>
          </div>
          {/* Tune mode chips */}
          <div style={{display:"flex",gap:6,overflowX:"auto",scrollbarWidth:"none",paddingBottom:2}}>
            {TUNE_MODES.map(t=><Chip key={t.id} label={t.label} icon={t.icon} selected={tuneId===t.id} onClick={()=>setTuneId(t.id)} color={tuneColor(t.id)} small/>)}
          </div>
        </div>}

        {/* WIDE: TUNE MODES ROW */}
        {wideLayout&&<div style={{padding:"16px 24px 8px",borderBottom:`1px solid ${MD3.outline}`,display:"flex",gap:8,flexWrap:"wrap",alignItems:"center"}}>
          <span style={{fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:600,color:MD3.onSurfaceVar,marginRight:8}}>Mode:</span>
          {TUNE_MODES.map(t=><Chip key={t.id} label={t.label} icon={t.icon} selected={tuneId===t.id} onClick={()=>setTuneId(t.id)} color={tuneColor(t.id)}/>)}
          {tuneGenerated&&<button onClick={()=>setOverlay("wizard")} style={{marginLeft:"auto",background:MD3.error+"18",border:`1px solid ${MD3.error}44`,borderRadius:20,padding:"6px 14px",color:MD3.error,fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:600,cursor:"pointer"}}>🔧 Tuning Wizard</button>}
        </div>}

        {/* MOBILE TAB BAR */}
        {!wideLayout&&<div style={{display:"flex",background:MD3.surfaceVar,borderBottom:`1px solid ${MD3.outline}`}}>
          {TABS.map(t=>{const vis=visited.has(t.id);return <button key={t.id} onClick={()=>goTab(t.id)} style={{flex:1,padding:"10px 0",background:"transparent",border:"none",borderBottom:tab===t.id?`2px solid ${color}`:"2px solid transparent",color:tab===t.id?color:vis?MD3.onSurface:MD3.onSurfaceLow,fontFamily:"Roboto,sans-serif",fontSize:10,fontWeight:tab===t.id?700:500,cursor:"pointer",transition:"all 0.15s",position:"relative"}}>{t.label}{vis&&tab!==t.id&&<span style={{position:"absolute",top:6,right:"calc(50% - 10px)",width:3,height:3,borderRadius:"50%",background:color+"88"}}/>}</button>;})}
        </div>}

        {/* CONTENT */}
        <div style={{padding:wideLayout?"24px":"14px 16px",paddingBottom:wideLayout?24:130,flex:1,maxWidth:wideLayout?800:"100%"}}>

          {/* CAR */}
          {tab==="car"&&<div>
            <SectionTitle color={color} mt={0}>Select Your Car</SectionTitle>
            <Select label="Manufacturer" value={make} onChange={v=>{setMake(v);const m=CAR_DB[v][0];setModel(m);applyCarDefaults(m);}} options={MANUFACTURERS} hint="FH6 car list — updating on game release"/>
            <Select label="Model" value={model} onChange={v=>{setModel(v);applyCarDefaults(v);}} options={(CAR_DB[make]||[]).map(m=>({v:m,l:m}))}/>
            <div style={{background:MD3.surfaceC1,border:`1px solid ${color}33`,borderRadius:16,padding:"14px 16px",marginTop:4}}>
              <div style={{fontFamily:"Roboto,sans-serif",fontSize:10,color:MD3.onSurfaceLow,letterSpacing:"0.1em",textTransform:"uppercase",marginBottom:6}}>Selected</div>
              <div style={{fontFamily:"Roboto,sans-serif",fontSize:20,color,fontWeight:700}}>{make}</div>
              <div style={{fontFamily:"Roboto,sans-serif",fontSize:14,color:MD3.onSurface}}>{model}</div>
              {getCarKnowledge(model)&&<div style={{marginTop:10,padding:"6px 10px",background:MD3.success+"14",border:`1px solid ${MD3.success}33`,borderRadius:8,fontFamily:"Roboto,sans-serif",fontSize:11,color:MD3.success}}>
                ✓ Auto-filled: {driveType} · {ASPIRATIONS.find(a=>a.id===aspiration)?.label} · {rpm.toLocaleString()} RPM · {weight} {wUnit} · {hp}hp — check Engine & Specs tabs, override if upgraded
              </div>}
              {!getCarKnowledge(model)&&<div style={{marginTop:10,padding:"6px 10px",background:MD3.warning+"14",border:`1px solid ${MD3.warning}33`,borderRadius:8,fontFamily:"Roboto,sans-serif",fontSize:11,color:MD3.warning}}>
                ⚠ Car not in database — check Engine & Specs tabs and enter your car's values manually
              </div>}
            </div>
            <SectionTitle color={color} mt={20}>Class &amp; PI</SectionTitle>
            <div style={{display:"flex",gap:6,flexWrap:"wrap",marginBottom:12}}>{CLASSES.map(c=><Chip key={c} label={c} selected={carClass===c} onClick={()=>{setCarClass(c);setPi(Math.round((PI_RANGES[c][0]+PI_RANGES[c][1])/2));}} color={color}/>)}</div>
            <NumField label="Performance Index" value={pi} onChange={setPi} min={piRange[0]} max={piRange[1]} step={1}/>
          </div>}

          {/* ENGINE */}
          {tab==="engine"&&<div>
            <SectionTitle color={color} mt={0}>Aspiration</SectionTitle>
            <CardOption options={ASPIRATIONS} value={aspiration} onChange={setAspiration} color={color}/>
            <SectionTitle color={color}>Engine Character</SectionTitle>
            <CardOption options={ENGINE_CHARS} value={engineChar} onChange={setEngineChar} color={color}/>
            <SectionTitle color={color}>Engine Swap</SectionTitle>
            <Select label="Swap (or stock)" value={engineSwap} onChange={setEngineSwap} options={ENGINE_SWAPS} hint="Engine swaps change torque character, weight distribution, and diff tuning"/>
            <SectionTitle color={color}>Input Device</SectionTitle>
            <CardOption options={INPUT_DEVICES} value={inputDevice} onChange={setInputDevice} color={color}/>
          </div>}

          {/* SETUP */}
          {tab==="setup"&&<div>
            <SectionTitle color={color} mt={0}>Drive &amp; Surface</SectionTitle>
            <Label>Drive Type</Label>
            <div style={{display:"flex",gap:8,marginBottom:16}}>{DRIVE_TYPES.map(d=><Chip key={d} label={d} selected={driveType===d} onClick={()=>setDriveType(d)} color={color}/>)}</div>
            <Label>Surface</Label>
            <div style={{display:"flex",gap:8,flexWrap:"wrap",marginBottom:16}}>{SURFACES.map(s=><Chip key={s} label={s} selected={surface===s} onClick={()=>setSurface(s)} color={color}/>)}</div>
            <SectionTitle color={color}>Tires</SectionTitle>
            <Select label="Compound" value={compound} onChange={setCompound} options={TIRE_COMPOUNDS}/>
            <div style={{display:"grid",gridTemplateColumns:"1fr 1fr",gap:10}}>
              <Select label="Front Width" value={tireWF} onChange={setTireWF} options={TIRE_WF.map(w=>({v:w,l:w+"mm"}))}/>
              <Select label="Rear Width"  value={tireWR} onChange={setTireWR} options={TIRE_WR.map(w=>({v:w,l:w+"mm"}))}/>
              <div style={{gridColumn:"1/-1",background:MD3.surfaceC1,border:`1px solid ${MD3.outline}`,borderRadius:10,padding:"10px 14px"}}>
                <div style={{fontFamily:"Roboto,sans-serif",fontSize:11,color:MD3.onSurfaceVar,lineHeight:1.6}}>
                  💡 <strong style={{color:MD3.onSurface}}>Tire pressure is calculated for you</strong> — TuneLab outputs the optimal PSI on the Tune tab based on your compound, surface, and tune style. You don't need to enter it here.
                </div>
              </div>
            </div>
            <SectionTitle color={color}>Aero &amp; Drag</SectionTitle>
            <div style={{display:"flex",gap:8,marginBottom:12}}><Chip label="Has Aero" selected={hasAero} onClick={()=>setHasAero(true)} color={color}/><Chip label="No Aero" selected={!hasAero} onClick={()=>setHasAero(false)} color={MD3.onSurfaceLow}/></div>
            {hasAero&&<div style={{display:"grid",gridTemplateColumns:"1fr 1fr",gap:10}}><NumField label="Front Downforce" value={aeroF} onChange={setAeroF} unit="kg" min={0} max={500} step={5}/><NumField label="Rear Downforce" value={aeroR} onChange={setAeroR} unit="kg" min={0} max={500} step={5}/></div>}
            <NumField label="Drag Coefficient (Cd)" value={dragCd} onChange={setDragCd} min={0.15} max={0.9} step={0.01} hint="Road: 0.28–0.32 · Aero build: 0.38–0.50 · Drag: 0.55+"/>
          </div>}

          {/* SPECS */}
          {tab==="specs"&&<div>
            <SectionTitle color={color} mt={0}>Power &amp; Mass</SectionTitle>
            <div style={{display:"grid",gridTemplateColumns:"1fr 1fr",gap:10}}>
              <NumField label={`Weight (${wUnit})`} value={weight} onChange={setWeight} unit={wUnit} min={wUnit==="lbs"?1100:500} max={wUnit==="lbs"?6600:3000} step={10}/>
              <NumField label={`Power (${units.power==="ps"?"PS":"hp"})`} value={hp} onChange={setHp} unit={units.power==="ps"?"PS":"hp"} min={50} max={2000} step={5} hint={units.power==="ps"?"PS (Pferdestärke) — used in Japanese/EU tuning menus. 1PS ≈ 0.99hp":undefined}/>
              <NumField label={`Torque (${wUnit==="lbs"?"lb-ft":"Nm"})`} value={torque} onChange={setTorque} unit={wUnit==="lbs"?"lb-ft":"Nm"} min={50} max={1500} step={5}/>
              <NumField label={`Top Speed (${spUnit})`} value={topspeed} onChange={setTopspeed} unit={spUnit} min={50} max={320} step={5}/>
              <NumField label="Max RPM (Redline)" value={rpm} onChange={setRpm} unit="rpm" min={3000} max={18000} step={100} hint="Find in car's upgrade screen"/>
            </div>
            <SectionTitle color={color}>Gearbox</SectionTitle>
            <NumField label="Number of Gears" value={gears} onChange={setGears} min={4} max={10} step={1}/>
            <div style={{background:MD3.surfaceC1,border:`1px solid ${MD3.outline}`,borderRadius:16,padding:14,marginTop:6}}>
              <Label>Calculated Stats</Label>
              <div style={{display:"grid",gridTemplateColumns:"1fr 1fr 1fr",gap:8}}>
                {[{l:"hp/tonne",v:Math.round(hp/(toKg(weight,wUnit)/1000))},{l:"Nm/tonne",v:Math.round((wUnit==="lbs"?torque*1.356:torque)/(toKg(weight,wUnit)/1000))},{l:"Cd",v:dragCd}].map(st=><div key={st.l} style={{background:MD3.surfaceVar,borderRadius:10,padding:"10px 12px"}}>
                  <div style={{fontFamily:"Roboto Mono,monospace",fontSize:18,color,fontWeight:700}}>{st.v}</div>
                  <div style={{fontFamily:"Roboto,sans-serif",fontSize:10,color:MD3.onSurfaceLow,marginTop:2,textTransform:"uppercase"}}>{st.l}</div>
                </div>)}
              </div>
            </div>
          </div>}

          {/* TUNE */}
          {tab==="tune"&&<div>
            {/* AI Enhancement bar */}
            {aiEnhancing&&<div style={{background:provInfo?.color+"18",border:`1px solid ${provInfo?.color}44`,borderRadius:12,padding:"10px 14px",marginBottom:12,display:"flex",alignItems:"center",gap:10}}>
              <span style={{fontFamily:"Roboto,sans-serif",fontSize:13,color:provInfo?.color,animation:"pulse 1.5s infinite",fontWeight:600}}>{provInfo?.icon} Enhancing with {provInfo?.label}…</span>
              <div style={{marginLeft:"auto",display:"flex",gap:4}}>{[0,1,2].map(i=><div key={i} style={{width:6,height:6,borderRadius:"50%",background:provInfo?.color,animation:`pulse 1s ${i*0.2}s infinite`}}/>)}</div>
            </div>}
            {/* Tune page chips */}
            <div style={{display:"flex",gap:6,overflowX:"auto",paddingBottom:8,marginBottom:14,scrollbarWidth:"none"}}>
              {TUNE_PAGES.map(p=>{const ready=!!tunePages[p];return <button key={p} onClick={()=>setTunePage(p)} style={{flexShrink:0,padding:"6px 14px",borderRadius:20,border:`1.5px solid ${tunePage===p?color:ready?MD3.outline:MD3.outlineVar}`,background:tunePage===p?color+"1e":"transparent",color:tunePage===p?color:ready?MD3.onSurface:MD3.onSurfaceLow,fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:tunePage===p?700:500,cursor:"pointer",whiteSpace:"nowrap",opacity:ready||tunePage===p?1:0.4,transition:"all 0.12s"}}>{p}</button>;})}
            </div>
            {loading&&<div style={{textAlign:"center",paddingTop:60}}><div style={{fontFamily:"Roboto,sans-serif",fontSize:15,fontWeight:600,color,marginBottom:20}}>Calculating tune…</div><div style={{display:"flex",justifyContent:"center",gap:8}}>{[0,1,2,3,4].map(i=><div key={i} style={{width:8,height:8,borderRadius:"50%",background:color,animation:`pulse 1.2s ${i*0.15}s infinite`}}/>)}</div></div>}
            {!loading&&error&&<div style={{color:MD3.error,fontFamily:"Roboto,sans-serif",fontSize:13,padding:"20px 0",lineHeight:1.7}}>{error}</div>}
            {!loading&&!error&&!Object.keys(tunePages).length&&<div style={{textAlign:"center",paddingTop:80}}>
              <div style={{fontSize:48,marginBottom:16}}>🏎</div>
              <div style={{fontFamily:"Roboto,sans-serif",fontSize:16,fontWeight:600,color:MD3.onSurface}}>No tune generated yet</div>
              <div style={{fontFamily:"Roboto,sans-serif",fontSize:13,color:MD3.onSurfaceVar,marginTop:8}}>Visit all tabs, then tap Generate</div>
            </div>}
            {!loading&&!error&&tunePages[tunePage]&&<TuneCard data={tunePages[tunePage]} color={color} name={tunePage}/>}
            {!loading&&!error&&tunePage==="Tires"&&tunePages.Tires&&<PsiExplainer pages={tunePages} color={color} units={units}/>}
            {tuneGenerated&&!loading&&<div style={{marginTop:16,display:"grid",gridTemplateColumns:"1fr 1fr",gap:8}}>
              <button onClick={()=>setOverlay("save")} style={{padding:"13px",background:color+"18",border:`1.5px solid ${color}44`,borderRadius:12,color,fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:700,cursor:"pointer"}}>💾 Save</button>
              <button onClick={()=>{const t=formatTuneText(getState(),tunePages);navigator.share?.({title:"TuneLab Tune",text:t}).catch(()=>{})||navigator.clipboard?.writeText(t).then(()=>setToast("Copied!"));}} style={{padding:"13px",background:MD3.success+"18",border:`1.5px solid ${MD3.success}44`,borderRadius:12,color:MD3.success,fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:700,cursor:"pointer"}}>🔗 Share</button>
              <button onClick={handleShareCard} style={{padding:"13px",background:MD3.warning+"18",border:`1.5px solid ${MD3.warning}44`,borderRadius:12,color:MD3.warning,fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:700,cursor:"pointer"}}>📸 Card</button>
              <button onClick={()=>setOverlay("wizard")} style={{padding:"13px",background:MD3.error+"18",border:`1.5px solid ${MD3.error}44`,borderRadius:12,color:MD3.error,fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:700,cursor:"pointer"}}>🔧 Wizard</button>
              <button onClick={()=>setShowReset(true)} style={{gridColumn:"1/-1",padding:"10px",background:MD3.error+"0a",border:`1px solid ${MD3.error}33`,borderRadius:12,color:MD3.error+"cc",fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:600,cursor:"pointer"}}>↺ Reset / New Build</button>
            </div>}
            {tuneGenerated&&!loading&&<div style={{marginTop:10,display:"flex",gap:8}}>
              <button onClick={()=>setShowQA(true)} style={{flex:1,padding:"10px",background:MD3.surfaceC1,border:`1px solid ${MD3.outline}`,borderRadius:12,color:MD3.onSurface,fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:600,cursor:"pointer"}}>🎛 Adjust Values</button>
            </div>}
            {tuneGenerated&&!loading&&<TuneRating tuneId={tuneId} saveId={null}/>}
            {tuneGenerated&&!loading&&<div style={{marginTop:10,display:"flex",gap:8}}>
              <button onClick={()=>setShowQA(true)} style={{flex:1,padding:"10px",background:MD3.surfaceC1,border:`1px solid ${MD3.outline}`,borderRadius:12,color:MD3.onSurface,fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:600,cursor:"pointer"}}>🎛 Adjust Values</button>
            </div>}
            {tuneGenerated&&!loading&&<TuneRating tuneId={tuneId} saveId={null}/>}
            {tuneGenerated&&!hasAI&&!loading&&<div style={{marginTop:12,background:MD3.surfaceC1,border:`1px solid ${MD3.outline}`,borderRadius:12,padding:"12px 14px",display:"flex",gap:10,alignItems:"center"}}>
              <span style={{fontSize:20}}>✦</span>
              <div style={{flex:1}}><div style={{fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:700,color:MD3.onSurface,marginBottom:2}}>Add free AI analysis</div><div style={{fontFamily:"Roboto,sans-serif",fontSize:11,color:MD3.onSurfaceVar}}>Connect Gemini (free) for car-specific notes and smarter wizard diagnoses.</div></div>
              <button onClick={()=>setOverlay("ai")} style={{flexShrink:0,background:color+"22",border:`1px solid ${color}55`,borderRadius:20,padding:"8px 14px",color,fontFamily:"Roboto,sans-serif",fontSize:12,fontWeight:700,cursor:"pointer"}}>Set up</button>
            </div>}
          </div>}

        </div>

        {/* MOBILE GENERATE BUTTON */}
        {!wideLayout&&<div style={{position:"fixed",bottom:0,left:"50%",transform:"translateX(-50%)",width:"100%",maxWidth:480,padding:"10px 16px 18px",background:`linear-gradient(0deg,${MD3.surface} 60%,transparent)`,pointerEvents:"none"}}>
          {!allVisited&&<div style={{marginBottom:8,pointerEvents:"auto"}}>
            <div style={{display:"flex",justifyContent:"space-between",marginBottom:4}}>
              <span style={{fontFamily:"Roboto,sans-serif",fontSize:11,color:MD3.onSurfaceLow}}>Visit all tabs to unlock</span>
              <span style={{fontFamily:"Roboto Mono,monospace",fontSize:11,color}}>{Math.round(progress*100)}%</span>
            </div>
            <div style={{height:4,background:MD3.outlineVar,borderRadius:2,overflow:"hidden"}}><div style={{height:"100%",width:`${progress*100}%`,background:color,borderRadius:2,transition:"width 0.4s ease"}}/></div>
            <div style={{display:"flex",gap:4,marginTop:4}}>
              {MAIN_TABS.map((t,i)=>{
                const vis=visited.has(t);
                const isCurrent=tab===t;
                const nextUnvisited=MAIN_TABS.find(x=>!visited.has(x));
                return <button key={t} onClick={()=>goTab(t)} style={{flex:1,textAlign:"center",background:"transparent",border:"none",padding:"2px 0",cursor:"pointer"}}>
                  <div style={{height:3,borderRadius:2,background:isCurrent?color:vis?color+"66":MD3.outlineVar,transition:"background 0.3s",marginBottom:3}}/>
                  <span style={{fontFamily:"Roboto,sans-serif",fontSize:8,color:isCurrent?color:vis?color+"99":MD3.onSurfaceLow,fontWeight:isCurrent?700:400}}>{t}</span>
                </button>;
              })}
            </div>
          </div>}
          {!allVisited
            ? <button onClick={()=>{const next=MAIN_TABS.find(t=>!visited.has(t));if(next)goTab(next);}} style={{width:"100%",padding:"16px",pointerEvents:"auto",background:color+"22",border:`1.5px solid ${color}55`,borderRadius:28,color,fontFamily:"Roboto,sans-serif",fontSize:15,fontWeight:700,cursor:"pointer",transition:"all 0.3s",display:"flex",alignItems:"center",justifyContent:"center",gap:10}}>
                <span style={{fontFamily:"Roboto,sans-serif",fontSize:13,color:color+"99"}}>{Math.round(progress*100)}%</span>
                Next: {MAIN_TABS.find(t=>!visited.has(t))?.toUpperCase()} →
              </button>
            : <button onClick={handleGenerate} disabled={loading} style={{width:"100%",padding:"16px",pointerEvents:"auto",background:loading?MD3.surfaceC3:`linear-gradient(135deg,${color},${color}cc)`,border:`1.5px solid ${color}`,borderRadius:28,color:loading?MD3.onSurfaceVar:"#000",fontFamily:"Roboto,sans-serif",fontSize:15,fontWeight:700,cursor:loading?"not-allowed":"pointer",boxShadow:loading?"none":`0 4px 24px ${color}44`,transition:"all 0.3s"}}>
                {loading?"Calculating…":`⚡ Generate ${tuneMode?.label||""} Tune`}
              </button>
          }
        </div>}

        {/* WIDE: GENERATE BUTTON */}
        {wideLayout&&<div style={{padding:"16px 24px",borderTop:`1px solid ${MD3.outline}`,background:MD3.surfaceVar}}>
          {!allVisited&&<div style={{marginBottom:12}}>
            <div style={{display:"flex",justifyContent:"space-between",marginBottom:6}}>
              <span style={{fontFamily:"Roboto,sans-serif",fontSize:12,color:MD3.onSurfaceVar}}>Progress — visit all tabs to unlock</span>
              <span style={{fontFamily:"Roboto Mono,monospace",fontSize:12,color}}>{Math.round(progress*100)}%</span>
            </div>
            <div style={{height:4,background:MD3.outlineVar,borderRadius:2,overflow:"hidden"}}><div style={{height:"100%",width:`${progress*100}%`,background:color,transition:"width 0.4s ease"}}/></div>
          </div>}
          <div style={{display:"flex",gap:12,alignItems:"center"}}>
            <button onClick={()=>{if(!allVisited){const next=MAIN_TABS.find(t=>!visited.has(t));if(next)goTab(next);}else handleGenerate();}} disabled={loading} style={{flex:1,padding:"15px",background:!allVisited?color+"18":loading?MD3.surfaceC3:`linear-gradient(135deg,${color},${color}cc)`,border:`1.5px solid ${!allVisited?color+"55":color}`,borderRadius:28,color:!allVisited?color:loading?MD3.onSurfaceVar:"#000",fontFamily:"Roboto,sans-serif",fontSize:15,fontWeight:700,cursor:loading?"not-allowed":"pointer",boxShadow:loading?"none":`0 4px 24px ${color}44`,transition:"all 0.3s"}}>
              {loading?"Calculating…":!allVisited?`Next: ${(MAIN_TABS.find(t=>!visited.has(t))||"").toUpperCase()} →`:`⚡ Generate ${tuneMode?.label||""} Tune`}
            </button>
            {tuneGenerated&&<button onClick={handleShareCard} style={{padding:"15px 20px",background:MD3.warning+"18",border:`1.5px solid ${MD3.warning}44`,borderRadius:28,color:MD3.warning,fontFamily:"Roboto,sans-serif",fontSize:13,fontWeight:700,cursor:"pointer",whiteSpace:"nowrap"}}>📸 Share Card</button>}
          </div>
        </div>}
      </div>
    </div>
  );
}
