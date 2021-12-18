using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WallpaperSlideshow.Models;
using WallpaperSlideshow.Services;

namespace WallpaperSlideshow.ViewModels;

class ConfigurationViewModel : ReactiveObject
{
    public ConfigurationViewModel()
    {
        OpenFolderCommand = ReactiveCommand.Create(() =>
        {
            var path = WallpaperService.GetWallpaperPath(SelectedMonitor!.MonitorPath!);
            using (Process.Start(new ProcessStartInfo("explorer", $"/select,\"{path}\"") { UseShellExecute = false })) { }
        }, this.WhenAnyValue(x => x.SelectedMonitor).Select(x => x is not null));

        OpenInBrowserCommand = ReactiveCommand.Create(() =>
        {

        }, this.WhenAnyValue(x => x.SelectedMonitor).Select(x => x is not null));

        AdvanceSlideShowCommand = ReactiveCommand.Create(() => WallpaperService.AdvanceWallpaperSlideShow());
    }

    Monitor? selectedMonitor;
    public Monitor? SelectedMonitor { get => selectedMonitor; set => this.RaiseAndSetIfChanged(ref selectedMonitor, value); }

    public ICommand OpenFolderCommand { get; }
    public ICommand OpenInBrowserCommand { get; }
    public ICommand AdvanceSlideShowCommand { get; }
}
