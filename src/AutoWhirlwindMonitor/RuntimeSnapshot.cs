using System.Globalization;
using Hunter.Input;
using HunterMotion;

namespace LC2.AutoWhirlwindMonitor;

internal sealed class RuntimeSnapshot
{
    private RuntimeSnapshot()
    {
    }

    internal bool PlayerFound { get; private set; }
    internal bool HeroFound { get; private set; }
    internal bool WeaponFound { get; private set; }
    internal bool InputFound { get; private set; }
    internal bool MotionFound { get; private set; }
    internal string Error { get; private set; }
    internal WeaponType WeaponType { get; private set; }
    internal float AttackSpeed { get; private set; }
    internal float BasicAttackSpeed { get; private set; }
    internal bool ChargeCounterAvailable { get; private set; }
    internal bool ChargeLevelCounterAvailable { get; private set; }
    internal bool ChargeThresholdCounterAvailable { get; private set; }
    internal float ChargeEnergy { get; private set; }
    internal float MaxChargeEnergy { get; private set; }
    internal float ChargeThresholdLevel1 { get; private set; }
    internal float ChargeThresholdLevel2 { get; private set; }
    internal float ChargeThresholdLevel3 { get; private set; }
    internal int ChargeLevel { get; private set; }
    internal int MaxChargeLevel { get; private set; }
    internal bool InputLocked { get; private set; }
    internal bool GameInputCoolDown { get; private set; }
    internal string MotionName { get; private set; }
    internal MotionType_LC2 MotionType { get; private set; }
    internal MotionStateType MotionStateType { get; private set; }
    internal float MotionTime { get; private set; }
    internal int MotionFrame { get; private set; }
    internal float MotionSpeedRate { get; private set; }
    internal float MotionPlaySpeed { get; private set; }
    internal float MotionTimeScale { get; private set; }
    internal bool AttackDown { get; private set; }
    internal bool AttackHeld { get; private set; }
    internal bool AttackReleased { get; private set; }
    internal bool AttackBuffered { get; private set; }
    internal float AttackPressTime { get; private set; }
    internal bool DodgeDown { get; private set; }
    internal bool DodgeReleased { get; private set; }
    internal bool DodgeBuffered { get; private set; }
    internal float DodgePressTime { get; private set; }
    internal bool HardAttackDown { get; private set; }
    internal bool HardAttackHeld { get; private set; }
    internal bool HardAttackReleased { get; private set; }
    internal bool HardAttackLongPressed { get; private set; }
    internal bool HardAttackBuffered { get; private set; }
    internal float HardAttackPressTime { get; private set; }

    internal static RuntimeSnapshot Create(
        bool playerFound,
        bool heroFound,
        bool weaponFound,
        bool inputFound,
        bool motionFound,
        string error,
        WeaponType weaponType,
        float attackSpeed,
        float basicAttackSpeed,
        bool chargeCounterAvailable,
        bool chargeLevelCounterAvailable,
        bool chargeThresholdCounterAvailable,
        float chargeEnergy,
        float maxChargeEnergy,
        float chargeThresholdLevel1,
        float chargeThresholdLevel2,
        float chargeThresholdLevel3,
        int chargeLevel,
        int maxChargeLevel,
        bool inputLocked,
        bool gameInputCoolDown,
        string motionName,
        MotionType_LC2 motionType,
        MotionStateType motionStateType,
        float motionTime,
        int motionFrame,
        float motionSpeedRate,
        float motionPlaySpeed,
        float motionTimeScale,
        bool attackDown,
        bool attackHeld,
        bool attackReleased,
        bool attackBuffered,
        float attackPressTime,
        bool dodgeDown,
        bool dodgeReleased,
        bool dodgeBuffered,
        float dodgePressTime,
        bool hardAttackDown,
        bool hardAttackHeld,
        bool hardAttackReleased,
        bool hardAttackLongPressed,
        bool hardAttackBuffered,
        float hardAttackPressTime)
    {
        return new RuntimeSnapshot
        {
            PlayerFound = playerFound,
            HeroFound = heroFound,
            WeaponFound = weaponFound,
            InputFound = inputFound,
            MotionFound = motionFound,
            Error = error ?? string.Empty,
            WeaponType = weaponType,
            AttackSpeed = attackSpeed,
            BasicAttackSpeed = basicAttackSpeed,
            ChargeCounterAvailable = chargeCounterAvailable,
            ChargeLevelCounterAvailable = chargeLevelCounterAvailable,
            ChargeThresholdCounterAvailable = chargeThresholdCounterAvailable,
            ChargeEnergy = chargeEnergy,
            MaxChargeEnergy = maxChargeEnergy,
            ChargeThresholdLevel1 = chargeThresholdLevel1,
            ChargeThresholdLevel2 = chargeThresholdLevel2,
            ChargeThresholdLevel3 = chargeThresholdLevel3,
            ChargeLevel = chargeLevel,
            MaxChargeLevel = maxChargeLevel,
            InputLocked = inputLocked,
            GameInputCoolDown = gameInputCoolDown,
            MotionName = motionName ?? string.Empty,
            MotionType = motionType,
            MotionStateType = motionStateType,
            MotionTime = motionTime,
            MotionFrame = motionFrame,
            MotionSpeedRate = motionSpeedRate,
            MotionPlaySpeed = motionPlaySpeed,
            MotionTimeScale = motionTimeScale,
            AttackDown = attackDown,
            AttackHeld = attackHeld,
            AttackReleased = attackReleased,
            AttackBuffered = attackBuffered,
            AttackPressTime = attackPressTime,
            DodgeDown = dodgeDown,
            DodgeReleased = dodgeReleased,
            DodgeBuffered = dodgeBuffered,
            DodgePressTime = dodgePressTime,
            HardAttackDown = hardAttackDown,
            HardAttackHeld = hardAttackHeld,
            HardAttackReleased = hardAttackReleased,
            HardAttackLongPressed = hardAttackLongPressed,
            HardAttackBuffered = hardAttackBuffered,
            HardAttackPressTime = hardAttackPressTime
        };
    }

    internal static RuntimeSnapshot Fault(string error)
    {
        RuntimeSnapshot snapshot = Create(
            false,
            false,
            false,
            false,
            false,
            error,
            WeaponType.None,
            0f,
            0f,
            false,
            false,
            false,
            0f,
            0f,
            0f,
            0f,
            0f,
            0,
            0,
            false,
            false,
            string.Empty,
            MotionType_LC2.None,
            MotionStateType.None,
            0f,
            0,
            0f,
            0f,
            0f,
            false,
            false,
            false,
            false,
            0f,
            false,
            false,
            false,
            0f,
            false,
            false,
            false,
            false,
            false,
            0f);
        return snapshot;
    }

    internal bool HasSameInputOrMotionState(RuntimeSnapshot other)
    {
        if (other == null)
        {
            return false;
        }

        return PlayerFound == other.PlayerFound
            && HeroFound == other.HeroFound
            && WeaponFound == other.WeaponFound
            && InputFound == other.InputFound
            && MotionFound == other.MotionFound
            && Error == other.Error
            && WeaponType == other.WeaponType
            && ChargeCounterAvailable == other.ChargeCounterAvailable
            && ChargeLevelCounterAvailable == other.ChargeLevelCounterAvailable
            && ChargeThresholdCounterAvailable == other.ChargeThresholdCounterAvailable
            && ChargeThresholdLevel1 == other.ChargeThresholdLevel1
            && ChargeThresholdLevel2 == other.ChargeThresholdLevel2
            && ChargeThresholdLevel3 == other.ChargeThresholdLevel3
            && ChargeLevel == other.ChargeLevel
            && MaxChargeLevel == other.MaxChargeLevel
            && InputLocked == other.InputLocked
            && GameInputCoolDown == other.GameInputCoolDown
            && MotionName == other.MotionName
            && MotionType == other.MotionType
            && MotionStateType == other.MotionStateType
            && AttackDown == other.AttackDown
            && AttackHeld == other.AttackHeld
            && AttackReleased == other.AttackReleased
            && AttackBuffered == other.AttackBuffered
            && DodgeDown == other.DodgeDown
            && DodgeReleased == other.DodgeReleased
            && DodgeBuffered == other.DodgeBuffered
            && HardAttackDown == other.HardAttackDown
            && HardAttackHeld == other.HardAttackHeld
            && HardAttackReleased == other.HardAttackReleased
            && HardAttackLongPressed == other.HardAttackLongPressed
            && HardAttackBuffered == other.HardAttackBuffered;
    }

    internal string ToLogLine()
    {
        if (!string.IsNullOrEmpty(Error))
        {
            return $"[自动旋风斩监测] 读取失败：{Error}";
        }

        string weaponText;
        if (!WeaponFound)
        {
            weaponText = "无武器";
        }
        else
        {
            string energyText = ChargeCounterAvailable
                ? $" 蓄力={F(ChargeEnergy)}/{F(MaxChargeEnergy)}"
                : " 蓄力计数器不可用";
            string thresholdText = ChargeThresholdCounterAvailable
                ? $" 一格格满={F(ChargeThresholdLevel1)} 二格满={F(ChargeThresholdLevel2)}"
                    + (ChargeThresholdLevel3 > 0f ? $" 三格满={F(ChargeThresholdLevel3)}" : string.Empty)
                : " 格数阈值不可用";
            string levelText = ChargeLevelCounterAvailable
                ? $" 等级={ChargeLevel}"
                    + (MaxChargeLevel > 0 ? $"/{MaxChargeLevel}" : string.Empty)
                    + $" 目标等级={Plugin.TargetChargeLevel.Value}"
                : " 等级计数器不可用";
            weaponText = $"{WeaponType} 攻速={F(AttackSpeed)} 基础攻速={F(BasicAttackSpeed)}"
                + energyText
                + thresholdText
                + levelText;
        }
        string motionText = MotionFound
            ? $"{MotionName} 类型={MotionType} 状态={MotionStateType} 时间={F(MotionTime)} 帧={MotionFrame} 动作速率={F(MotionSpeedRate)} 播放速率={F(MotionPlaySpeed)} 时间倍率={F(MotionTimeScale)}"
            : "无动作状态";
        string inputText = InputFound
            ? $"锁定={InputLocked} 输入冷却={GameInputCoolDown} J按下={AttackDown} J持续={AttackHeld} J缓冲={AttackBuffered} J按压={F(AttackPressTime)} K按下={DodgeDown} K缓冲={DodgeBuffered} K按压={F(DodgePressTime)} L按下={HardAttackDown} L持续={HardAttackHeld} L长按={HardAttackLongPressed} L松开={HardAttackReleased} L缓冲={HardAttackBuffered} L按压={F(HardAttackPressTime)}"
            : "无输入状态";

        return $"[自动旋风斩监测] 玩家={PlayerFound} 英雄={HeroFound} 武器[{weaponText}] 动作[{motionText}] 输入[{inputText}]";
    }

    private static string F(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
