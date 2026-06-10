using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using SKRYBEK.Core.Enums;

namespace SKRYBEK.App.Converters;

[ValueConversion(typeof(bool), typeof(Brush))]
public sealed class SamochodTypToBorderBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var podstawowy = value is true;
        var key = podstawowy ? "SamochodPodstawowyBorderBrush" : "SamochodDodatkowyBorderBrush";
        return Application.Current.TryFindResource(key) as Brush
               ?? Application.Current.FindResource("BorderBrush");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

[ValueConversion(typeof(TypSamochodu), typeof(string))]
public sealed class TypSamochoduToEtykietaConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is TypSamochodu typ ? TypSamochoduEtykiety.Format(typ) : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

[ValueConversion(typeof(int), typeof(Brush))]
public sealed class PozycjaOznaczenieBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value is 1 ? "SamochodOznaczenieDBrush"
            : value is 2 ? "SamochodOznaczenieKBrush"
            : null;
        return key is null
            ? DependencyProperty.UnsetValue
            : Application.Current.FindResource(key);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public static class TypSamochoduEtykiety
{
    public static string Format(TypSamochodu typ) => typ switch
    {
        TypSamochodu.Podstawowy => "P — podstawowy",
        TypSamochodu.Dodatkowy  => "D — dodatkowy",
        _                       => typ.ToString()
    };
}
