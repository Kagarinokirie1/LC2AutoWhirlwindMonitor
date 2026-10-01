# 更新记录

本文件记录自动旋风斩插件的版本演进，格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，版本号遵循 `主版本.次版本.补丁版本`。

## [0.7.1] - 2026-10-01

### 修复

- 修复连招穿过怪物后同一个自动 `K` 被消费两次、续接 `THW_DodgeAttack_A/B` 的问题。
- 自动 `K` 改为只写当前帧 `Pressing`，不再建立约 5 帧的跨动作 `PressDown` 缓冲；源攻击中的首次闪避仍能正常生效，进入 `THW_Dodge` 后不会再次消费同一次按键。
- 在四参数 `MotionActor.SetTargetMotion_ForceSet` 出口拦截自动循环守卫范围内的 `Dodge/DodgeAttack -> DodgeAttack` 强制切换，和 `MotionEvent_DodgeCancel` 事件短路共同构成最终保护。
- 保留动作帧时钟修复，继续避免命中停顿时 J/K/L 相对动作帧漂移。

### 验证

- 本地构建通过，`0` 警告、`0` 错误。
- 已由用户实机测试确认穿怪后不再触发无限闪避冲刺攻击，自动连招恢复正常。

## [0.7.0] - 2026-10-01

### 变更

- 增加三参数 `MotionActor.SetTargetMotion` 出口拦截，尝试阻止已经锁存的 `THW_Dodge -> THW_DodgeAttack` 动作切换。
- 增加独立的 `DodgeCancel` 事件命中日志，便于区分事件短路和动作切换出口的实际执行情况。

### 说明

- 实机日志确认三参数出口拦截计数为 `0`；连招事件实际调用四参数 `SetTargetMotion_ForceSet`，该方案由 `0.7.1` 修正。

## [0.6.9] - 2026-10-01

### 修复

- 为 `MotionEvent_DodgeCancel.EventUpdate_Logic` 增加 Harmony 前缀，在当前动作已属于 `Dodge/DodgeAttack` 时直接跳过原事件更新。
- 短路事件时清理 `Dodge` 的输入缓冲和当前输入状态，避免残留 `K` 在后续动作中继续污染取消逻辑。
- 保留 `MotionActor.LogicUpdate` 守卫作为事件执行顺序变化时的兜底。

## [0.6.6] - 2026-10-01

### 修复

- 增加 `DodgeInputGuard`，自动 `K` 注入前检查当前动作类型。
- 当前动作已经包含 `Dodge` 或 `DodgeAttack` 时跳过危险 `K`，并记录源动作、当前动作类型和动作帧。

## [0.6.5] - 2026-10-01

### 修复

- 修复连招穿过怪物后因命中停顿导致阶段时钟漂移、`K` 在 `THW_Dodge` 错误帧被消费的问题。
- 新增 `ActionFrameClock`，按 `MotionActor_LC2.CurFrame` 的相邻推进量换算逻辑时间；动作帧停滞时阶段节拍同步降速。
- 移除 `0.6.4` 检测异常动作后等待结束并重新开始整套循环的恢复方案，恢复过程继续按正常连招节奏运行。

## [0.6.4] - 2026-10-01

### 变更

- 增加 `THW_DodgeAttack_A/B` 异常恢复原型：检测到冲刺攻击后停止注入，等待动作结束并从首次 `L` 蓄力重新开始。

### 说明

- 用户实机反馈该方案不是模拟手动连招，而是在失败后强制重开循环，因此 `0.6.5` 已移除。

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
[0.6.4]: https://github.com/Kagarinokirie1/LC2AutoWhirlwindMonitor/releases/tag/v0.6.4
[0.6.5]: https://github.com/Kagarinokirie1/LC2AutoWhirlwindMonitor/releases/tag/v0.6.5
[0.6.6]: https://github.com/Kagarinokirie1/LC2AutoWhirlwindMonitor/releases/tag/v0.6.6
[0.6.9]: https://github.com/Kagarinokirie1/LC2AutoWhirlwindMonitor/releases/tag/v0.6.9
[0.7.0]: https://github.com/Kagarinokirie1/LC2AutoWhirlwindMonitor/releases/tag/v0.7.0
[0.7.1]: https://github.com/Kagarinokirie1/LC2AutoWhirlwindMonitor/releases/tag/v0.7.1
