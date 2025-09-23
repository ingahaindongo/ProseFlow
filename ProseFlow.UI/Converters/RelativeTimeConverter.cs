using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ProseFlow.UI.Converters;

public class RelativeTimeConverter : IValueConverter
{
    private const int Second = 1;
    private const int Minute = 60 * Second;
    private const int Hour = 60 * Minute;
    private const int Day = 24 * Hour;
    private const int Month = 30 * Day;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime dt)
            return string.Empty;

        var ts = new TimeSpan(DateTime.UtcNow.Ticks - dt.Ticks);
        var delta = Math.Abs(ts.TotalSeconds);

        switch (delta)
        {
            case < 1 * Minute:
                return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";
            case < 2 * Minute:
                return "a minute ago";
            case < 45 * Minute:
                return ts.Minutes + " minutes ago";
            case < 90 * Minute:
                return "an hour ago";
            case < 24 * Hour:
                return ts.Hours + " hours ago";
            case < 48 * Hour:
                return "yesterday";
            case < 30 * Day:
                return ts.Days + " days ago";
            case < 12 * Month:
            {
                var months = System.Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            default:
            {
                var years = System.Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year ago" : years + " years ago";
            }
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}