using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WallpaperSlideshow.Services;
using WallpaperSlideshow.Views;

namespace WallpaperSlideshow.ViewModels;

class NotifyIconViewModel : ReactiveObject
{
    public NotifyIconViewModel()
    {
        AdvanceSlideShowCommand = ReactiveCommand.Create(() => WallpaperService.AdvanceWallpaperSlideShow());
        ShowConfigurationCommand = ReactiveCommand.Create(() => new ConfigurationView().Show());
        ExitCommand = ReactiveCommand.Create(() => Application.Current.Shutdown());
    }


    public ICommand AdvanceSlideShowCommand { get; }
    public ICommand ShowConfigurationCommand { get; }
    public ICommand ExitCommand { get; }
}
