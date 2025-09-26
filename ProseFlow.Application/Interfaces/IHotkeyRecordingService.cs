using ProseFlow.Application.DTOs;

namespace ProseFlow.Application.Interfaces;

/// <summary>
/// Defines a contract for a service that can globally capture a single hotkey combination.
/// This acts as a mediator between the global hook and the UI for configuration purposes.
/// </summary>
public interface IHotkeyRecordingService
{
    /// <summary>
    /// Fired when a valid, complete hotkey combination has been detected while in recording mode.
    /// The event payload is a technology-agnostic DTO.
    /// </summary>
    event Action<HotkeyData> HotkeyDetected;
    
    /// <summary>
    /// Fired when the state of pressed modifiers changes during recording, providing live feedback.
    /// The string payload is a user-friendly representation of the current state (e.g., "Ctrl+Shift...").
    /// </summary>
    event Action<string> RecordingStateUpdated;

    /// <summary>
    /// Gets a value indicating whether the service is currently listening for a single hotkey input.
    /// </summary>
    bool IsRecording { get; }

    /// <summary>
    /// Puts the service into recording mode.
    /// It will listen for the next valid key combination press.
    /// </summary>
    void BeginRecording();

    /// <summary>
    /// Takes the service out of recording mode.
    /// </summary>
    void EndRecording();
}