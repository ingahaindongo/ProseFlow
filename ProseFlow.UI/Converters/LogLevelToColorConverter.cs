using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ProseFlow.Core.Enums;

namespace ProseFlow.UI.Converters;

public class LogLevelToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not LogLevel level)
            return Brushes.Gray;

        return level switch
        {
            LogLevel.Warning => Brushes.Orange,
            LogLevel.Error => Brushes.Red,
            _ => Brushes.Green // For Info and Debug, etc.
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}