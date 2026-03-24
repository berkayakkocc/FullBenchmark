using FullBenchmark.Contracts.Domain.Enums;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FullBenchmark.UI.Converters;

/// <summary>Maps a ScoringBadge to a display colour.</summary>
[ValueConversion(typeof(ScoringBadge), typeof(SolidColorBrush))]
public sealed class BadgeToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush Excellent    = new(Color.FromRgb(0x2B, 0xDB, 0x8C));
    private static readonly SolidColorBrush VeryGood     = new(Color.FromRgb(0x4A, 0xB0, 0xFF));
    private static readonly SolidColorBrush Good         = new(Color.FromRgb(0x78, 0x57, 0xFF));
    private static readonly SolidColorBrush Average      = new(Color.FromRgb(0xF5, 0xA6, 0x23));
    private static readonly SolidColorBrush BelowAverage = new(Color.FromRgb(0xFF, 0x4D, 0x6A));
    private static readonly SolidColorBrush Unknown      = new(Color.FromRgb(0x90, 0x90, 0xB8));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is ScoringBadge badge ? badge switch
        {
            ScoringBadge.Outstanding  => Excellent,
            ScoringBadge.Excellent    => Excellent,
            ScoringBadge.VeryGood     => VeryGood,
            ScoringBadge.Good         => Good,
            ScoringBadge.Average      => Average,
            ScoringBadge.BelowAverage => BelowAverage,
            _                         => Unknown
        } : Unknown;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Maps a 0–1000 score to a colour brush (green → blue → purple → orange → red).</summary>
[ValueConversion(typeof(double), typeof(SolidColorBrush))]
public sealed class ScoreToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush Excellent    = new(Color.FromRgb(0x2B, 0xDB, 0x8C));
    private static readonly SolidColorBrush VeryGood     = new(Color.FromRgb(0x4A, 0xB0, 0xFF));
    private static readonly SolidColorBrush Good         = new(Color.FromRgb(0x78, 0x57, 0xFF));
    private static readonly SolidColorBrush Average      = new(Color.FromRgb(0xF5, 0xA6, 0x23));
    private static readonly SolidColorBrush BelowAverage = new(Color.FromRgb(0xFF, 0x4D, 0x6A));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double score) return BelowAverage;
        return score switch
        {
            >= 801 => Excellent,
            >= 601 => VeryGood,
            >= 401 => Good,
            >= 201 => Average,
            _      => BelowAverage
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Formats a double score with one decimal place (e.g. "742.5").</summary>
[ValueConversion(typeof(double), typeof(string))]
public sealed class ScoreFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double d ? d.ToString("F1", CultureInfo.InvariantCulture) : "—";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => double.TryParse(value?.ToString(), out double d) ? d : 0.0;
}

/// <summary>Formats nullable double, showing "—" when null.</summary>
[ValueConversion(typeof(double?), typeof(string))]
public sealed class NullableScoreConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double d ? d.ToString("F1", CultureInfo.InvariantCulture) : "—";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
