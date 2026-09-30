# 更新记录

本文件记录自动旋风斩插件的版本演进，格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循 `主版本.次版本.补丁版本`。

## [0.6.3] - 2026-10-01

### 修复

- 修复首次蓄力完成后接 `J` 不稳定、无法进入旋风斩的问题。
- 移除松开 `L` 后额外等待 `ChargeReleaseWaitSeconds` 或 `MotionComboReady` 的中间阶段，改为松手后同一输入帧进入 `AttackTap`。
- 修复自动注入只建立 `Pressing` 缓冲的问题：现在同时调用 `SetInput(PressDown, 0)` 与 `SetInput(Pressing, 0)`，使游戏约 5 帧的预输入窗口能够读到 `J`。

### 新增

- `J` 注入日志，记录注入阶段、当前动作名、动作时间、动作帧与输入冷却。

## [0.6.2] - 2026-09-30

### 新增

- `MotionComboReadyPatch`，监听 `HunterMotion.MotionActor.MotionComboReady` 并过滤本地角色，尝试在连招窗口就绪时进入 `J`。
- `UseComboReadyDetection` 配置，默认开启。

### 说明

- 实机日志确认蓄力释放动作没有向本地角色发出可用的 Ready 事件，该版本始终走超时兜底；0.6.3 起该补丁仅保留为诊断日志。

## [0.6.1] - 2026-09-30

### 修复

- 修复首次蓄力在第二格蓄力条已满后仍等待很久才释放的问题；改为优先读取与 `WeaponUI_THW` 同源的 `Hide_ChargeLevel1/2/3` 累计阈值。

### 新增

- 监测日志新增 UI 一/二/三格累计阈值，释放日志新增二格阈值与实际命中的阈值来源。

## [0.6.0] - 2026-09-30

### 变更

- 移除「必须先确认重新起蓄」的延迟判定，首次蓄力在维护按键的同一帧检查并释放。
- 循环中的 `L` 由长按改为与 `J/K` 相同的普通短按状态机，`ChargeTapHoldSeconds` 恢复为实际生效的保持时间。

### 新增

- `InputStateInjector` 集中维护 `SetInput` 后的 `_curFrameInputActionState`。
- `TargetChargeLevel` 配置，默认 `2`。

### 修复

- 修复首次长按 `L` 每帧重复发送零时长 `Pressing` 导致蓄力反复重置的问题。
- 修复 `MaximumAutoChargeSeconds` 被普通阶段 1.5 秒缩放下限截断的问题。

## [0.5.1] - 2026-09-29

### 修复

- 修复首次长按期间每帧重复发送 `Pressing` 的问题。
- 蓄力计数器暂不可用时持续按住到 `MaximumAutoChargeSeconds`（默认 4 秒），不再被普通阶段的时长上限提前截断。

## [0.5.0] - 2026-09-29

### 变更

- 改用游戏蓄力能量计数器自动判断强蓄力完成时机，默认目标能量百分比 `100`。
- 实际耗时写入 `MeasuredChargeHoldSeconds`，作为计数器不可用时的兜底值。

### 修复

- 修正长按阶段没有持续维持 `Pressing` 标志的问题；此前实机只充到约 `12/100`。

## [0.4.1] - 2026-09-29

### 新增

- `InitialChargeHoldSeconds`，首次蓄力基础值提高到 `1.5` 秒，避免沿用旧版 `0.65` 秒导致只出现短蓄力。

## [0.4.0] - 2026-09-29

### 变更

- 依据实机日志确认真实键盘事件与普通 `Update` 注入都晚于游戏输入判定，改为在 `CreatureInputCtrl.UpdateInput` 后置阶段注入。
- 移除 `SendInput`，循环调整为「首次长按 `L` 松开，之后 `J -> K -> L`」。

### 修复

- 清理 `J/K/L` 旧缓冲，修复走路和攻击时自动翻滚的问题。

## [0.3.0] - 2026-09-29

### 新增

- 增加 Windows 真实键盘事件，确保游戏 Rewired 层收到 `J/K/L` 按下与松开。

### 说明

- 该方案在 0.4.0 被证实时序过晚并移除。

## [0.2.0] - 2026-09-29

### 新增

- 由只读监测推进到可配置输入循环版本，使用 `Player.mGameInputCtrl.SetInput(...)` 注入 `J/K/L`。
- 默认循环为长按 `L`、松开 `L`、`J`、`K`、`L`。
- 新增蓄力时长、松开等待、`J` 保持、`J` 到 `K`、`K` 保持、`K` 回 `L` 等配置项。
- 新增基于 `AtkSpd / BasicAtkSpd` 的阶段时长缩放，为横斩或纵斩 30% 攻速词条预留自适应。

## [0.1.0] - 2026-09-28

### 新增

- 双手武器自动旋风斩监测原型，使用 `src/AutoWhirlwindMonitor` 独立维护源码与版本。
- 通过 `LC2.PlayerManager`、`LC2.Hero`、`LC2.CreatureInputCtrl` 与 `LC2.MotionActor_LC2` 读取本地角色、武器、攻速、输入和动作状态。
- 按 `O` 控制监测开关；该版本不注入攻击输入，仅收集蓄力斩、旋风斩与闪避取消窗口的实测数据。

[0.6.3]: https://github.com/Kagarinokirie1/LC2AutoWhirlwindMonitor/releases/tag/v0.6.3
[0.6.2]: https://github.com/Kagarinokirie1/LC2AutoWhirlwindMonitor/releases/tag/v0.6.2
[0.6.1]: https://github.com/Kagarinokirie1/LC2AutoWhirlwindMonitor/releases/tag/v0.6.1
[0.6.0]: https://github.com/Kagarinokirie1/LC2AutoWhirlwindMonitor/releases/tag/v0.6.0
[0.5.1]: https://github.com/Kagarinokirie1/LC2AutoWhirlwindMonitor/releases/tag/v0.5.1
[0.5.0]: https://github.com/Kagarinokirie1/LC2AutoWhirlwindMonitor/releases/tag/v0.5.0
[0.4.1]: https://github.com/Kagarinokirie1/LC2AutoWhirlwindMonitor/releases/tag/v0.4.1
[0.4.0]: https://github.com/Kagarinokirie1/LC2AutoWhirlwindMonitor/releases/tag/v0.4.0
[0.3.0]: https://github.com/Kagarinokirie1/LC2AutoWhirlwindMonitor/releases/tag/v0.3.0
[0.2.0]: https://github.com/Kagarinokirie1/LC2AutoWhirlwindMonitor/releases/tag/v0.2.0
[0.1.0]: https://github.com/Kagarinokirie1/LC2AutoWhirlwindMonitor/releases/tag/v0.1.0
