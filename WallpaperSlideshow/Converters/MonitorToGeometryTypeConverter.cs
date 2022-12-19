using System;
using System.Globalization;
using System.Windows.Data;
using WallpaperSlideshow.Models;

namespace WallpaperSlideshow.Converters;

class MonitorToGeometryTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not Monitor monitor || monitor.Bounds is null ? Binding.DoNothing : monitor.Bounds!.Width > monitor.Bounds!.Height
            ? $"Horizontal ({monitor.Bounds!.Width}x{monitor.Bounds!.Height})"
            : $"Vertical ({monitor.Bounds!.Width}x{monitor.Bounds!.Height})";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
