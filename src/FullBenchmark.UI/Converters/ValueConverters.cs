using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace FullBenchmark.UI.Converters;

/// <summary>Converts bytes to a human-readable string (B / KB / MB / GB).</summary>
[ValueConversion(typeof(long), typeof(string))]
public sealed class BytesToHumanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not long bytes) return "—";
        return bytes switch
        {
            < 1024L                    => $"{bytes} B",
            < 1024L * 1024             => $"{bytes / 1024.0:F1} KB",
            < 1024L * 1024 * 1024      => $"{bytes / 1024.0 / 1024:F1} MB",
            _                          => $"{bytes / 1024.0 / 1024 / 1024:F2} GB"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Converts bytes to megabytes string (e.g. "1024.0 MB").</summary>
[ValueConversion(typeof(long), typeof(string))]
public sealed class BytesToMegabytesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is long l ? $"{l / 1024.0 / 1024:F1} MB" : "—";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Converts bytes to gigabytes string (e.g. "16.0 GB").</summary>
[ValueConversion(typeof(long), typeof(string))]
public sealed class BytesToGigabytesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is long l ? $"{l / 1024.0 / 1024 / 1024:F1} GB" : "—";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Returns Visibility.Collapsed when value is null, Visible otherwise.</summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public sealed class NullToCollapsedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Clamps a 0–1000 score to a 0–100 value for ProgressBar.Value.</summary>
[ValueConversion(typeof(double), typeof(double))]
public sealed class ScoreToProgressConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double d ? Math.Clamp(d / 10.0, 0, 100) : 0.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Formats a DateTimeOffset as a short local date+time string.</summary>
[ValueConversion(typeof(DateTimeOffset), typeof(string))]
public sealed class DateTimeOffsetShortConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is DateTimeOffset dto
            ? dto.LocalDateTime.ToString("MMM d, yyyy  HH:mm", CultureInfo.CurrentCulture)
            : "—";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Formats a TimeSpan as "Xm Ys".</summary>
[ValueConversion(typeof(TimeSpan), typeof(string))]
public sealed class TimeSpanShortConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not TimeSpan ts) return "—";
        if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
        return $"{ts.TotalSeconds:F1}s";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Returns "%" when value is not null, empty string when null (used for GPU % label).</summary>
[ValueConversion(typeof(double?), typeof(string))]
public sealed class NullToPercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double ? "%" : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Multiplies a double by a parameter (for width-binding scaling).</summary>
public sealed class MultiplyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d && double.TryParse(parameter?.ToString(), NumberStyles.Any,
                                                  CultureInfo.InvariantCulture, out double factor))
            return d * factor;
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
