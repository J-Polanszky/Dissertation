using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Unity.Services.Authentication;

[Serializable]
public class PerformanceLog
{
    public string playerName;
    public string cpuType;
    public int cpuCores;
    public int ramMB;
    public string gpuType;
    public int gpuMemMB;
    public List<Sample> samples;
}

[Serializable]
public class Sample
{
    public int second;
    public float cpu;
    public float memory;
    public float fps;
}

public class PerformanceLogger : MonoBehaviour
{
    public static PerformanceLogger Instance { get; private set; }
    
    private Process process;
    private TimeSpan lastTotalProcessorTime;
    private float lastSampleTime;
    private bool isLogging;
    private List<float> cpuUsages = new();
    private List<float> memoryUsages = new();
    private List<float> frameRates = new();
    private float logInterval = 5f;
    private Coroutine loggingCoroutine;

    // Hardware info
    private string cpuType;
    private int cpuCores;
    private int ramMB;
    private string gpuType;
    private int gpuMemMB;

    private void Awake()
    {
        if (Instance == null){
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    public void StartLogging()
    {
        process = Process.GetCurrentProcess();
        lastTotalProcessorTime = process.TotalProcessorTime;
        lastSampleTime = Time.realtimeSinceStartup;
        cpuType = SystemInfo.processorType;
        cpuCores = SystemInfo.processorCount;
        ramMB = SystemInfo.systemMemorySize;
        gpuType = SystemInfo.graphicsDeviceName;
        gpuMemMB = SystemInfo.graphicsMemorySize;
        isLogging = true;
        cpuUsages.Clear();
        memoryUsages.Clear();
        frameRates.Clear();
        loggingCoroutine = StartCoroutine(LogPerformance());
    }

    public PerformanceLog EndLogging()
    {
        isLogging = false;
        if (loggingCoroutine != null)
            StopCoroutine(loggingCoroutine);
        return GetPerformanceLog();
    }

    private IEnumerator LogPerformance()
    {
        float nextSampleTime = Time.realtimeSinceStartup;
        while (isLogging)
        {
            float now = Time.realtimeSinceStartup;
            if (now >= nextSampleTime)
            {
                // CPU usage
                TimeSpan currentTotalProcessorTime = process.TotalProcessorTime;
                double cpuUsedMs = (currentTotalProcessorTime - lastTotalProcessorTime).TotalMilliseconds;
                double elapsedMs = (now - lastSampleTime) * 1000.0;
                float cpuUsagePercent = (float)((cpuUsedMs / (elapsedMs * cpuCores)) * 100.0);
                cpuUsages.Add(cpuUsagePercent);
                lastTotalProcessorTime = currentTotalProcessorTime;
                lastSampleTime = now;

                // Memory usage (MB)
                float memMB = process.WorkingSet64 / (1024f * 1024f);
                memoryUsages.Add(memMB);

                // FPS
                float fps = 1.0f / Time.deltaTime;
                frameRates.Add(fps);

                nextSampleTime += logInterval;
            }
            yield return null;
        }
    }

    private PerformanceLog GetPerformanceLog()
    {
        var log = new PerformanceLog
        {
            playerName = AuthenticationService.Instance.PlayerInfo.Username,
            cpuType = cpuType,
            cpuCores = cpuCores,
            ramMB = ramMB,
            gpuType = gpuType,
            gpuMemMB = gpuMemMB,
            samples = BuildSamples()
        };
        return log;
    }

    private List<Sample> BuildSamples()
    {
        var samples = new List<Sample>();
        int count = Mathf.Min(cpuUsages.Count, memoryUsages.Count, frameRates.Count);
        for (int i = 0; i < count; i++)
        {
            samples.Add(new Sample
            {
                second = i * (int)logInterval,
                cpu = cpuUsages[i],
                memory = memoryUsages[i],
                fps = frameRates[i]
            });
        }
        return samples;
    }
}