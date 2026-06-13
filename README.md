# FH6 调校工具 (FH6 Adjust Tool)

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![WPF](https://img.shields.io/badge/platform-WPF%20%7C%20.NET%2010.0-orange.svg)

`FH6 调校工具` 是一款专为《极限竞速：地平线 6 (Forza Horizon 6)》打造的第三方开源车辆物理调校与 AI 智能调优助手。

它融合了地平线社区成熟的物理调校算法，并搭载了先进的 AI 大语言模型（支持多供应商 API 接入），为竞速游戏爱好者提供一站式的车辆属性计算、方案存档及人机交互式的连续微调指导。

---

## 🌟 核心功能

1. **精准物理计算**
   - 自动匹配车辆规格：仅需选择车型、输入车重及前部配重比，即可自动载入或计算核心调校属性。
   - 针对不同驾驶风格（**抓地/漂移/直线加速/拉力离地**）自动推荐最优的胎压、避震弹簧刚度、阻尼常数、防倾杆刚度以及齿轮传动比。
   - 支持调校结果的**手动编辑与微调**，完美兼顾理论计算与个性化驾驶习惯。

2. **AI 智能诊断与连续对话**
   - **多供应商 API 兼容**：原生支持官方 **Google Gemini** 接口，并全面兼容 **OpenAI 格式标准**（可无缝接入 DeepSeek、Claude、Ollama 本地模型或国内各类 API 代理）。
   - **一键增强调校**：AI 将直接读取当前车辆的属性及计算出的物理调校结果，进行多维度的赛道表现诊断并输出优化报告。
   - **连续聊天问答**：调优完成后，左侧栏会自动展开连续对话聊天室。用户可就车辆表现（如“高速弯推头”、“起步打滑”）向 AI 调校助手追问，AI 将结合历史记录与当前车辆配置给予精细的修改方向。
   - **模型自定义**：设置页面支持自主选择或输入自定义模型名称、接口地址（Base URL）及 Key。

3. **本地方案存档与分享**
   - 调校方案支持重命名、另存为及彻底覆盖/删除。
   - 内置**调校分享码**功能。支持将调校一键生成紧凑的分享码发给好友，或通过输入分享码快速导入他人的调校配置。

---

## 🛠️ 技术架构

- **UI 层 (WPF)**：基于 Windows Presentation Foundation 构建，自绘标题栏与高仿真自适应布局。
- **Core 核心库 (C# / VB.NET)**：
  - `TuningCalculator`：车辆物理学公式引擎，推导并输出基础悬挂和传动值。
  - `ShareCodec`：紧凑型 Base64 车辆规格与调校状态编解码器。
  - `AiClientFactory`：多 API 适配层，实现 `IAiClient` 以封装多供应商请求。

---

## 🚀 编译与运行

### 环境依赖
- **操作系统**：Windows 7 SP1 或更高版本
- **开发工具链**：.NET 10.0 SDK

### 本地编译步骤
1. 克隆本项目仓库：
   ```bash
   git clone https://github.com/Dr-hydra/FH6-Adjust-Tool.git
   cd FH6-Adjust-Tool
   ```
2. 使用 .NET CLI 编译 Release 版本：
   ```powershell
   dotnet build .\FH6AdjustTool.sln -c Release
   ```
3. 编译完成后，即可在下方路径找到可执行文件并直接双击运行：
   `src\FH6AdjustTool\bin\Release\net10.0-windows\FH6AdjustTool.exe`

---

## 📝 免责声明

1. 本软件为**第三方个人开源项目**，与微软公司 (Microsoft)、Xbox、Turn 10 或 Playground Games 官方无任何隶属或关联关系。《极限竞速：地平线》(Forza Horizon) 为微软公司注册商标。
2. 程序内嵌的物理调校算法部分推导自社区公开资料和玩家公开的公式常数，计算结果仅供竞速游戏爱好者交流 and 学习调优使用，不保证绝对的赛道圈速提升。

---

## 📄 开源许可证

- 本项目桌面客户端软件遵循 **MIT** 开源许可协议。
- 界面库组件部分遵循 **GPLv3** 开源许可协议。
