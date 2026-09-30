using System.Collections.Generic;
using Hunter.Input;

namespace LC2.AutoWhirlwindMonitor;

internal sealed class InputStateInjector
{
    private const float LongPressThresholdSeconds = 0.2f;

    private readonly HashSet<KeyType> _activeKeys = new();
    private readonly HashSet<KeyType> _warnedKeys = new();

    internal bool IsPressing(KeyType keyType)
    {
        return _activeKeys.Contains(keyType);
    }

    internal bool TryBeginPress(CreatureInputCtrl input, KeyType keyType)
    {
        if (input == null)
        {
            return false;
        }

        try
        {
            // 物理按下会同时建立 PressDown 和 Pressing。Pressing, 0 虽然也会写入
            // WasPressed，但不会创建 PressDown 的跨帧缓冲，因此这里显式补建。
            input.ClearKeyBuffer(keyType, true);
            input.SetInput(keyType, KeyState.PressDown, 0f);
            input.SetInput(keyType, KeyState.Pressing, 0f);
            _activeKeys.Add(keyType);

            LC2.KeyActionState state = ResolveState(input, keyType);
            if (state == null)
            {
                return false;
            }

            state.Pressing = true;
            state.WasShortPressed = false;
            state.WasDoublePressed = false;
            state.WasReleased = false;
            state.Releasing = false;
            state.IsLongPressed = false;
            state.PressedTime = 0f;
            state.Value = 1f;
            state.ValueWithThreshold = 1f;
            SyncStateDictionary(state, state.WasPressed);
            _warnedKeys.Remove(keyType);
            return true;
        }
        catch (System.Exception ex)
        {
            WarnOnce(keyType, "建立按下状态失败", ex);
            return false;
        }
    }

    internal bool TryMaintainPress(CreatureInputCtrl input, KeyType keyType, float pressTime)
    {
        if (input == null || !_activeKeys.Contains(keyType))
        {
            return false;
        }

        try
        {
            LC2.KeyActionState state = ResolveState(input, keyType);
            if (state == null)
            {
                return false;
            }

            // 非零时长的 Pressing 分支只刷新 PressedTime 和输入缓冲，
            // 不会写入 Pressing 字段，因此仍需在同一帧补齐状态。
            input.SetInput(keyType, KeyState.Pressing, pressTime);

            // 后续帧不能再次设置 WasPressed，否则游戏会把长按识别成连续点击。
            state.WasPressed = false;
            state.Pressing = true;
            state.WasShortPressed = false;
            state.WasDoublePressed = false;
            state.WasReleased = false;
            state.Releasing = false;
            state.IsLongPressed = pressTime >= LongPressThresholdSeconds;
            state.PressedTime = pressTime;
            state.Value = 1f;
            state.ValueWithThreshold = 1f;
            SyncStateDictionary(state, false);
            _warnedKeys.Remove(keyType);
            return true;
        }
        catch (System.Exception ex)
        {
            WarnOnce(keyType, "维持按下状态失败", ex);
            return false;
        }
    }

    internal bool TryRelease(CreatureInputCtrl input, KeyType keyType, float pressTime)
    {
        if (input == null || !_activeKeys.Remove(keyType))
        {
            return false;
        }

        try
        {
            // 松开事件只需在同一帧写入一次；后续阶段由游戏自己的缓冲清理。
            input.SetInput(keyType, KeyState.Released, pressTime);
            input.SetInput(keyType, KeyState.Releasing, pressTime);
            _warnedKeys.Remove(keyType);
            return true;
        }
        catch (System.Exception ex)
        {
            WarnOnce(keyType, "写入松开状态失败", ex);
            return false;
        }
    }

    internal void Reset()
    {
        _activeKeys.Clear();
        _warnedKeys.Clear();
    }

    private static LC2.KeyActionState ResolveState(CreatureInputCtrl input, KeyType keyType)
    {
        Il2CppSystem.Collections.Generic.Dictionary<KeyType, LC2.KeyActionState> states =
            input._curFrameInputActionState;
        if (states == null)
        {
            return null;
        }

        return states.TryGetValue(keyType, out LC2.KeyActionState state) ? state : null;
    }

    private static void SyncStateDictionary(LC2.KeyActionState state, bool wasPressed)
    {
        Il2CppSystem.Collections.Generic.Dictionary<KeyState, bool> states = state.KeyStateDict;
        if (states == null)
        {
            return;
        }

        states[KeyState.PressDown] = wasPressed;
        states[KeyState.Pressing] = true;
        states[KeyState.ShortPressed] = false;
        states[KeyState.DoublePressed] = false;
        states[KeyState.Released] = false;
        states[KeyState.LongPressed] = state.IsLongPressed;
    }

    private void WarnOnce(KeyType keyType, string action, System.Exception ex)
    {
        if (_warnedKeys.Add(keyType))
        {
            Plugin.Logger?.LogWarning(
                $"[自动旋风斩] {action}：按键={keyType} 错误={ex.GetType().Name}: {ex.Message}");
        }
    }
}
