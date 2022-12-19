using System.Windows;
using WallpaperSlideshow.Support;

namespace WallpaperSlideshow.Views;

public partial class ConfigurationView : Window
{
    public ConfigurationView()
    {
        InitializeComponent();
        this.DisableMinimizeMaximizeButtons(false, true);
    }
}
