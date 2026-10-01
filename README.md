# LC2 自动旋风斩

失落城堡 2（Lost Castle 2）双手近战武器的自动旋风斩循环插件，基于 BepInEx 6 IL2CPP 与 Harmony 实现，通过注入游戏内部输入状态完成「蓄力斩 -> 旋风斩 -> 闪避取消」的无限循环。

当前版本：**0.7.1**

## 功能

- 按 `O` 一键开关自动循环，开启时同步输出监测日志。
- 首次自动进入长按 `L` 蓄力，读取游戏三格蓄力条同源的隐藏阈值计数器，第二格刚满的同一帧立即松手。
- 松手后在**同一输入帧**注入 `J`，并同时建立 `PressDown` 与 `Pressing` 输入缓冲，复现手动「提前或稍晚报 J」时游戏能识别的预输入。
- 之后按 `J -> K -> L` 三个普通短按循环，不再每轮重新长按蓄力。
- `J`、`L` 保留当前帧与约 5 帧预输入缓冲；自动 `K` 只写当前帧 `Pressing`，不建立跨动作 `PressDown` 缓冲，避免同一次闪避输入在 `THW_Dodge` 内被二次消费。
- 阶段时钟按动作帧推进量运行。连招穿怪出现命中停顿时同步降速，避免 J/K/L 相对动作帧漂移。
- 在四参数 `MotionActor.SetTargetMotion_ForceSet` 出口增加最终保护，拒绝自动循环守卫范围内的 `Dodge/DodgeAttack -> DodgeAttack` 强制接招，避免错误续接冲刺攻击。
- 输入注入发生在 `LC2.CreatureInputCtrl.UpdateInput` 执行完成后，确保角色动作判定能读取到当帧状态。
- 依据 `BasicAtkSpd / AtkSpd` 缩放各阶段时长，自动适配横斩或纵斩的 30% 攻速词条。
- 启动、停止、异常退出时清理 `J/K/L` 输入缓冲，避免残留缓冲导致手动移动或攻击时自动翻滚。
- 完整的监测日志：武器类型、攻速、蓄力能量与阈值、动作名/状态/帧/播放速率、按键各状态与按压时间。

## 运行要求

| 项目 | 要求 |
| --- | --- |
| 游戏 | 失落城堡 2（Unity `6000.3.23f1`） |
| 框架 | BepInEx 6 IL2CPP（`6.0.0-be.764`）+ UnityDoorstop `4.5.0` |
| 运行库 | .NET 6（随框架包提供） |
| 武器 | 双手近战武器（`Two_Handed_Melee`） |

> 自动循环仅在本地角色装备双手近战武器、且输入未被游戏锁定时才会真正启动，其余情况只会记录监测日志。

## 安装

### 方式一：使用带框架的整包（推荐）

从 [Releases](https://github.com/Kagarinokirie1/LC2AutoWhirlwindMonitor/releases) 下载 `LC2AutoWhirlwindMonitor-v0.7.1-BepInEx.zip`，把压缩包内的所有内容解压到游戏根目录（即 `LostCastle2.exe` 所在目录），覆盖同名文件即可。

解压后目录应形如：

```text
Lost Castle 2/
├── LostCastle2.exe
├── winhttp.dll
├── doorstop_config.ini
├── .doorstop_version
├── dotnet/
└── BepInEx/
    ├── core/
    └── plugins/
        └── LC2AutoWhirlwindMonitor.dll
```

包内已自带 BepInEx 框架、UnityDoorstop、CoreCLR 运行时和 Unity 基础库，无需再单独安装。

### 方式二：只安装插件

已有可用的 BepInEx 6 IL2CPP 环境时，把 `LC2AutoWhirlwindMonitor.dll` 放进 `BepInEx/plugins/` 即可。

## 使用

1. 启动游戏，进入关卡并装备双手近战武器。
2. 按 `O` 开启自动循环。
3. 插件会先长按 `L` 蓄力，第二格蓄力条满时自动松手并接 `J` 进入旋风斩，随后持续 `J -> K -> L` 循环。
4. 再按一次 `O` 停止自动循环，并清理残留输入缓冲。

首次进入关卡后请在无法被击中的位置试跑几轮，确认节奏符合当前装备攻速。

## 配置

配置文件位于 `BepInEx/config/lc2.autowhirlwind.monitor.cfg`，游戏内修改后重启生效。

### 总开关与热键

| 键 | 默认值 | 说明 |
| --- | --- | --- |
| `1.总开关/Enabled` | `true` | 关闭后插件不初始化，`O` 键不响应。 |
| `2.热键/ToggleKey` | `O` | 切换自动循环与监测日志的按键。 |

### 采样

| 键 | 默认值 | 说明 |
| --- | --- | --- |
| `3.采样/SnapshotIntervalSeconds` | `0.5` | 状态无变化时的周期日志间隔（秒）。 |
| `3.采样/LogInputChanges` | `true` | 输入或动作状态变化时立即输出日志。 |

### 自动循环

| 键 | 默认值 | 说明 |
| --- | --- | --- |
| `4.自动循环/EnableAutomation` | `true` | 关闭后 `O` 只切换监测日志，不注入输入。 |
| `4.自动循环/AdaptToAttackSpeed` | `true` | 按攻速比例缩放各阶段时长。 |
| `4.自动循环/UseChargeCounterDetection` | `true` | 读取游戏蓄力计数器判断释放时机。 |
| `4.自动循环/UseComboReadyDetection` | `true` | 仅记录连招 Ready 诊断事件，不参与触发。 |
| `4.自动循环/TargetChargeLevel` | `2` | 目标蓄力格数（1-3），默认第二格满即释放。 |
| `4.自动循环/TargetChargeEnergyPercent` | `100` | 两类计数器都不可用时的兜底能量百分比。 |
| `4.自动循环/AutoCalibrateChargeHold` | `true` | 记录本次蓄力耗时到 `MeasuredChargeHoldSeconds`。 |

### 阶段时长

| 键 | 默认值 | 说明 |
| --- | --- | --- |
| `5.阶段时长/InitialChargeHoldSeconds` | `1.5` | 无法读取计数器时的首次蓄力兜底时间（秒）。 |
| `5.阶段时长/MeasuredChargeHoldSeconds` | `0` | 自动测量写入的蓄力时间，大于 `0` 时优先使用。 |
| `5.阶段时长/MaximumAutoChargeSeconds` | `4` | 首次长按 `L` 的最长等待时间（秒）。 |
| `5.阶段时长/ChargeReleaseWaitSeconds` | `0.05` | 兼容旧配置保留，0.6.3 起不再参与循环。 |
| `5.阶段时长/AttackTapHoldSeconds` | `0.04` | `J` 旋风斩按下保持时间（秒）。 |
| `5.阶段时长/AttackToDodgeSeconds` | `0.03` | `J` 到 `K` 闪避取消的等待时间（秒）。 |
| `5.阶段时长/DodgeTapHoldSeconds` | `0.05` | `K` 闪避按下保持时间（秒）。 |
| `5.阶段时长/DodgeToChargeSeconds` | `0.03` | `K` 回到下一次 `L` 的等待时间（秒）。 |
| `5.阶段时长/ChargeTapHoldSeconds` | `0.04` | 循环中 `L` 短按的保持时间（秒）。 |
| `5.阶段时长/ChargeToAttackSeconds` | `0.03` | 循环中 `L` 回到下一次 `J` 的等待时间（秒）。 |
| `5.阶段时长/MinimumScaledSeconds` | `0.016` | 攻速缩放后的最短阶段时间（秒）。 |
| `5.阶段时长/MaximumScaledSeconds` | `1.5` | 攻速缩放后的最长阶段时间（秒）。 |

## 日志

日志写入 `BepInEx/LogOutput.log`，同时输出到 BepInEx 控制台。排查问题时建议保留以下内容：

- `首次松开 L 后同帧预输入 J` 之后的动作名与动作时间。
- 每轮 `J -> K -> L` 的动作名、输入帧与输入冷却。
- `注入单帧 K`、`DodgeCancel 事件补丁命中` 和 `已拦截 Dodge -> DodgeAttack 目标切换` 等输入保护日志。
- 蓄力能量、UI 一/二/三格累计阈值与本次释放命中的阈值来源。

## 从源码构建

构建依赖游戏目录下由 BepInEx 生成的 `BepInEx/interop` 程序集，因此需要先至少启动过一次带 BepInEx 的游戏。

```powershell
dotnet build .\src\AutoWhirlwindMonitor\LC2AutoWhirlwindMonitor.csproj -c Release
Copy-Item -LiteralPath .\src\AutoWhirlwindMonitor\bin\Release\LC2AutoWhirlwindMonitor.dll -Destination .\BepInEx\plugins\LC2AutoWhirlwindMonitor.dll -Force
```

或直接使用脚本：

```powershell
.\scripts\build.ps1
```

仓库默认假设 `src/` 与游戏的 `BepInEx/` 同级；如果仓库放在别处，用 `-p:GameDir` 指定游戏根目录：

```powershell
dotnet build .\src\AutoWhirlwindMonitor\LC2AutoWhirlwindMonitor.csproj -c Release -p:GameDir="D:\Steam\steamapps\common\Lost Castle 2"
```

## 兼容性

- 针对 Unity `6000.3.23f1` 的游戏版本开发，游戏更新后 Hook 点与蓄力计数器名称可能变化。
- 与仅读写自身配置的其它 BepInEx 插件兼容；它会操作本地角色的 `J/K/L` 输入状态，与同样注入输入或改写连招判定的插件可能冲突。
- 联机时是否使用请自行评估，本插件只改动本地输入状态。

## 免责声明

本项目为个人学习与本地娱乐用途的非官方插件，与 Hunter Studio 及《失落城堡 2》官方无关。使用第三方插件可能存在封号或存档异常风险，请自行评估并在使用前备份存档。

## 许可证

插件源码与发布包中的自有部分采用 [MIT](LICENSE) 许可证。发布包内附带的 BepInEx、UnityDoorstop 等第三方组件遵循各自许可证，详见 [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt)。

## 相关文档

- [技术实现说明](docs/技术实现说明.md)：Hook 点、输入时序依据、蓄力阈值来源与各版本行为差异。
- [更新记录](CHANGELOG.md)：各版本新增与修复内容。
