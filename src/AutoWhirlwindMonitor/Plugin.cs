using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace LC2.AutoWhirlwindMonitor;

[BepInPlugin(Guid, Name, Version)]
public sealed class Plugin : BasePlugin
{
    public const string Guid = "lc2.autowhirlwind.monitor";
    public const string Name = "LC2 Auto Whirlwind Monitor";
    public const string Version = "0.6.3";

    internal static Plugin Instance { get; private set; }
    internal static ManualLogSource Logger { get; private set; }
    internal static ConfigEntry<bool> Enabled { get; private set; }
    internal static ConfigEntry<KeyCode> ToggleKey { get; private set; }
    internal static ConfigEntry<float> SnapshotIntervalSeconds { get; private set; }
    internal static ConfigEntry<bool> LogInputChanges { get; private set; }
    internal static ConfigEntry<bool> EnableAutomation { get; private set; }
    internal static ConfigEntry<bool> AdaptToAttackSpeed { get; private set; }
    internal static ConfigEntry<bool> UseChargeCounterDetection { get; private set; }
    internal static ConfigEntry<bool> UseComboReadyDetection { get; private set; }
    internal static ConfigEntry<int> TargetChargeLevel { get; private set; }
    internal static ConfigEntry<float> TargetChargeEnergyPercent { get; private set; }
    internal static ConfigEntry<bool> AutoCalibrateChargeHold { get; private set; }
    internal static ConfigEntry<float> InitialChargeHoldSeconds { get; private set; }
    internal static ConfigEntry<float> MeasuredChargeHoldSeconds { get; private set; }
    internal static ConfigEntry<float> MaximumAutoChargeSeconds { get; private set; }
    internal static ConfigEntry<float> ChargeReleaseWaitSeconds { get; private set; }
    internal static ConfigEntry<float> AttackTapHoldSeconds { get; private set; }
    internal static ConfigEntry<float> AttackToDodgeSeconds { get; private set; }
    internal static ConfigEntry<float> DodgeTapHoldSeconds { get; private set; }
    internal static ConfigEntry<float> DodgeToChargeSeconds { get; private set; }
    internal static ConfigEntry<float> ChargeTapHoldSeconds { get; private set; }
    internal static ConfigEntry<float> ChargeToAttackSeconds { get; private set; }
    internal static ConfigEntry<float> MinimumScaledSeconds { get; private set; }
    internal static ConfigEntry<float> MaximumScaledSeconds { get; private set; }

    private GameObject _host;
    private Harmony _harmony;

    public override void Load()
    {
        Instance = this;
        Logger = base.Log;
        Enabled = Config.Bind("1.总开关", "Enabled", true, "自动旋风斩总开关。关闭后 O 键不响应。");
        ToggleKey = Config.Bind("2.热键", "ToggleKey", KeyCode.O, "切换自动循环和监测日志的按键。");
        SnapshotIntervalSeconds = Config.Bind("3.采样", "SnapshotIntervalSeconds", 0.5f, "监测开启时，即使状态未变化也输出的间隔秒数。");
        LogInputChanges = Config.Bind("3.采样", "LogInputChanges", true, "J/K/L 或动作状态发生变化时立即输出一条日志。");
        EnableAutomation = Config.Bind("4.自动循环", "EnableAutomation", true, "是否允许 O 键启动实际输入注入。关闭后 O 只切换监测日志。");
        AdaptToAttackSpeed = Config.Bind("4.自动循环", "AdaptToAttackSpeed", true, "是否根据当前攻速与基础攻速的比例缩放各阶段时长。");
        UseChargeCounterDetection = Config.Bind("4.自动循环", "UseChargeCounterDetection", true, "是否读取游戏蓄力计数器，达到目标蓄力格或兜底条件后立即松开 L。");
        UseComboReadyDetection = Config.Bind("4.自动循环", "UseComboReadyDetection", true, "是否记录游戏连招 Ready 事件用于诊断；0.6.3 起首次 J 改为松开 L 后同帧预输入，不再依赖该事件触发。");
        TargetChargeLevel = Config.Bind("4.自动循环", "TargetChargeLevel", 2, new ConfigDescription("自动松开 L 的目标蓄力格数，默认 2 表示第二格蓄力条刚满时立即释放。", new AcceptableValueRange<int>(1, 3)));
        TargetChargeEnergyPercent = Config.Bind("4.自动循环", "TargetChargeEnergyPercent", 100f, "蓄力格阈值和等级计数器都不可用时使用的兜底能量百分比，100 表示满蓄力。");
        AutoCalibrateChargeHold = Config.Bind("4.自动循环", "AutoCalibrateChargeHold", true, "达到目标蓄力等级后，记录本次耗时并写入 MeasuredChargeHoldSeconds。");
        InitialChargeHoldSeconds = Config.Bind("5.阶段时长", "InitialChargeHoldSeconds", 1.5f, "无法读取蓄力计数器时的首次 L 蓄力兜底时间。");
        MeasuredChargeHoldSeconds = Config.Bind("5.阶段时长", "MeasuredChargeHoldSeconds", 0f, "由插件自动测量并写入的实际蓄力时长；大于 0 时优先作为下一次兜底值。");
        MaximumAutoChargeSeconds = Config.Bind("5.阶段时长", "MaximumAutoChargeSeconds", 4f, "首次长按 L 的最长等待时间；计数器暂不可用时也持续按住到该上限。");
        ChargeReleaseWaitSeconds = Config.Bind("5.阶段时长", "ChargeReleaseWaitSeconds", 0.05f, "旧版首次松开 L 后等待连招窗口的兜底时间；0.6.3 起保留用于兼容旧配置文件，不再参与自动循环。");
        AttackTapHoldSeconds = Config.Bind("5.阶段时长", "AttackTapHoldSeconds", 0.04f, "J 旋风斩的按下保持时间。");
        AttackToDodgeSeconds = Config.Bind("5.阶段时长", "AttackToDodgeSeconds", 0.03f, "J 后到 K 闪避取消的时间。");
        DodgeTapHoldSeconds = Config.Bind("5.阶段时长", "DodgeTapHoldSeconds", 0.05f, "K 闪避的按下保持时间。");
        DodgeToChargeSeconds = Config.Bind("5.阶段时长", "DodgeToChargeSeconds", 0.03f, "K 后回到下一次 L 蓄力的等待时间。");
        ChargeTapHoldSeconds = Config.Bind("5.阶段时长", "ChargeTapHoldSeconds", 0.04f, "循环中 L 普通短按的按下保持时间。");
        ChargeToAttackSeconds = Config.Bind("5.阶段时长", "ChargeToAttackSeconds", 0.03f, "循环中 L 后回到下一次 J 的等待时间。");
        MinimumScaledSeconds = Config.Bind("5.阶段时长", "MinimumScaledSeconds", 0.016f, "攻速缩放后的最短阶段时间。");
        MaximumScaledSeconds = Config.Bind("5.阶段时长", "MaximumScaledSeconds", 1.5f, "攻速缩放后的最长阶段时间。");

        if (!Enabled.Value)
        {
            Logger.LogInfo("[自动旋风斩监测] 已禁用，跳过初始化。");
            return;
        }

        try
        {
            ClassInjector.RegisterTypeInIl2Cpp<MonitorBehaviour>();
        }
        catch (System.Exception ex)
        {
            Logger.LogWarning($"[自动旋风斩监测] RegisterTypeInIl2Cpp 失败，可能已注册：{ex.Message}");
        }

        _harmony = new Harmony(Guid);
        try
        {
            _harmony.PatchAll(typeof(CreatureInputUpdatePatch));
            Logger.LogInfo("[自动旋风斩监测] 已补丁 CreatureInputCtrl.UpdateInput。");
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"[自动旋风斩监测] CreatureInputCtrl.UpdateInput 补丁失败：{ex}");
        }

        try
        {
            _harmony.PatchAll(typeof(MotionComboReadyPatch));
            Logger.LogInfo("[自动旋风斩监测] 已补丁 MotionActor.MotionComboReady。");
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"[自动旋风斩监测] MotionActor.MotionComboReady 补丁失败：{ex}");
        }

        _host = new GameObject("LC2_AutoWhirlwindMonitor_Host");
        Object.DontDestroyOnLoad(_host);
        _host.hideFlags = HideFlags.HideAndDontSave;
        _host.AddComponent<MonitorBehaviour>();

        Logger.LogInfo($"[自动旋风斩监测] 已加载，按 {ToggleKey.Value} 开关自动循环"
            + (EnableAutomation.Value ? "与监测日志。" : "；当前配置仅记录日志。"));
    }
}
