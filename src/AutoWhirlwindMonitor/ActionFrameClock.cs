using System;

namespace LC2.AutoWhirlwindMonitor;

// 自动循环的阶段时钟。穿怪命中停顿会让动作帧几乎停住，但 Time.unscaledTime
// 仍在推进；如果阶段仍按现实秒计时，J 之后的 K/L 会被压缩到 J 之后一两帧里，
// 闪避取消窗口就会误接出 THW_DodgeAttack 连锁。手动操作能连招，是因为人的
// 节奏跟着画面走。因此这里用动作帧推进量换算阶段时钟，命中停顿期间自动降速。
internal sealed partial class AutoWhirlwindController
{
    // 实测动作帧在正常速度下约每秒推进 60 帧；阶段时长按现实秒配置，
    // 这里用动作帧推进量反推阶段时钟。
    private const float NominalActionFramesPerSecond = 60f;
    private const float MaxActionFrameCredit = 6f;

    private float _logicalTime;
    private float _lastPulseTime;
    private string _lastMotionName = string.Empty;
    private int _lastMotionFrame;
    private float _actionFrameCredit;
    private bool _paceThrottled;

    private void ResetActionFrameClock(RuntimeSnapshot snapshot, float now)
    {
        _logicalTime = now;
        _lastPulseTime = now;
        _lastMotionName = snapshot == null ? string.Empty : snapshot.MotionName;
        _lastMotionFrame = snapshot == null ? 0 : snapshot.MotionFrame;
        _actionFrameCredit = 0f;
        _paceThrottled = false;
    }

    private void ClearActionFrameClock()
    {
        _lastPulseTime = 0f;
        _lastMotionName = string.Empty;
        _lastMotionFrame = 0;
        _actionFrameCredit = 0f;
        _paceThrottled = false;
    }

    private void AdvanceLogicalTime(RuntimeSnapshot snapshot, float now)
    {
        float rawDelta = now - _lastPulseTime;
        _lastPulseTime = now;
        if (rawDelta < 0f)
        {
            rawDelta = 0f;
        }

        float logicalDelta = ResolveLogicalDelta(snapshot, rawDelta);
        _logicalTime += logicalDelta;
        LogPaceChange(snapshot, rawDelta, logicalDelta);
    }

    private float ResolveLogicalDelta(RuntimeSnapshot snapshot, float rawDelta)
    {
        if (snapshot == null)
        {
            return rawDelta;
        }

        bool sameMotion = string.Equals(
            snapshot.MotionName,
            _lastMotionName,
            StringComparison.Ordinal);
        int frameDelta = snapshot.MotionFrame - _lastMotionFrame;
        _lastMotionName = snapshot.MotionName;
        _lastMotionFrame = snapshot.MotionFrame;

        if (!sameMotion || frameDelta < 0)
        {
            // 动作切换或同名动作重播时动作帧会归零，这一帧按正常速度放行。
            _actionFrameCredit = 0f;
            return rawDelta;
        }

        // 保留未用完的动作帧余额，避免帧推进量化导致时钟漂移；
        // 同时用 rawDelta 封顶，保证阶段不会比现实时间走得更快。
        _actionFrameCredit += frameDelta;
        float frameBudget = _actionFrameCredit / NominalActionFramesPerSecond;
        float logicalDelta = frameBudget < rawDelta ? frameBudget : rawDelta;
        _actionFrameCredit -= logicalDelta * NominalActionFramesPerSecond;
        if (_actionFrameCredit < 0f)
        {
            _actionFrameCredit = 0f;
        }
        else if (_actionFrameCredit > MaxActionFrameCredit)
        {
            _actionFrameCredit = MaxActionFrameCredit;
        }

        return logicalDelta;
    }

    private void LogPaceChange(RuntimeSnapshot snapshot, float rawDelta, float logicalDelta)
    {
        if (rawDelta <= 0.0001f)
        {
            return;
        }

        float pace = logicalDelta / rawDelta;
        if (pace < 0.5f)
        {
            if (_paceThrottled)
            {
                return;
            }

            _paceThrottled = true;
            Plugin.Logger.LogInfo(
                $"[自动旋风斩] 检测到命中停顿，阶段节拍降为动作帧速度："
                + $"动作={DescribeMotion(snapshot)} 节拍={pace:0.###}。");
            return;
        }

        if (_paceThrottled && pace > 0.9f)
        {
            _paceThrottled = false;
            Plugin.Logger.LogInfo(
                $"[自动旋风斩] 命中停顿结束，阶段节拍恢复正常："
                + $"动作={DescribeMotion(snapshot)}。");
        }
    }

    private static string DescribeMotion(RuntimeSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return "无";
        }

        return $"{snapshot.MotionName} 帧={snapshot.MotionFrame} 时间倍率={snapshot.MotionTimeScale:0.###}";
    }
}
