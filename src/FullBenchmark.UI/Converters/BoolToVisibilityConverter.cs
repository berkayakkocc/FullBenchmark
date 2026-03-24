using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FullBenchmark.UI.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public Visibility TrueValue  { get; set; } = Visibility.Visible;
    public Visibility FalseValue { get; set; } = Visibility.Collapsed;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? TrueValue : FalseValue;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v == TrueValue;
}

/// <summary>Inverse: true → Collapsed, false → Visible.</summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Collapsed;
}
