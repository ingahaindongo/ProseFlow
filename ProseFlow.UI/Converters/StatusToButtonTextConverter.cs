using System;
using System.Globalization;
using Avalonia.Data.Converters;
using ProseFlow.Infrastructure.Services.AiProviders.Local;

namespace ProseFlow.UI.Converters;

public class StatusToButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ModelStatus status)
            return "Load Model";

        return status switch
        {
            ModelStatus.Loaded => "Unload Model",
            ModelStatus.Loading => "Loading...",
            _ => "Load Model"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}