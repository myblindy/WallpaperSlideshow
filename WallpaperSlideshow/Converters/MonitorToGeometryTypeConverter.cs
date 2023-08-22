using System;
using System.Globalization;
using System.Windows.Data;
using WallpaperSlideshow.Models;

namespace WallpaperSlideshow.Converters;

class MonitorToGeometryTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not Monitor monitor || monitor.Screen is null ? Binding.DoNothing : monitor.Screen.Bounds!.Width > monitor.Screen.Bounds!.Height
            ? $"Horizontal ({monitor.Screen.Bounds!.Width}x{monitor.Screen.Bounds!.Height})"
            : $"Vertical ({monitor.Screen.Bounds!.Width}x{monitor.Screen.Bounds!.Height})";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
