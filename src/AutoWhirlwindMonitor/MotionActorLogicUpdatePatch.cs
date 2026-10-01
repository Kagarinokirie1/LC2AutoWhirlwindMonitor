using HarmonyLib;
using HunterMotion;
using Il2CppInterop.Runtime.InteropTypes;

namespace LC2.AutoWhirlwindMonitor;

// LogicUpdate 是动作事件执行入口。这里提前调用同一个守卫，作为事件方法补丁
// 之外的兜底；真正阻止二次取消的是 DodgeCancelEventPatch 返回 false。
[HarmonyPatch(typeof(MotionActor), nameof(MotionActor.LogicUpdate), new[] { typeof(float) })]
internal static class MotionActorLogicUpdatePatch
{
    [HarmonyPrefix]
    private static void Prefix(MotionActor __instance)
    {
        MotionActor_LC2 actor = __instance?.TryCast<MotionActor_LC2>();
        if (actor != null)
        {
            DodgeCancelGuard.TrySuppressDodgeCancelEvent(actor, "MotionActor.LogicUpdate");
        }
    }
}
