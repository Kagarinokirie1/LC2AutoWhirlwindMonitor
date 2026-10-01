using System;
using Hunter.Input;
using HunterMotion;

namespace LC2.AutoWhirlwindMonitor;

internal sealed partial class AutoWhirlwindController
{
    private enum Phase
    {
        Stopped,
        InitialChargeHold,
        AttackTap,
        AttackReleaseWait,
        DodgeTap,
        DodgeReleaseWait,
        ChargeTap,
        ChargeToAttackWait
    }

    private Phase _phase = Phase.Stopped;
    private float _phaseStartedAt;
    private int _phaseFrame;
    private int _lastPulseFrame = -1;
    private CreatureInputCtrl _input;
    private System.IntPtr _inputPointer;
    private System.IntPtr _motionPointer;
    private RuntimeSnapshot _lastSnapshot;
    private readonly InputStateInjector _inputState = new InputStateInjector();

    internal bool IsRunning => _phase != Phase.Stopped;

    internal void Start(RuntimeSnapshot snapshot, CreatureInputCtrl input, float now)
    {
        if (input == null)
        {
            Plugin.Logger.LogWarning("[自动旋风斩] 未找到可注入的游戏输入控制器，自动循环未启动。");
            return;
        }

        if (snapshot.WeaponType != WeaponType.Two_Handed_Melee)
        {
            Plugin.Logger.LogWarning($"[自动旋风斩] 当前武器为 {snapshot.WeaponType}，不是双手近战武器，自动循环未启动。");
            return;
        }

        _input = input;
        _inputPointer = input.Pointer;
        _motionPointer = GameStateReader.TryGetLocalMotionActor(out MotionActor_LC2 motion)
            ? motion.Pointer
            : System.IntPtr.Zero;
        _lastSnapshot = snapshot;
        _inputState.Reset();
        ClearTrackedBuffers();
        ResetActionFrameClock(snapshot, now);
        Enter(Phase.InitialChargeHold, now, "启动并开始首次 L 蓄力");
    }

    internal void Stop(string reason)
    {
        if (_phase == Phase.Stopped)
        {
            return;
        }

        DodgeCancelGuard.Disarm();
        ReleaseInjectedKeys();
        _inputState.Reset();
        _phase = Phase.Stopped;
        _input = null;
        _inputPointer = System.IntPtr.Zero;
        _motionPointer = System.IntPtr.Zero;
        _lastSnapshot = null;
        ClearActionFrameClock();
        Plugin.Logger.LogInfo($"[自动旋风斩] 已停止：{reason}。");
    }

    internal void UpdateFromGameInput(CreatureInputCtrl input, float now)
    {
        if (!IsRunning || input == null || _input == null || input.Pointer != _inputPointer)
        {
            return;
        }

        if (UnityEngine.Time.frameCount == _lastPulseFrame)
        {
            return;
        }

        _lastPulseFrame = UnityEngine.Time.frameCount;
        RuntimeSnapshot snapshot = GameStateReader.Read(out CreatureInputCtrl currentInput);
        if (currentInput == null || currentInput.Pointer != _inputPointer)
        {
            Stop("输入控制器丢失");
            return;
        }

        _lastSnapshot = snapshot;
        if (snapshot.WeaponType != WeaponType.Two_Handed_Melee)
        {
            Stop("武器切换或未装备双手近战武器");
            return;
        }

        if (snapshot.InputLocked)
        {
            Stop("游戏输入被锁定");
            return;
        }

        _phaseFrame++;
        AdvanceLogicalTime(snapshot, now);
        float elapsed = _logicalTime - _phaseStartedAt;

        switch (_phase)
        {
            case Phase.InitialChargeHold:
                UpdateInitialChargeHold(snapshot, elapsed, now);
                break;
            case Phase.AttackTap:
                UpdateAttackTap(snapshot, elapsed, now);
                break;
            case Phase.AttackReleaseWait:
                UpdateWait(Phase.DodgeTap, Scaled(Plugin.AttackToDodgeSeconds.Value, snapshot), elapsed, now, "进入 K");
                break;
            case Phase.DodgeTap:
                UpdateDodgeTap(snapshot, elapsed, now);
                break;
            case Phase.DodgeReleaseWait:
                UpdateWait(Phase.ChargeTap, Scaled(Plugin.DodgeToChargeSeconds.Value, snapshot), elapsed, now, "进入循环 L");
                break;
            case Phase.ChargeTap:
                UpdateChargeTap(snapshot, elapsed, now);
                break;
            case Phase.ChargeToAttackWait:
                UpdateWait(Phase.AttackTap, Scaled(Plugin.ChargeToAttackSeconds.Value, snapshot), elapsed, now, "循环回到 J");
                break;
        }
    }

    internal void NotifyMotionComboReady(MotionActor actor, BaseMotionData nextMotion, float now)
    {
        if (!Plugin.UseComboReadyDetection.Value || !IsRunning)
        {
            return;
        }

        if (actor == null || actor.Pointer == System.IntPtr.Zero || actor.Pointer != _motionPointer)
        {
            return;
        }

        string nextMotionName = string.IsNullOrEmpty(nextMotion?.motionName)
            ? "未知"
            : nextMotion.motionName;
        string snapshotDescription = _lastSnapshot == null
            ? "无"
            : $"{_lastSnapshot.MotionName} 时间={_lastSnapshot.MotionTime:0.###} 帧={_lastSnapshot.MotionFrame}";
        Plugin.Logger.LogInfo(
            $"[自动旋风斩] 连招 Ready 诊断：下一动作={nextMotionName}，当前阶段={_phase}，"
            + $"当前动作={snapshotDescription}。");
    }

    private void UpdateInitialChargeHold(RuntimeSnapshot snapshot, float elapsed, float now)
    {
        float pressTime = elapsed;
        if (_phaseFrame == 0)
        {
            BeginPress(KeyType.HardAttack);
            return;
        }

        MaintainPress(KeyType.HardAttack, pressTime);

        bool targetReached = HasReachedTargetCharge(snapshot);
        float timeoutSeconds = Plugin.UseChargeCounterDetection.Value
            && (Plugin.TargetChargeLevel.Value > 0
                || Plugin.TargetChargeEnergyPercent.Value > 0f)
            ? ResolveMaximumAutoChargeHold()
            : ResolveFallbackChargeHold(snapshot);

        if (!targetReached && elapsed < timeoutSeconds)
        {
            return;
        }

        if (targetReached)
        {
            RecordMeasuredChargeHold(elapsed);
            Plugin.Logger.LogInfo(
                $"[自动旋风斩] 首次蓄力达到目标："
                + $"等级={snapshot.ChargeLevel}/{snapshot.MaxChargeLevel}"
                + $" 目标等级={Plugin.TargetChargeLevel.Value}"
                + $" 能量={snapshot.ChargeEnergy:0.###}/{snapshot.MaxChargeEnergy:0.###}"
                + $" 二格阈值={snapshot.ChargeThresholdLevel2:0.###}"
                + $" 命中={ResolveChargeHitSource(snapshot)}"
                + $" 实测按住={elapsed:0.###} 秒，立即松开 L。");
        }
        else
        {
            Plugin.Logger.LogWarning(
                $"[自动旋风斩] 首次蓄力达到等待上限："
                + $"等级={snapshot.ChargeLevel}/{snapshot.MaxChargeLevel}"
                + $" 目标等级={Plugin.TargetChargeLevel.Value}"
                + $" 能量={snapshot.ChargeEnergy:0.###}/{snapshot.MaxChargeEnergy:0.###}"
                + $" 二格阈值={snapshot.ChargeThresholdLevel2:0.###}"
                + $"，已按住={elapsed:0.###} 秒，强制松开 L。");
        }

        ReleaseKey(KeyType.HardAttack, pressTime);
        Plugin.Logger.LogInfo(
            $"[自动旋风斩] 首次松开 L 后同帧预输入 J：释放前动作={snapshot.MotionName} "
            + $"时间={snapshot.MotionTime:0.###} 帧={snapshot.MotionFrame}。");
        Enter(Phase.AttackTap, now, "首次松开 L 后同帧进入 J");
        _phaseFrame = 0;
        UpdateAttackTap(snapshot, 0f, now);
    }

    private static bool HasReachedTargetCharge(RuntimeSnapshot snapshot)
    {
        if (!Plugin.UseChargeCounterDetection.Value)
        {
            return false;
        }

        int targetLevel = Math.Clamp(Plugin.TargetChargeLevel.Value, 1, 3);
        if (snapshot.ChargeCounterAvailable
            && TryGetUiChargeThreshold(snapshot, targetLevel, out float thresholdEnergy))
        {
            return snapshot.ChargeEnergy >= thresholdEnergy - 0.001f;
        }

        if (snapshot.ChargeLevelCounterAvailable)
        {
            return snapshot.ChargeLevel >= targetLevel;
        }

        float targetPercent = Math.Clamp(Plugin.TargetChargeEnergyPercent.Value, 0f, 100f);
        if (targetPercent <= 0f
            || !snapshot.ChargeCounterAvailable
            || snapshot.MaxChargeEnergy <= 0f)
        {
            return false;
        }

        float percentTargetEnergy = snapshot.MaxChargeEnergy * targetPercent / 100f;
        return snapshot.ChargeEnergy >= percentTargetEnergy - 0.001f;
    }

    private static string ResolveChargeHitSource(RuntimeSnapshot snapshot)
    {
        int targetLevel = Math.Clamp(Plugin.TargetChargeLevel.Value, 1, 3);
        if (snapshot.ChargeCounterAvailable
            && TryGetUiChargeThreshold(snapshot, targetLevel, out _))
        {
            return "UI蓄力格阈值";
        }

        return snapshot.ChargeLevelCounterAvailable ? "蓄力等级" : "蓄力能量";
    }

    private static bool TryGetUiChargeThreshold(
        RuntimeSnapshot snapshot,
        int targetLevel,
        out float threshold)
    {
        threshold = 0f;
        if (!snapshot.ChargeThresholdCounterAvailable)
        {
            return false;
        }

        switch (targetLevel)
        {
            case 1:
                threshold = snapshot.ChargeThresholdLevel1;
                break;
            case 2:
                threshold = snapshot.ChargeThresholdLevel2;
                break;
            case 3:
                threshold = snapshot.ChargeThresholdLevel3;
                break;
            default:
                return false;
        }

        return threshold > 0f;
    }

    private float ResolveFallbackChargeHold(RuntimeSnapshot snapshot)
    {
        float measured = Plugin.MeasuredChargeHoldSeconds.Value;
        return measured > 0.05f
            ? Clamp(measured)
            : Scaled(Plugin.InitialChargeHoldSeconds.Value, snapshot);
    }

    private float ResolveMaximumAutoChargeHold()
    {
        // 蓄力上限不是普通动作阶段时长，不能受 MaximumScaledSeconds 的 1.5 秒限制。
        return Math.Max(Plugin.MinimumScaledSeconds.Value, Plugin.MaximumAutoChargeSeconds.Value);
    }

    private void RecordMeasuredChargeHold(float elapsed)
    {
        if (!Plugin.AutoCalibrateChargeHold.Value || elapsed <= 0.05f)
        {
            return;
        }

        float previous = Plugin.MeasuredChargeHoldSeconds.Value;
        if (previous > 0f && Math.Abs(previous - elapsed) < 0.05f)
        {
            return;
        }

        Plugin.MeasuredChargeHoldSeconds.Value = elapsed;
        Plugin.Instance?.Config.Save();
        Plugin.Logger.LogInfo($"[自动旋风斩] 已自动保存实测蓄力时间：{elapsed:0.###} 秒。");
    }

    private void UpdateAttackTap(RuntimeSnapshot snapshot, float elapsed, float now)
    {
        float pressTime = elapsed;
        if (_phaseFrame == 0)
        {
            Plugin.Logger.LogInfo(
                $"[自动旋风斩] 注入 J：阶段={_phase} 动作={snapshot.MotionName} "
                + $"时间={snapshot.MotionTime:0.###} 帧={snapshot.MotionFrame} "
                + $"输入冷却={snapshot.GameInputCoolDown}。");
            BeginPress(KeyType.Attack);
        }
        else
        {
            MaintainPress(KeyType.Attack, pressTime);
        }

        if (elapsed >= Scaled(Plugin.AttackTapHoldSeconds.Value, snapshot))
        {
            ReleaseKey(KeyType.Attack, pressTime);
            Enter(Phase.AttackReleaseWait, now, "松开 J");
        }
    }

    private void UpdateDodgeTap(RuntimeSnapshot snapshot, float elapsed, float now)
    {
        if (_phaseFrame == 0 && ShouldBlockDodgeTap(snapshot))
        {
            Enter(Phase.DodgeReleaseWait, now, "当前动作已属于闪避或冲刺攻击，跳过危险 K");
            return;
        }

        float pressTime = elapsed;
        if (_phaseFrame == 0)
        {
            DodgeCancelGuard.Arm(_input, _motionPointer, snapshot);
            bool injected = _inputState.TryBeginSingleFramePress(_input, KeyType.Dodge);
            Plugin.Logger.LogInfo(
                $"[自动旋风斩] 注入单帧 K：结果={injected} 源动作={snapshot.MotionName} "
                + $"时间={snapshot.MotionTime:0.###} 帧={snapshot.MotionFrame} "
                + "仅写 Pressing，不建立 PressDown 跨帧缓冲。");
        }
        else
        {
            MaintainPress(KeyType.Dodge, pressTime);
        }

        if (elapsed >= Scaled(Plugin.DodgeTapHoldSeconds.Value, snapshot))
        {
            ReleaseKey(KeyType.Dodge, pressTime);
            Enter(Phase.DodgeReleaseWait, now, "松开 K");
        }
    }

    private void UpdateChargeTap(RuntimeSnapshot snapshot, float elapsed, float now)
    {
        float pressTime = elapsed;
        if (_phaseFrame == 0)
        {
            BeginPress(KeyType.HardAttack);
        }
        else
        {
            MaintainPress(KeyType.HardAttack, pressTime);
        }

        if (elapsed >= Scaled(Plugin.ChargeTapHoldSeconds.Value, snapshot))
        {
            ReleaseKey(KeyType.HardAttack, pressTime);
            Enter(Phase.ChargeToAttackWait, now, "循环松开 L");
        }
    }

    private void BeginPress(KeyType keyType)
    {
        _inputState.TryBeginPress(_input, keyType);
    }

    private void MaintainPress(KeyType keyType, float pressTime)
    {
        _inputState.TryMaintainPress(_input, keyType, pressTime);
    }

    private void ReleaseKey(KeyType keyType, float pressTime)
    {
        _inputState.TryRelease(_input, keyType, pressTime);
    }

    private void UpdateWait(Phase nextPhase, float waitSeconds, float elapsed, float now, string reason)
    {
        if (elapsed >= waitSeconds)
        {
            Enter(nextPhase, now, reason);
        }
    }

    private void Enter(Phase phase, float now, string reason)
    {
        _phase = phase;
        _phaseStartedAt = _logicalTime;
        _phaseFrame = -1;
        if (phase == Phase.DodgeTap)
        {
            CaptureDodgeTapSource(_lastSnapshot);
        }

        Plugin.Logger.LogInfo($"[自动旋风斩] 阶段 -> {phase}：{reason}。");
    }

    private float Scaled(float seconds, RuntimeSnapshot snapshot)
    {
        if (!Plugin.AdaptToAttackSpeed.Value)
        {
            return Clamp(seconds);
        }

        float attackSpeed = snapshot.AttackSpeed;
        float basicAttackSpeed = snapshot.BasicAttackSpeed;
        if (attackSpeed <= 0.001f || basicAttackSpeed <= 0.001f)
        {
            return Clamp(seconds);
        }

        float ratio = basicAttackSpeed / attackSpeed;
        return Clamp(seconds * ratio);
    }

    private float Clamp(float seconds)
    {
        float minimum = Plugin.MinimumScaledSeconds.Value;
        float maximum = Plugin.MaximumScaledSeconds.Value;
        if (seconds < minimum)
        {
            return minimum;
        }

        if (seconds > maximum)
        {
            return maximum;
        }

        return seconds;
    }

    private void ReleaseInjectedKeys()
    {
        if (_input == null)
        {
            return;
        }

        try
        {
            ReleaseKey(KeyType.HardAttack, 0f);
            ReleaseKey(KeyType.Attack, 0f);
            ReleaseKey(KeyType.Dodge, 0f);
            ClearTrackedBuffers();
        }
        catch
        {
        }
    }

    private void ClearTrackedBuffers()
    {
        if (_input == null)
        {
            return;
        }

        _input.ClearKeyBuffer(KeyType.HardAttack, true);
        _input.ClearKeyBuffer(KeyType.Attack, true);
        _input.ClearKeyBuffer(KeyType.Dodge, true);
    }
}
