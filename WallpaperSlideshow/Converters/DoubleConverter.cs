using System;
using System.Globalization;
using System.Windows.Data;

namespace WallpaperSlideshow.Converters;

sealed class DoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not double dValue ? Binding.DoNothing : dValue.ToString(CultureInfo.InvariantCulture);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not string sValue || !double.TryParse(sValue, out var dValue) ? Binding.DoNothing : dValue;
}
