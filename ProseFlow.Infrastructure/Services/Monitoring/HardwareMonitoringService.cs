using System.Runtime.InteropServices;
using System.Timers;
using LibreHardwareMonitor.Hardware;
using Microsoft.Extensions.Logging;
using ProseFlow.Application.Events;
using ProseFlow.Core.Models;
using Timer = System.Timers.Timer;

namespace ProseFlow.Infrastructure.Services.Monitoring;


/// <summary>
/// A singleton service that polls for system hardware metrics in the background.
/// </summary>
public class HardwareMonitoringService : IDisposable
{
    private readonly ILogger<HardwareMonitoringService> _logger;
    private readonly Computer _computer;
    private readonly Timer? _timer;
    private readonly bool _isMonitoringAvailable = true;

    private HardwareMetrics _currentMetrics = new();

    // LibreHardwareMonitor sensor names
    private const string CpuTotalSensorName = "CPU Total";
    private const string MemoryUsedSensorName = "Memory Used";
    private const string MemoryAvailableSensorName = "Memory Available";
    private const string GpuCoreSensorName = "GPU Core";
    private const string GpuMemoryTotalSensorName = "GPU Memory Total";
    private const string GpuMemoryUsedSensorName = "D3D Dedicated Memory Used";

    /// <summary>
    /// An event that is raised when the hardware metrics are updated.
    /// This event will not fire if hardware monitoring is unavailable.
    /// </summary>
    public event Action<HardwareMetrics>? MetricsUpdated;
    
    /// <summary>
    /// Gets a value indicating whether hardware monitoring is available.
    /// </summary>
    public bool IsMonitoringAvailable => _isMonitoringAvailable;

    public HardwareMonitoringService(ILogger<HardwareMonitoringService> logger)
    {
        _logger = logger;
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true
        };

        try
        {
            _computer.Open();
            InitializeTotalMetrics();

            _timer = new Timer(2000); // Poll every 2 seconds
            _timer.Elapsed += OnTimerElapsed;
            
            // Subscribe to visibility changes to control the timer.
            AppEvents.MainWindowVisibilityChanged += OnMainWindowVisibilityChanged;
        }
        catch (Exception ex)
        {
            _isMonitoringAvailable = false;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
                RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                _logger.LogWarning("Hardware monitoring service failed to initialize on macOS ARM64 architecture. This is likely due to a missing OSX-ARM64 version of MonoPosixHelper library.");
            else
                _logger.LogWarning(ex, "Hardware monitoring service failed to initialize, likely due to a missing native dependency for this OS/architecture. Hardware metrics will be unavailable.");
        }
    }

    /// <summary>
    /// Enumerates all detectable GPU devices in the system.
    /// </summary>
    /// <returns>A list of GpuDevice objects.</returns>
    public List<GpuDeviceInfo> GetGpuDevices()
    {
        var devices = new List<GpuDeviceInfo>();

        if (!IsMonitoringAvailable) return devices;

        var gpus = _computer.Hardware
            .Where(h => h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel)
            .ToList();

        devices.AddRange(gpus.Select((t, i) => new GpuDeviceInfo
        {
            Index = i,
            Name = t.Name,
            VramGb = Math.Round((t.Sensors.FirstOrDefault(s => s.Name == GpuMemoryTotalSensorName && s.SensorType == SensorType.SmallData)?.Value ?? 0) / 1024.0, 1),
            Type = t.HardwareType switch
            {
                HardwareType.GpuNvidia => "NVIDIA",
                HardwareType.GpuAmd => "AMD",
                HardwareType.GpuIntel => "Intel",
                _ => "Unknown"
            }
        }));

        return devices;
    }

    /// <summary>
    /// Starts or stops the hardware polling based on the main window's visibility.
    /// </summary>
    private void OnMainWindowVisibilityChanged(bool isVisible)
    {
        if (_timer is null) return;

        switch (isVisible)
        {
            case true when !_timer.Enabled:
                _logger.LogInformation("Main window is visible, starting hardware polling.");
                _timer.Start();
                break;
            case false when _timer.Enabled:
                _logger.LogInformation("Main window is hidden, stopping hardware polling to conserve resources.");
                _timer.Stop();
                break;
        }
    }

    /// <summary>
    /// Gets a snapshot of the latest hardware metrics.
    /// Returns an empty metrics object if monitoring is unavailable.
    /// </summary>
    public HardwareMetrics GetCurrentMetrics()
    {
        return !_isMonitoringAvailable ? new HardwareMetrics() : _currentMetrics;
    }

    private void InitializeTotalMetrics()
    {
        double totalRam = 0;
        var memoryHardware = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory);
        if (memoryHardware != null)
        {
            memoryHardware.Update();
            var used = GetSensorValue(memoryHardware, MemoryUsedSensorName, SensorType.Data);
            var available = GetSensorValue(memoryHardware, MemoryAvailableSensorName, SensorType.Data);
            totalRam = used + available;
        }

        
        double maxVramMb = 0;

        // Filter for all GPU hardware (Nvidia, AMD, Intel)
        var gpus = _computer.Hardware.Where(h =>
            h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel);

        foreach (var gpu in gpus)
        {
            gpu.Update();
            var vramSensor = gpu.Sensors.FirstOrDefault(s => s.Name == GpuMemoryTotalSensorName && s.SensorType == SensorType.SmallData);
            
            // If multiple GPUs are present, assume the one with the most memory is the primary/dedicated one.
            if (vramSensor?.Value != null && vramSensor.Value > maxVramMb) maxVramMb = vramSensor.Value.Value;
        }
        
        // Convert the final value from MB to GB
        var totalVramGb = maxVramMb / 1024.0;
        

        _currentMetrics = _currentMetrics with
        {
            RamTotalGb = Math.Round(totalRam, 1),
            VramTotalGb = Math.Round(totalVramGb, 1)
        };
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_isMonitoringAvailable) return;

        try
        {
            double cpuUsage = 0;
            double ramUsed = 0;

            double maxGpuUsage = 0;
            double maxVramUsedMb = 0;

            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();
                switch (hardware.HardwareType)
                {
                    case HardwareType.Cpu:
                        cpuUsage = GetSensorValue(hardware, CpuTotalSensorName, SensorType.Load);
                        break;
                    case HardwareType.Memory:
                        ramUsed = GetSensorValue(hardware, MemoryUsedSensorName, SensorType.Data);
                        break;
                    case HardwareType.GpuNvidia:
                    case HardwareType.GpuAmd:
                    case HardwareType.GpuIntel:
                        var gpuUsage = GetSensorValue(hardware, GpuCoreSensorName, SensorType.Load);
                        if(gpuUsage > maxGpuUsage) maxGpuUsage = gpuUsage;

                        var vramUsedMb = GetSensorValue(hardware, GpuMemoryUsedSensorName, SensorType.SmallData);
                        if(vramUsedMb > maxVramUsedMb) maxVramUsedMb = vramUsedMb;
                        break;
                }
            }

            var ramTotal = _currentMetrics.RamTotalGb;
            var vramTotal = _currentMetrics.VramTotalGb;
            // Convert the final used VRAM value from MB to GB
            var vramUsedGb = maxVramUsedMb / 1024.0; 

            _currentMetrics = _currentMetrics with
            {
                CpuUsagePercent = Math.Round(cpuUsage, 1),
                RamUsedGb = Math.Round(ramUsed, 1),
                RamUsagePercent = ramTotal > 0 ? Math.Round(ramUsed / ramTotal * 100, 1) : 0,
                GpuUsagePercent = Math.Round(maxGpuUsage, 1),
                VramUsedGb = Math.Round(vramUsedGb, 1),
                VramUsagePercent = vramTotal > 0 ? Math.Round(vramUsedGb / vramTotal * 100, 1) : 0
            };

            MetricsUpdated?.Invoke(_currentMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An error occurred during hardware metrics polling.");
        }
    }

    private static float GetSensorValue(IHardware hardware, string sensorName, SensorType sensorType)
    {
        var sensor = hardware.Sensors.FirstOrDefault(s => s.Name == sensorName && s.SensorType == sensorType);
        return sensor?.Value ?? 0f;
    }

    public void Dispose()
    {
        AppEvents.MainWindowVisibilityChanged -= OnMainWindowVisibilityChanged;
        _timer?.Stop();
        _timer?.Dispose();
        if (_isMonitoringAvailable) _computer.Close();
        
        GC.SuppressFinalize(this);
    }
}