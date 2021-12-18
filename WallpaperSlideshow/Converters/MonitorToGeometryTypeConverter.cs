using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using WallpaperSlideshow.Models;

namespace WallpaperSlideshow.Converters;

class MonitorToGeometryTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not Monitor monitor ? Binding.DoNothing : monitor.Right - monitor.Left > monitor.Bottom - monitor.Top 
            ? $"Horizontal ({monitor.Right - monitor.Left}x{monitor.Bottom - monitor.Top})" 
            : $"Vertical ({monitor.Right - monitor.Left}x{monitor.Bottom - monitor.Top})";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
