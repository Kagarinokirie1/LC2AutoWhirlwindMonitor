using HunterMotion;

namespace LC2.AutoWhirlwindMonitor;

// K 是 MotionEvent_DodgeCancel 的取消输入源。当前动作已经属于 Dodge 或
// DodgeAttack 时再注入 K，会让游戏锁存新的冲刺取消标志并在后续逻辑帧续接
// THW_DodgeAttack。这里只阻止危险输入源，不改写游戏动作状态，也不等待重开。
internal sealed partial class AutoWhirlwindController
{
    private string _dodgeSourceMotionName = string.Empty;
    private MotionType_LC2 _dodgeSourceMotionType = MotionType_LC2.None;
    private bool _dodgeTapGuardLogged;

    private void CaptureDodgeTapSource(RuntimeSnapshot snapshot)
    {
        _dodgeSourceMotionName = string.IsNullOrEmpty(snapshot?.MotionName)
            ? "未知"
            : snapshot.MotionName;
        _dodgeSourceMotionType = snapshot?.MotionType ?? MotionType_LC2.None;
        _dodgeTapGuardLogged = false;
    }

    private bool ShouldBlockDodgeTap(RuntimeSnapshot snapshot)
    {
        if (snapshot == null
            || (!ContainsMotionType(snapshot.MotionType, MotionType_LC2.Dodge)
                && !ContainsMotionType(snapshot.MotionType, MotionType_LC2.DodgeAttack)))
        {
            return false;
        }

        if (!_dodgeTapGuardLogged)
        {
            _dodgeTapGuardLogged = true;
            Plugin.Logger.LogWarning(
                $"[自动旋风斩] 已跳过危险 K：源动作={_dodgeSourceMotionName} 类型={_dodgeSourceMotionType}，"
                + $"当前动作={snapshot.MotionName} 类型={snapshot.MotionType} 帧={snapshot.MotionFrame}。"
                + "当前动作已经包含 Dodge/DodgeAttack，继续注入 K 会锁存新的冲刺取消。");
        }

        return true;
    }

    private static bool ContainsMotionType(MotionType_LC2 actual, MotionType_LC2 expected)
    {
        return (actual & expected) == expected;
    }
}
