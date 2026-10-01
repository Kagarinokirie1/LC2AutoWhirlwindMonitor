using System;
using HarmonyLib;
using HunterMotion;
using Il2CppInterop.Runtime.InteropTypes;

namespace LC2.AutoWhirlwindMonitor;

// MotionEvent_ChangeMotion.ChangeToNextMotion 实际调用的是四参数 ForceSet 重载。
// 这里在连招真正提交目标动作的出口拦截 Dodge/DodgeAttack -> DodgeAttack，
// 避免闪避动作已经建立后，同一次自动 K 又被连招事件消费成冲刺攻击。
[HarmonyPatch(
    typeof(MotionActor),
    nameof(MotionActor.SetTargetMotion_ForceSet),
    new[] { typeof(BaseMotionData), typeof(SetMotionPriority), typeof(int), typeof(bool) })]
internal static class DodgeTargetMotionForceSetPatch
{
    [HarmonyPrefix]
    private static bool Prefix(
        MotionActor __instance,
        BaseMotionData inMotion,
        SetMotionPriority inPriority,
        int startFrame,
        bool stayPriority,
        ref bool __result)
    {
        MotionActor_LC2 actor = __instance?.TryCast<MotionActor_LC2>();
        if (actor == null)
        {
            return true;
        }

        try
        {
            if (!DodgeCancelGuard.TrySuppressDodgeAttackTargetMotion(
                    actor,
                    inMotion,
                    inPriority,
                    startFrame))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError(
                $"[自动旋风斩] 拦截 ForceSet DodgeAttack 目标切换失败："
                + $"{ex.GetType().Name}: {ex.Message}");
            return true;
        }

        __result = false;
        return false;
    }
}
