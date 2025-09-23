using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ProseFlow.UI.Converters;

public class BoolToOutputTargetConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool openInWindow) return openInWindow ? "New Window" : "In-Place";
        return "Unknown";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string outputTarget && outputTarget.Equals("New Window", StringComparison.OrdinalIgnoreCase);
    }
}