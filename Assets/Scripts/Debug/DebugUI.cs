using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    public static DebugUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI _text;

    private readonly Dictionary<string, Func<string>> _trackedValues = new Dictionary<string, Func<string>>();
    private float _fps;

    private void Awake()
    {
        Instance = this;

        _trackedValues["FPS"] = () => _fps.ToString("F0");

        _trackedValues["Frame Time (ms)"] = () =>
        {
            PerformanceManager pm = PerformanceManager.Instance;
            return pm != null ? pm.CurrentFrameTimeMs.ToString("F2") : "N/A";
        };

        // _trackedValues["Lowest FPS"] = () =>
        // {
        //     PerformanceManager pm = PerformanceManager.Instance;
        //     if (pm == null || pm.LowestFpsSeen == float.MaxValue) return "N/A";

        //     // Show decimals so very low values are not rounded to 0
        //     return pm.LowestFpsSeen.ToString("F2");
        // };

        _trackedValues["CPU >20ms Frames"] = () =>
        {
            PerformanceManager pm = PerformanceManager.Instance;
            return pm != null ? pm.CpuSpikeCount.ToString() : "N/A";
        };

        // Optional but useful for verifying safeguard behavior
        _trackedValues["Perf Mode"] = () =>
        {
            PerformanceManager pm = PerformanceManager.Instance;
            return pm != null ? pm.CurrentMode.ToString() : "N/A";
        };

        _trackedValues["Gaze Interval"] = () =>
        {
            PerformanceManager pm = PerformanceManager.Instance;
            return pm != null ? pm.ActiveGazeInterval.ToString() : "N/A";
        };

        _trackedValues["Trigger Interval (s)"] = () =>
        {
            PerformanceManager pm = PerformanceManager.Instance;
            return pm != null ? pm.ActiveTriggerInterval.ToString("F3") : "N/A";
        };
    }

    public void Track(string label, Func<string> valueGetter)
    {
        _trackedValues[label] = valueGetter;
    }

    private void Update()
    {
        _fps = 1f / Mathf.Max(Time.deltaTime, 0.0001f);

        if (_trackedValues.Count == 0)
        {
            _text.text = "No entries";
            return;
        }

        StringBuilder sb = new StringBuilder(256);
        foreach (var kvp in _trackedValues)
        {
            sb.AppendLine($"{kvp.Key}: {kvp.Value()}");
        }

        _text.text = sb.ToString();
    }
}