using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WallpaperSlideshow.Converters;

class DoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not double dValue ? Binding.DoNothing : dValue.ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not string sValue || !double.TryParse(sValue, out var dValue) ? Binding.DoNothing : dValue;
}
