using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ProseFlow.UI.Converters;

/// <summary>
/// Converts a file size in bytes (long) to a human-readable string format (e.g., "KB", "MB", "GB").
/// </summary>
public class FileSizeConverter : IValueConverter
{
    private static readonly string[] SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not (long byteCount and > 0))
            return "0 B";
        

        var mag = (int)Math.Log(byteCount, 1024);
        var adjustedSize = (decimal)byteCount / (1L << (mag * 10));

        // Adjust format based on the magnitude
        var format = mag < 2 ? "N0" : "N1"; // No decimals for B/KB, one decimal for MB and up
        
        return $"{adjustedSize.ToString(format, CultureInfo.InvariantCulture)} {SizeSuffixes[mag]}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}