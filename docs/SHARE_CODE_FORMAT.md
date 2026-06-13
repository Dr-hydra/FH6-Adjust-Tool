# FH6 Adjust Tool — 分享码格式手册

> 对应文件：`docs/SHARE_CODE_FORMAT.md`（项目仓库内同路径有一份副本）
> 最后更新：2026-06-13  
> 当前版本：**v1**

---

## 概述

分享码是一种紧凑的文本字符串，让玩家无需传输文件即可分享调校方案。
典型长度 **300–500 字符**（含变速箱+空力的完整配置约 450 字符）。

**编码流程：**
```
SavedTune → Payload JSON → UTF-8 字节 → GZip(Optimal) → Base64 → 版本前缀
```

**示例：**
```
FH6v1:H4sIAAAAAAAACj2Py0oFMQyGX6VkpRBzcmmn7Sx1OO5EZgQXZ2Z1ENwJy...
```

---

## 格式版本前缀

| 前缀 | 版本 | 状态 |
|------|------|------|
| `FH6v1:` | v1 | ✅ 当前版本 |
| `FH6v2:` | v2 | 🔮 未来预留 |

解码器通过前缀路由到对应版本的解码器（见 `ShareCodec.TryDecode`）。

---

## Payload JSON 结构

```json
{
  "N": "调校方案名称",
  "C": "车辆显示文本",
  "K": "车辆搜索关键词",
  "S": [ ...State数组 24项... ],
  "R": [ ...Result平铺数组 变长... ]
}
```

---

## State 数组布局（v1，共 24 个元素）

State 字段所有枚举字符串替换为整数索引，布尔值替换为 0/1。

| 位置 | 字段 | 类型 |
|------|------|------|
| [0] | TuneId | 枚举索引 |
| [1] | DriveType | 枚举索引 |
| [2] | Surface | 枚举索引 |
| [3] | InputDevice | 枚举索引 |
| [4] | Weight | double |
| [5] | WeightDist | double |
| [6] | Gears | int |
| [7] | TireWF | string（自由文本） |
| [8] | TireWR | string（自由文本） |
| [9] | Compound | 枚举索引 |
| [10] | HasAero | 0/1 |
| [11] | AeroF | double |
| [12] | AeroR | double |
| [13] | DragCd | double |
| [14] | Pi | int |
| [15] | CarClass | 枚举索引 |
| [16] | WeightUnit | 枚举索引 |
| [17] | SpeedUnit | 枚举索引 |
| [18] | PressureUnit | 枚举索引 |
| [19] | SpringsUnit | 枚举索引 |
| [20] | FeelBalance | double |
| [21] | FeelAggression | double |
| [22] | IncludeGearing | 0/1 |
| [23] | DragDist | 枚举索引 |

### 编码表（v1）

> ⚠️ **已有条目顺序永不可改，新值只能追加到末尾**

```
TuneId:       0=General  1=Race    2=Touge   3=Wangan
              4=Drift    5=Drag    6=Rally   7=Rain

DriveType:    0=AWD   1=RWD   2=FWD

Surface:      0=Road  1=Dirt  2=Snow  3=Mixed

InputDevice:  0=controller  1=wheel  2=keyboard

Compound:     0=Street   1=Sport   2=Race Semi-Slick  3=Race Slick
              4=Rally    5=Drift   6=Snow              7=Drag

CarClass:     0=D  1=C  2=B  3=A  4=S1  5=S2  6=R  7=X

WeightUnit:   0=lbs    1=kg
SpeedUnit:    0=mph    1=kmh
PressureUnit: 0=psi    1=bar
SpringsUnit:  0=lbs/in  1=n/mm  2=kgf/mm
DragDist:     0=quarter  1=half  2=top
```

---

## Result 平铺数组布局（v1）

所有数值按**固定顺序**平铺为字符串数组，无字段名。
变长段长度由 State 中对应字段推断。

### 固定段（始终存在，共 23 个值）

```
[0]  Tires / Front Pressure
[1]  Tires / Rear Pressure
[2]  Alignment / Front Camber
[3]  Alignment / Rear Camber
[4]  Alignment / Front Toe
[5]  Alignment / Rear Toe
[6]  Alignment / Front Caster
[7]  Suspension / Front Spring
[8]  Suspension / Rear Spring
[9]  Suspension / Front Ride Height
[10] Suspension / Rear Ride Height
[11] ARB / Front ARB
[12] ARB / Rear ARB
[13] Damping / Front Rebound
[14] Damping / Rear Rebound
[15] Damping / Front Bump
[16] Damping / Rear Bump
[17] Braking / Brake Balance
[18] Braking / Brake Pressure
[19] Diff / Front Accel
[20] Diff / Front Decel
[21] Diff / Rear Accel
[22] Diff / Rear Decel
```

### 条件段（紧接固定段之后，按顺序）

```
若 State[1] == 0（AWD）:
  Diff / Center Balance                        (+1)

若 State[10] == 1（HasAero）:
  Aero / Front Downforce                       (+1)
  Aero / Rear Downforce                        (+1)

若 State[22] == 1（IncludeGearing）:
  Gearing / Final Drive                        (+1)
  Gearing / 1st Gear                           (+1)
  Gearing / 2nd Gear                           (+1)
  ...
  Gearing / {N}th Gear   N = State[6]（Gears） (+N 共计 Gears 项)
```

---

## 版本升级指南

### 情况 A：新增枚举值（**无需**升级版本）

在编码表数组末尾追加即可，旧码仍然有效：

```csharp
// ShareCodec.cs
private static readonly string[] Surfaces =
    ["Road", "Dirt", "Snow", "Mixed", "Gravel"];  // ← 追加
```

### 情况 B：必须升级版本号的情况

- 改变 State 数组任何字段的**位置**
- 改变 Result 固定段的**字段顺序**
- 删除或重命名现有枚举条目
- 在 State 数组中间插入新字段

**升级步骤：**

1. 在 `ShareCodec.cs` 新增前缀常量：
   ```csharp
   private const string PrefixV2 = "FH6v2:";
   ```

2. 新增 `DecodeV2(string b64)` 方法，按新布局解码

3. 修改 `Encode` 方法改用新布局，前缀改为 `PrefixV2`

4. 在 `TryDecode` 路由中添加：
   ```csharp
   if (code.StartsWith(PrefixV2))
   {
       tune = DecodeV2(code[PrefixV2.Length..]);
       return tune != null;
   }
   ```

5. 保留 `DecodeV1` 实现（历史版本码继续可读）

6. 更新本文档版本表

---

## 代码位置

| 文件 | 作用 |
|------|------|
| `src/QING.Core/Shared/ShareCodec.cs` | 编解码核心 |
| `src/FH6AdjustTool/Pages/PageSavedTunes.xaml` | 导入 UI |
| `src/FH6AdjustTool/Pages/PageSavedTunes.xaml.vb` | 导入/分享事件逻辑 |

---

## 手动测试步骤

1. 打开"保存的调校"页面，展开任意调校卡片
2. 点击"📋 分享"按钮，剪贴板提示约 300-500 字符
3. 在页面顶部输入框粘贴该分享码，点击"导入"
4. 确认新增了同名（或带"(导入)"后缀）的方案
5. 展开导入的方案，核对所有数值与原方案一致
