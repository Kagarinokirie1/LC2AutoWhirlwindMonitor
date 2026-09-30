using LC2;
using HunterMotion;
using UnityEngine;

namespace LC2.AutoWhirlwindMonitor;

public sealed class MonitorBehaviour : MonoBehaviour
{
    private static MonitorBehaviour _instance;

    private readonly AutoWhirlwindController _automation = new AutoWhirlwindController();
    private bool _monitoring;
    private float _nextSnapshotTime;
    private RuntimeSnapshot _lastSnapshot;

    private void Awake()
    {
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    internal static void PulseGameInput(CreatureInputCtrl input)
    {
        MonitorBehaviour instance = _instance;
        if (instance == null || !instance._monitoring || !instance._automation.IsRunning)
        {
            return;
        }

        instance._automation.UpdateFromGameInput(input, Time.unscaledTime);
    }

    internal static void PulseMotionComboReady(MotionActor actor, BaseMotionData nextMotion, float now)
    {
        MonitorBehaviour instance = _instance;
        if (instance == null || !instance._monitoring || !instance._automation.IsRunning)
        {
            return;
        }

        instance._automation.NotifyMotionComboReady(actor, nextMotion, now);
    }

    private void Update()
    {
        if (!Plugin.Enabled.Value)
        {
            return;
        }

        if (Input.GetKeyDown(Plugin.ToggleKey.Value))
        {
            _monitoring = !_monitoring;
            _lastSnapshot = null;
            _nextSnapshotTime = 0f;

            if (_monitoring && Plugin.EnableAutomation.Value)
            {
                RuntimeSnapshot toggleSnapshot = GameStateReader.Read(out CreatureInputCtrl toggleInput);
                _automation.Start(toggleSnapshot, toggleInput, Time.unscaledTime);
            }
            else
            {
                _automation.Stop("热键关闭");
            }

            Plugin.Logger.LogInfo(_monitoring
                ? "[自动旋风斩监测] 已开启。"
                : "[自动旋风斩监测] 已关闭。");
        }

        if (!_monitoring)
        {
            return;
        }

        RuntimeSnapshot snapshot = GameStateReader.Read(out CreatureInputCtrl input);
        float now = Time.unscaledTime;

        bool stateChanged = Plugin.LogInputChanges.Value && !snapshot.HasSameInputOrMotionState(_lastSnapshot);
        bool intervalReached = now >= _nextSnapshotTime;

        if (stateChanged || intervalReached)
        {
            Plugin.Logger.LogInfo(snapshot.ToLogLine());
            _lastSnapshot = snapshot;
            float interval = Plugin.SnapshotIntervalSeconds.Value;
            _nextSnapshotTime = now + (interval < 0.1f ? 0.1f : interval);
        }
    }
}
