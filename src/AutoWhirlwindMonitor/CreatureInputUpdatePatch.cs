using HarmonyLib;
using LC2;

namespace LC2.AutoWhirlwindMonitor;

[HarmonyPatch(typeof(CreatureInputCtrl), nameof(CreatureInputCtrl.UpdateInput))]
internal static class CreatureInputUpdatePatch
{
    [HarmonyPostfix]
    private static void Postfix(CreatureInputCtrl __instance)
    {
        MonitorBehaviour.PulseGameInput(__instance);
    }
}
