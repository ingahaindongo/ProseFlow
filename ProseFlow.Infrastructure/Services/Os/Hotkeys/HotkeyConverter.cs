using ProseFlow.Application.DTOs;
using SharpHook.Data;

namespace ProseFlow.Infrastructure.Services.Os.Hotkeys;

/// <summary>
/// Provides utility methods to convert between SharpHook's hotkey data types and user-friendly strings.
/// </summary>
public static class HotkeyConverter
{
    /// <summary>
    /// Checks if a given KeyCode represents a modifier key by explicitly checking against known values.
    /// </summary>
    public static bool IsModifier(KeyCode key) =>
        key is KeyCode.VcLeftControl or KeyCode.VcRightControl or
                 KeyCode.VcLeftShift or KeyCode.VcRightShift or
                 KeyCode.VcLeftAlt or KeyCode.VcRightAlt or
                 KeyCode.VcLeftMeta or KeyCode.VcRightMeta;

    /// <summary>
    /// Converts a SharpHook KeyCode and EventMask into a technology-agnostic HotkeyData DTO.
    /// </summary>
    /// <param name="key">The key code of the main key.</param>
    /// <param name="mask">The event mask representing the modifier keys.</param>
    /// <returns>A HotkeyData DTO representing the combination.</returns>
    public static HotkeyData ToHotkeyData(KeyCode key, EventMask mask)
    {
        var modifiers = new List<string>();
        if (mask.HasFlag(EventMask.LeftCtrl) || mask.HasFlag(EventMask.RightCtrl)) modifiers.Add("Ctrl");
        if (mask.HasFlag(EventMask.LeftShift) || mask.HasFlag(EventMask.RightShift)) modifiers.Add("Shift");
        if (mask.HasFlag(EventMask.LeftAlt) || mask.HasFlag(EventMask.RightAlt)) modifiers.Add("Alt");
        if (mask.HasFlag(EventMask.LeftMeta) || mask.HasFlag(EventMask.RightMeta)) modifiers.Add(OperatingSystem.IsMacOS() ? "Cmd" : "Win");
        
        var finalKeyName = string.Empty;
        
        // The final key is only added if it's a defined key, and it's not a modifier itself.
        if (key != KeyCode.VcUndefined && !IsModifier(key))
        {
            var keyStr = key.ToString();
            finalKeyName = keyStr.StartsWith("Vc") ? keyStr[2..] : keyStr;
        }

        return new HotkeyData(finalKeyName, modifiers);
    }

    /// <summary>
    /// Converts a SharpHook KeyCode and EventMask into a human-readable string (e.g., "Ctrl+Shift+A").
    /// Can also convert a modifier mask alone if KeyCode is VcUndefined.
    /// </summary>
    /// <param name="key">The key code of the main key, or VcUndefined to format modifiers only.</param>
    /// <param name="modifiers">The event mask representing the modifier keys.</param>
    /// <returns>A formatted string representation of the hotkey.</returns>
    public static string ToFriendlyString(KeyCode key, EventMask modifiers)
    {
        var parts = new List<string>();

        // Add modifier strings in a consistent order by checking for specific left/right flags.
        if (modifiers.HasFlag(EventMask.LeftCtrl) || modifiers.HasFlag(EventMask.RightCtrl)) parts.Add("Ctrl");
        if (modifiers.HasFlag(EventMask.LeftShift) || modifiers.HasFlag(EventMask.RightShift)) parts.Add("Shift");
        if (modifiers.HasFlag(EventMask.LeftAlt) || modifiers.HasFlag(EventMask.RightAlt)) parts.Add("Alt");
        if (modifiers.HasFlag(EventMask.LeftMeta) || modifiers.HasFlag(EventMask.RightMeta)) parts.Add(OperatingSystem.IsMacOS() ? "Cmd" : "Win");

        // Add the main key only if it's defined and is not a modifier itself.
        if (key != KeyCode.VcUndefined && !IsModifier(key))
        {
            var keyName = key.ToString();
            if (keyName.StartsWith("Vc")) keyName = keyName[2..];
            parts.Add(keyName);
        }

        return string.Join("+", parts);
    }

    /// <summary>
    /// Parses a user-friendly hotkey string (e.g., "Ctrl+J") into its SharpHook components.
    /// </summary>
    /// <param name="hotkey">The hotkey string to parse.</param>
    /// <returns>A tuple containing the SharpHook KeyCode and EventMask.</returns>
    public static (KeyCode key, EventMask modifiers) FromFriendlyString(string hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey)) return (KeyCode.VcUndefined, EventMask.None);

        var modifiers = EventMask.None;
        var key = KeyCode.VcUndefined;

        var parts = hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            switch (part.ToUpperInvariant())
            {
                case "CTRL":
                    modifiers |= EventMask.Ctrl;
                    break;
                case "SHIFT":
                    modifiers |= EventMask.Shift;
                    break;
                case "ALT":
                    modifiers |= EventMask.Alt;
                    break;
                case "CMD":
                case "WIN":
                case "META":
                    modifiers |= EventMask.Meta;
                    break;
                default:
                    // TryParse with "Vc" prefix
                    if (!Enum.TryParse($"Vc{part}", true, out key)) key = KeyCode.VcUndefined;
                    break;
            }
        }
        return (key, modifiers);
    }
}