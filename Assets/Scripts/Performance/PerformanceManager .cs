using System;
using UnityEngine;

public enum PerformanceMode
{
    Normal,
    Safeguard
}

public class PerformanceManager : MonoBehaviour
{
    public static PerformanceManager Instance { get; private set; }

    [Header("Targets")]
    [SerializeField] private int targetFps = 72;
    [SerializeField] private float cpuSpikeThresholdMs = 20f;

    [Header("Mode Switching (hysteresis)")]
    [SerializeField] private float enterSafeguardAboveMs = 13.9f;   // 72 FPS budget
    [SerializeField] private float enterSafeguardForSeconds = 1.2f;
    [SerializeField] private float returnNormalBelowMs = 12.8f;     // safer buffer
    [SerializeField] private float returnNormalForSeconds = 2.0f;
    [SerializeField] private float minSecondsBetweenModeSwitches = 1.5f;

    [Header("Optimization Signals")]
    [SerializeField] private int normalGazeInterval = 1;
    [SerializeField] private int safeguardGazeInterval = 2;
    [SerializeField] private float normalTriggerInterval = 0f;
    [SerializeField] private float safeguardTriggerInterval = 0.03f;
    [SerializeField] private float metricWarmupSeconds = 1.5f;
    private float _runtime;

    public PerformanceMode CurrentMode { get; private set; } = PerformanceMode.Normal;

    public float CurrentFps { get; private set; }
    public float CurrentFrameTimeMs { get; private set; }
    public float LowestFpsSeen { get; private set; } = float.MaxValue;
    public int CpuSpikeCount { get; private set; }

    // Other scripts read these to throttle work safely.
    public int ActiveGazeInterval { get; private set; }
    public float ActiveTriggerInterval { get; private set; }

    public event Action<PerformanceMode> ModeChanged;

    private float _badTime;
    private float _goodTime;
    private float _timeSinceLastSwitch;
    private int _frameCounter;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ActiveGazeInterval = normalGazeInterval;
        ActiveTriggerInterval = normalTriggerInterval;
    }

    private void Start()
    {
        Application.targetFrameRate = targetFps;
        QualitySettings.vSyncCount = 0;
        Time.fixedDeltaTime = 1f / targetFps;
    }

    private void Update()
    {
        _runtime += Time.unscaledDeltaTime;
        _timeSinceLastSwitch += Time.unscaledDeltaTime;

        SampleFrameStats();
        EvaluateMode();
    }

    private void SampleFrameStats()
    {
        _frameCounter++;

        float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        CurrentFps = 1f / dt;
        CurrentFrameTimeMs = dt * 1000f;

        // Ignore startup jitter so metrics are meaningful
        if (_runtime < metricWarmupSeconds)
            return;

        // Ignore pathological startup/editor hitch frames that skew minimum FPS.
        if (CurrentFps < 0.5f)
            return;

        if (LowestFpsSeen == float.MaxValue)
            LowestFpsSeen = CurrentFps;
        else if (CurrentFps < LowestFpsSeen)
            LowestFpsSeen = CurrentFps;

        if (CurrentFrameTimeMs > cpuSpikeThresholdMs)
            CpuSpikeCount++;
    }

    private void EvaluateMode()
    {
        if (CurrentMode == PerformanceMode.Normal)
        {
            if (CurrentFrameTimeMs > enterSafeguardAboveMs)
            {
                _badTime += Time.unscaledDeltaTime;
            }
            else
            {
                _badTime = 0f;
            }

            if (_badTime >= enterSafeguardForSeconds && _timeSinceLastSwitch >= minSecondsBetweenModeSwitches)
            {
                SetMode(PerformanceMode.Safeguard);
            }
        }
        else
        {
            if (CurrentFrameTimeMs < returnNormalBelowMs)
            {
                _goodTime += Time.unscaledDeltaTime;
            }
            else
            {
                _goodTime = 0f;
            }

            if (_goodTime >= returnNormalForSeconds && _timeSinceLastSwitch >= minSecondsBetweenModeSwitches)
            {
                SetMode(PerformanceMode.Normal);
            }
        }
    }

    private void SetMode(PerformanceMode mode)
    {
        if (CurrentMode == mode) return;

        CurrentMode = mode;
        _timeSinceLastSwitch = 0f;
        _badTime = 0f;
        _goodTime = 0f;

        if (mode == PerformanceMode.Safeguard)
        {
            ActiveGazeInterval = safeguardGazeInterval;
            ActiveTriggerInterval = safeguardTriggerInterval;
        }
        else
        {
            ActiveGazeInterval = normalGazeInterval;
            ActiveTriggerInterval = normalTriggerInterval;
        }

        Debug.Log("[PerformanceManager] Mode switched to: " + mode);
        ModeChanged?.Invoke(mode);
    }

    // Helper for gaze scripts: run expensive gaze only every N frames.
    public bool ShouldRunGazeThisFrame()
    {
        int interval = Mathf.Max(1, ActiveGazeInterval);
        return _frameCounter % interval == 0;
    }

    // Helper for trigger/fixed-update scripts.
    public bool ShouldRunThrottled(ref float accumulator)
    {
        float interval = Mathf.Max(0f, ActiveTriggerInterval);
        if (interval <= 0f) return true;

        accumulator += Time.deltaTime;
        if (accumulator >= interval)
        {
            accumulator = 0f;
            return true;
        }

        return false;
    }

    [ContextMenu("Reset Runtime Metrics")]
    public void ResetMetrics()
    {
        LowestFpsSeen = float.MaxValue;
        CpuSpikeCount = 0;
    }
}