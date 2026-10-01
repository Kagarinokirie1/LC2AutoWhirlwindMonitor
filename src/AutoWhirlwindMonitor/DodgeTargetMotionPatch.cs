using System;
using HarmonyLib;
using HunterMotion;
using Il2CppInterop.Runtime.InteropTypes;

namespace LC2.AutoWhirlwindMonitor;

// DodgeCancel 会先把 K 和方向状态锁存，再通过 SetTargetMotion 切换到冲刺攻击。
// 即使清掉了 K 缓冲，已经锁存的目标切换仍可能继续执行，因此在最终动作切换点再做一次
// 精确拦截，只拒绝自动循环守卫范围内的 Dodge/DodgeAttack -> DodgeAttack。
[HarmonyPatch(
    typeof(MotionActor),
    nameof(MotionActor.SetTargetMotion),
    new[] { typeof(BaseMotionData), typeof(SetMotionPriority), typeof(int) })]
internal static class DodgeTargetMotionPatch
{
    [HarmonyPrefix]
    private static bool Prefix(
        MotionActor __instance,
        BaseMotionData inMotion,
        SetMotionPriority inPriority,
        int startFrame,
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
                $"[自动旋风斩] 拦截 DodgeAttack 目标切换失败："
                + $"{ex.GetType().Name}: {ex.Message}");
            return true;
        }

        __result = false;
        return false;
    }
}
