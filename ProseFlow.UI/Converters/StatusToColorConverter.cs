using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ProseFlow.Infrastructure.Services.AiProviders.Local;

namespace ProseFlow.UI.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ModelStatus status)
            return Brushes.Gray;

        return status switch
        {
            ModelStatus.Loaded => Brushes.LimeGreen,
            ModelStatus.Loading => Brushes.Orange,
            ModelStatus.Error => Brushes.Red,
            ModelStatus.Unloaded => Brushes.SlateGray,
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}