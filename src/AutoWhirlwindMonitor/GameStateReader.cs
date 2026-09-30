using System;
using Hunter.Common;
using Hunter.Input;
using HunterMotion;
using Il2CppInterop.Runtime.InteropTypes;
using LC2.Power;

namespace LC2.AutoWhirlwindMonitor;

internal static class GameStateReader
{
    internal static RuntimeSnapshot Read()
    {
        return Read(out _);
    }

    internal static RuntimeSnapshot Read(out CreatureInputCtrl input)
    {
        input = null;
        try
        {
            PlayerManager playerManager = Singleton<PlayerManager>.Instance;
            if (playerManager == null)
            {
                return RuntimeSnapshot.Fault("PlayerManager.Instance 为空");
            }

            Player player = playerManager.LocalPlayer;
            if (player == null)
            {
                return RuntimeSnapshot.Fault("LocalPlayer 为空");
            }

            Hero hero = player.OwnerHero;
            if (hero == null)
            {
                return RuntimeSnapshot.Fault("OwnerHero 为空");
            }

            Weapon weapon = hero.Weapon;
            WeaponRuntimeData weaponRuntime = weapon == null ? null : weapon.RuntimeData;
            if (weaponRuntime == null)
            {
                return RuntimeSnapshot.Fault("当前武器或 WeaponRuntimeData 为空");
            }

            HeroRuntimeData heroRuntime = hero.RuntimeData;
            input = player.mGameInputCtrl;
            if (input == null)
            {
                input = hero.InputCtrl;
            }

            MotionActor_LC2 motion = ResolveMotion(hero.MotionActor);
            ReadChargeCounters(
                weaponRuntime.WeaponType == WeaponType.Two_Handed_Melee ? hero : null,
                out bool chargeCounterAvailable,
                out bool chargeLevelCounterAvailable,
                out bool chargeThresholdCounterAvailable,
                out float chargeEnergy,
                out float maxChargeEnergy,
                out float chargeThresholdLevel1,
                out float chargeThresholdLevel2,
                out float chargeThresholdLevel3,
                out int chargeLevel,
                out int maxChargeLevel);

            return RuntimeSnapshot.Create(
                true,
                true,
                true,
                input != null,
                motion != null,
                string.Empty,
                weaponRuntime.WeaponType,
                heroRuntime == null ? 0f : heroRuntime.AtkSpd,
                heroRuntime == null ? 0f : heroRuntime.BasicAtkSpd,
                chargeCounterAvailable,
                chargeLevelCounterAvailable,
                chargeThresholdCounterAvailable,
                chargeEnergy,
                maxChargeEnergy,
                chargeThresholdLevel1,
                chargeThresholdLevel2,
                chargeThresholdLevel3,
                chargeLevel,
                maxChargeLevel,
                input != null && input.Locked,
                player.IsGameInputCoolDown,
                motion == null ? string.Empty : motion.CurRuntimeMotionName,
                motion == null ? MotionType_LC2.None : motion.CurRuntimeMotionType,
                motion == null ? MotionStateType.None : motion.CurRuntimeMotionStateType,
                motion == null ? 0f : motion.CurFloatTime,
                motion == null ? 0 : motion.CurFrame,
                motion == null ? 0f : motion.SpeedRate,
                motion == null ? 0f : motion.PlaySpeed,
                motion == null ? 0f : motion.TimeScale,
                input != null && input.GetGameKeyState(KeyType.Attack, KeyState.PressDown),
                input != null && input.GetGameKeyState(KeyType.Attack, KeyState.Pressing),
                input != null && input.GetGameKeyState(KeyType.Attack, KeyState.Released),
                input != null && input.GetGameKeyStateIncludeBuffer(KeyType.Attack, KeyState.PressDown),
                input == null ? 0f : input.GetGameKeyPressTime(KeyType.Attack),
                input != null && input.GetGameKeyState(KeyType.Dodge, KeyState.PressDown),
                input != null && input.GetGameKeyState(KeyType.Dodge, KeyState.Released),
                input != null && input.GetGameKeyStateIncludeBuffer(KeyType.Dodge, KeyState.PressDown),
                input == null ? 0f : input.GetGameKeyPressTime(KeyType.Dodge),
                input != null && input.GetGameKeyState(KeyType.HardAttack, KeyState.PressDown),
                input != null && input.GetGameKeyState(KeyType.HardAttack, KeyState.Pressing),
                input != null && input.GetGameKeyState(KeyType.HardAttack, KeyState.Released),
                input != null && input.GetGameKeyState(KeyType.HardAttack, KeyState.LongPressed),
                input != null && input.GetGameKeyStateIncludeBuffer(KeyType.HardAttack, KeyState.PressDown),
                input == null ? 0f : input.GetGameKeyPressTime(KeyType.HardAttack));
        }
        catch (Exception ex)
        {
            return RuntimeSnapshot.Fault($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    internal static bool TryGetLocalMotionActor(out MotionActor_LC2 motion)
    {
        motion = null;
        try
        {
            PlayerManager playerManager = Singleton<PlayerManager>.Instance;
            Player player = playerManager == null ? null : playerManager.LocalPlayer;
            Hero hero = player == null ? null : player.OwnerHero;
            motion = hero == null ? null : ResolveMotion(hero.MotionActor);
            return motion != null;
        }
        catch
        {
            motion = null;
            return false;
        }
    }

    private static MotionActor_LC2 ResolveMotion(MotionActor actor)
    {
        if (actor == null)
        {
            return null;
        }

        return actor.TryCast<MotionActor_LC2>();
    }

    private static void ReadChargeCounters(
        Hero hero,
        out bool available,
        out bool chargeLevelCounterAvailable,
        out bool chargeThresholdCounterAvailable,
        out float chargeEnergy,
        out float maxChargeEnergy,
        out float chargeThresholdLevel1,
        out float chargeThresholdLevel2,
        out float chargeThresholdLevel3,
        out int chargeLevel,
        out int maxChargeLevel)
    {
        available = false;
        chargeLevelCounterAvailable = false;
        chargeThresholdCounterAvailable = false;
        chargeEnergy = 0f;
        maxChargeEnergy = 0f;
        chargeThresholdLevel1 = 0f;
        chargeThresholdLevel2 = 0f;
        chargeThresholdLevel3 = 0f;
        chargeLevel = 0;
        maxChargeLevel = 0;

        if (hero == null)
        {
            return;
        }

        try
        {
            PowerExecutor powerExecutor = hero.PowerExecutor;
            if (powerExecutor == null)
            {
                return;
            }

            string chargeKey = PFAM_THW_Mod.CounterStr_THW_Charge;
            string chargeLevelKey = PFAM_THW_Mod.CounterStr_THW_ChargeLevel;
            string chargeThresholdLevel1Key = PFAM_THW_Mod.HideCounterStr_THW_Hide_ChargeLevel1;
            string chargeThresholdLevel2Key = PFAM_THW_Mod.HideCounterStr_THW_Hide_ChargeLevel2;
            string chargeThresholdLevel3Key = PFAM_THW_Mod.HideCounterStr_THW_Hide_ChargeLevel3;

            maxChargeEnergy = powerExecutor.Counter_GetMaxValue(chargeKey);
            if (maxChargeEnergy > 0f)
            {
                chargeEnergy = Math.Max(0f, powerExecutor.Counter_GetCurValue(chargeKey));
                available = true;
            }

            float counterMaxChargeLevel = powerExecutor.Counter_GetMaxValue(chargeLevelKey);
            maxChargeLevel = counterMaxChargeLevel > 0f && counterMaxChargeLevel <= 100f
                ? (int)Math.Round(counterMaxChargeLevel)
                : 0;
            float currentChargeLevel = powerExecutor.Counter_GetCurValue(chargeLevelKey);
            bool currentChargeLevelValid = !float.IsNaN(currentChargeLevel)
                && !float.IsInfinity(currentChargeLevel)
                && currentChargeLevel >= 0f
                && currentChargeLevel <= 100f;
            if (currentChargeLevelValid)
            {
                chargeLevel = (int)Math.Round(currentChargeLevel);
            }

            chargeLevelCounterAvailable = counterMaxChargeLevel > 0f
                && currentChargeLevelValid;

            // WeaponUI_THW 的三格蓄力条直接读取这三个隐藏计数器：
            // 第一格满 = Hide_ChargeLevel1，第二格满 = 第一格 + Hide_ChargeLevel2。
            float hiddenThreshold1 = powerExecutor.Counter_GetCurValue(chargeThresholdLevel1Key);
            float hiddenThreshold2 = powerExecutor.Counter_GetCurValue(chargeThresholdLevel2Key);
            float hiddenThreshold3 = powerExecutor.Counter_GetCurValue(chargeThresholdLevel3Key);
            bool hiddenThreshold1Valid = IsValidPositiveCounter(hiddenThreshold1);
            bool hiddenThreshold2Valid = IsValidPositiveCounter(hiddenThreshold2);
            bool hiddenThreshold3Valid = IsValidPositiveCounter(hiddenThreshold3);
            if (hiddenThreshold1Valid)
            {
                chargeThresholdLevel1 = hiddenThreshold1;
            }

            if (hiddenThreshold1Valid && hiddenThreshold2Valid)
            {
                chargeThresholdLevel2 = hiddenThreshold1 + hiddenThreshold2;
                chargeThresholdCounterAvailable = true;
            }

            if (hiddenThreshold1Valid && hiddenThreshold2Valid && hiddenThreshold3Valid)
            {
                chargeThresholdLevel3 = hiddenThreshold1 + hiddenThreshold2 + hiddenThreshold3;
            }
        }
        catch
        {
            available = false;
            chargeLevelCounterAvailable = false;
            chargeThresholdCounterAvailable = false;
            chargeEnergy = 0f;
            maxChargeEnergy = 0f;
            chargeThresholdLevel1 = 0f;
            chargeThresholdLevel2 = 0f;
            chargeThresholdLevel3 = 0f;
            chargeLevel = 0;
            maxChargeLevel = 0;
        }
    }

    private static bool IsValidPositiveCounter(float value)
    {
        return !float.IsNaN(value)
            && !float.IsInfinity(value)
            && value > 0f;
    }
}
