using System;
using Hunter.Input;
using HunterMotion;
using LC2;
using UnityEngine;

namespace LC2.AutoWhirlwindMonitor;

// 自动 K 从源攻击切进 THW_Dodge 后，PressDown 缓冲可能还会跨动作存活。
// MotionEvent_DodgeCancel 在 THW_Dodge 中再次读到这条缓冲时会锁存冲刺取消，
// 最终强制切到 THW_DodgeAttack。这个守卫允许源攻击动作中的首次 K 执行原事件，
// 但从动作已经进入 Dodge/DodgeAttack 开始，直接短路后续 DodgeCancel 事件更新。
internal static class DodgeCancelGuard
{
    private const float MaximumGuardSeconds = 2f;

    private static CreatureInputCtrl _input;
    private static IntPtr _inputPointer;
    private static IntPtr _motionPointer;
    private static float _armedAt;
    private static string _sourceMotionName = string.Empty;
    private static int _sourceMotionFrame;
    private static bool _enteredDodgeOrDodgeAttack;
    private static bool _suppressLogged;
    private static bool _eventPatchHitLogged;
    private static bool _targetBlockLogged;

    internal static void Arm(
        CreatureInputCtrl input,
        IntPtr motionPointer,
        RuntimeSnapshot sourceSnapshot)
    {
        Disarm();

        if (input == null || input.Pointer == IntPtr.Zero || motionPointer == IntPtr.Zero)
        {
            return;
        }

        _input = input;
        _inputPointer = input.Pointer;
        _motionPointer = motionPointer;
        _armedAt = Time.unscaledTime;
        _sourceMotionName = string.IsNullOrEmpty(sourceSnapshot?.MotionName)
            ? "未知"
            : sourceSnapshot.MotionName;
        _sourceMotionFrame = sourceSnapshot?.MotionFrame ?? 0;
    }

    internal static void Disarm()
    {
        _input = null;
        _inputPointer = IntPtr.Zero;
        _motionPointer = IntPtr.Zero;
        _armedAt = 0f;
        _sourceMotionName = string.Empty;
        _sourceMotionFrame = 0;
        _enteredDodgeOrDodgeAttack = false;
        _suppressLogged = false;
        _eventPatchHitLogged = false;
        _targetBlockLogged = false;
    }

    internal static void LogEventPatchHit(MotionActor_LC2 actor)
    {
        if (_eventPatchHitLogged
            || !CanUseGuard(actor, out MotionType_LC2 currentMotionType,
                out string currentMotionName, out int currentMotionFrame))
        {
            return;
        }

        _eventPatchHitLogged = true;
        Plugin.Logger.LogInfo(
            $"[自动旋风斩] DodgeCancel 事件补丁命中："
            + $"源动作={_sourceMotionName} 帧={_sourceMotionFrame}，"
            + $"当前动作={currentMotionName} 类型={currentMotionType} 帧={currentMotionFrame}。");
    }

    internal static bool TrySuppressDodgeCancelEvent(MotionActor_LC2 actor, string source)
    {
        if (!CanUseGuard(actor, out MotionType_LC2 currentMotionType,
                out string currentMotionName, out int currentMotionFrame))
        {
            return false;
        }

        if (!ContainsMotionType(currentMotionType, MotionType_LC2.Dodge)
            && !ContainsMotionType(currentMotionType, MotionType_LC2.DodgeAttack))
        {
            if (_enteredDodgeOrDodgeAttack)
            {
                Disarm();
            }

            return false;
        }

        bool pressed;
        bool buffered;
        try
        {
            pressed = _input.GetGameKeyState(KeyType.Dodge, KeyState.PressDown);
            buffered = _input.GetGameKeyStateIncludeBuffer(KeyType.Dodge, KeyState.PressDown);
        }
        catch (Exception ex)
        {
            LogGuardFailure("读取 K 输入状态失败", ex);
            Disarm();
            return false;
        }

        bool firstSuppression = !_enteredDodgeOrDodgeAttack;
        _enteredDodgeOrDodgeAttack = true;

        if (pressed || buffered)
        {
            try
            {
                // 布尔重载会同时清理 PressDown 缓冲和当前输入动作状态，
                // 避免同一帧的 WasPressed 在新的源攻击动作中再次存活。
                _input.ClearKeyBuffer(KeyType.Dodge, true);
            }
            catch (Exception ex)
            {
                LogGuardFailure("清理 K 跨动作状态失败", ex);
            }
        }

        if (!firstSuppression || _suppressLogged)
        {
            return true;
        }

        _suppressLogged = true;
        Plugin.Logger.LogWarning(
            $"[自动旋风斩] 已抑制 THW_Dodge 内自动 K 的二次取消："
            + $"源动作={_sourceMotionName} 帧={_sourceMotionFrame}，"
            + $"当前动作={currentMotionName} 类型={currentMotionType} 帧={currentMotionFrame}，"
            + $"逻辑事件来源={source} K按下={pressed} K缓冲={buffered}。");

        return true;
    }

    internal static bool TrySuppressDodgeAttackTargetMotion(
        MotionActor_LC2 actor,
        BaseMotionData targetMotion,
        SetMotionPriority priority,
        int startFrame)
    {
        if (!CanUseGuard(actor, out MotionType_LC2 currentMotionType,
                out string currentMotionName, out int currentMotionFrame))
        {
            return false;
        }

        bool currentIsDodgeAction =
            ContainsMotionType(currentMotionType, MotionType_LC2.Dodge)
            || ContainsMotionType(currentMotionType, MotionType_LC2.DodgeAttack);
        if (!currentIsDodgeAction || targetMotion == null)
        {
            return false;
        }

        BaseMotionData_LC2 targetMotionLc2;
        try
        {
            targetMotionLc2 = targetMotion.TryCast<BaseMotionData_LC2>();
        }
        catch (Exception ex)
        {
            LogGuardFailure("读取目标动作类型失败", ex);
            return false;
        }

        if (targetMotionLc2 == null
            || !ContainsMotionType(
                targetMotionLc2.motionType_lc2,
                MotionType_LC2.DodgeAttack))
        {
            return false;
        }

        if (!_targetBlockLogged)
        {
            _targetBlockLogged = true;
            string targetMotionName = string.IsNullOrEmpty(targetMotionLc2.motionName)
                ? "未知"
                : targetMotionLc2.motionName;
            Plugin.Logger.LogWarning(
                $"[自动旋风斩] 已拦截 Dodge -> DodgeAttack 目标切换："
                + $"源动作={_sourceMotionName} 帧={_sourceMotionFrame}，"
                + $"当前动作={currentMotionName} 类型={currentMotionType} 帧={currentMotionFrame}，"
                + $"目标动作={targetMotionName} 类型={targetMotionLc2.motionType_lc2}，"
                + $"优先级={priority} 起始帧={startFrame}。");
        }

        return true;
    }

    private static bool CanUseGuard(
        MotionActor_LC2 actor,
        out MotionType_LC2 currentMotionType,
        out string currentMotionName,
        out int currentMotionFrame)
    {
        currentMotionType = MotionType_LC2.None;
        currentMotionName = string.Empty;
        currentMotionFrame = 0;

        if (_input == null || _inputPointer == IntPtr.Zero || _motionPointer == IntPtr.Zero)
        {
            return false;
        }

        if (Time.unscaledTime - _armedAt > MaximumGuardSeconds)
        {
            Disarm();
            return false;
        }

        if (actor == null || actor.Pointer == IntPtr.Zero || actor.Pointer != _motionPointer)
        {
            return false;
        }

        if (_input.Pointer != _inputPointer)
        {
            Disarm();
            return false;
        }

        try
        {
            currentMotionType = actor.CurRuntimeMotionType;
            currentMotionName = actor.CurRuntimeMotionName;
            currentMotionFrame = actor.CurFrame;
            return true;
        }
        catch (Exception ex)
        {
            LogGuardFailure("读取当前动作失败", ex);
            Disarm();
            return false;
        }
    }

    private static bool ContainsMotionType(MotionType_LC2 actual, MotionType_LC2 expected)
    {
        return (actual & expected) == expected;
    }

    private static void LogGuardFailure(string action, Exception ex)
    {
        Plugin.Logger?.LogError(
            $"[自动旋风斩] 闪避取消守卫{action}：{ex.GetType().Name}: {ex.Message}");
    }
}
