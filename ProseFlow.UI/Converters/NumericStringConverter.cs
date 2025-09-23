using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace ProseFlow.UI.Converters;

/// <summary>
/// Safely converts between various numeric types (int, float, double, etc.) and their string representation.
/// This converter is essential for binding numeric properties to a TextBox, as it gracefully handles
/// invalid or intermediate user input (e.g., "1.", "-", or non-numeric text) without crashing the application.
/// </summary>
public class NumericStringConverter : IValueConverter
{
    /// <summary>
    /// Converts a numeric value from the ViewModel to a string for display in the UI.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Use a specific format if provided, otherwise default to a general representation.
        var format = parameter as string;
        if (!string.IsNullOrWhiteSpace(format) && value is IFormattable formattable)
            return formattable.ToString(format, CultureInfo.InvariantCulture);

        return value?.ToString();
    }


    /// <summary>
    /// Converts a string from a UI TextBox back to the underlying numeric type of the ViewModel property.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s))
        {
            return AvaloniaProperty.UnsetValue;
        }

        // Attempt to parse the string into the target numeric type.
        return targetType switch
        {
            not null when targetType == typeof(int) => int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture,
                out var intResult)
                ? intResult
                : AvaloniaProperty.UnsetValue,
            not null when targetType == typeof(float) => float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture,
                out var floatResult)
                ? floatResult
                : AvaloniaProperty.UnsetValue,
            not null when targetType == typeof(double) => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture,
                out var doubleResult)
                ? doubleResult
                : AvaloniaProperty.UnsetValue,
            not null when targetType == typeof(decimal) => decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture,
                out var decimalResult)
                ? decimalResult
                : AvaloniaProperty.UnsetValue,
            not null when targetType == typeof(long) => long.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture,
                out var longResult)
                ? longResult
                : AvaloniaProperty.UnsetValue,
            _ => AvaloniaProperty.UnsetValue // Default case if targetType doesn't match any of the above
        };
    }
}