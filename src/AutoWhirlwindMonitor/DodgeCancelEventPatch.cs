using HarmonyLib;
using HunterMotion;

namespace LC2.AutoWhirlwindMonitor;

[HarmonyPatch(
    typeof(MotionEvent_DodgeCancel),
    nameof(MotionEvent_DodgeCancel.EventUpdate_Logic),
    new[] { typeof(int) })]
internal static class DodgeCancelEventPatch
{
    [HarmonyPrefix]
    private static bool Prefix(MotionEvent_DodgeCancel __instance)
    {
        MotionActor_LC2 actor = __instance?._actorLC2;
        if (actor == null)
        {
            return true;
        }

        DodgeCancelGuard.LogEventPatchHit(actor);
        return !DodgeCancelGuard.TrySuppressDodgeCancelEvent(
            actor, "MotionEvent_DodgeCancel.EventUpdate_Logic");
    }
}
