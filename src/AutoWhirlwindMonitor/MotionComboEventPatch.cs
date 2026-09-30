using HarmonyLib;
using HunterMotion;
using UnityEngine;

namespace LC2.AutoWhirlwindMonitor;

[HarmonyPatch(typeof(MotionActor), nameof(MotionActor.MotionComboReady))]
internal static class MotionComboReadyPatch
{
    [HarmonyPostfix]
    private static void Postfix(MotionActor __instance, BaseMotionData nextMotion)
    {
        MonitorBehaviour.PulseMotionComboReady(__instance, nextMotion, Time.unscaledTime);
    }
}
