using ProseFlow.Application.DTOs;
using ProseFlow.Application.Interfaces;
using SharpHook.Data;

namespace ProseFlow.Infrastructure.Services.Os.Hotkeys;

/// <summary>
/// Implements the service that mediates hotkey recording.
/// </summary>
public class HotkeyRecordingService : IHotkeyRecordingService
{
    /// <inheritdoc />
    public event Action<HotkeyData>? HotkeyDetected;
    
    /// <inheritdoc />
    public event Action<string>? RecordingStateUpdated;

    /// <inheritdoc />
    public bool IsRecording { get; private set; }

    /// <inheritdoc />
    public void BeginRecording()
    {
        IsRecording = true;
    }

    /// <inheritdoc />
    public void EndRecording()
    {
        IsRecording = false;
    }

    /// <summary>
    /// To be called by the global hook service when a complete hotkey is captured during recording.
    /// </summary>
    internal void OnHotkeyDetected(KeyCode key, EventMask mask)
    {
        if (!IsRecording) return;
        
        var hotkeyData = HotkeyConverter.ToHotkeyData(key, mask);
        HotkeyDetected?.Invoke(hotkeyData);
    }
    
    /// <summary>
    /// To be called by the global hook service when the modifier key state changes during recording.
    /// </summary>
    internal void OnRecordingStateUpdated(string partialHotkeyText)
    {
        if (!IsRecording) return;
        RecordingStateUpdated?.Invoke(partialHotkeyText);
    }
}