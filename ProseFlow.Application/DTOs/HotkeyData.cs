namespace ProseFlow.Application.DTOs;

/// <summary>
/// A technology-agnostic Data Transfer Object for representing a captured hotkey combination.
/// </summary>
/// <param name="Key">The primary, non-modifier key that was pressed (e.g., "A", "J", "F5").</param>
/// <param name="Modifiers">A list of modifier keys that were held down (e.g., ["Ctrl", "Shift"]).</param>
public record HotkeyData(string Key, IReadOnlyList<string> Modifiers);