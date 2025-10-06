using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ProseFlow.UI.ViewModels.Dialogs;

namespace ProseFlow.UI.Converters;

/// <summary>
/// Converts a PasswordStrength enum value to a corresponding color Brush for UI display.
/// </summary>
public class PasswordStrengthToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not PasswordStrength strength)
            return Brushes.Transparent;

        return strength switch
        {
            PasswordStrength.VeryWeak => Brushes.Red,
            PasswordStrength.Weak => Brushes.Orange,
            PasswordStrength.Medium => Brushes.Yellow,
            PasswordStrength.Strong => Brushes.LightGreen,
            PasswordStrength.VeryStrong => Brushes.Green,
            _ => Brushes.Transparent
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}