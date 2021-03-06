using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
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
        OpenFolderCommand = ReactiveCommand.Create<Monitor>(selectedMonitor =>
        {
            var path = WallpaperService.GetWallpaperPath(selectedMonitor!.MonitorPath!);
            using (Process.Start(new ProcessStartInfo("explorer", $"/select,\"{path}\"") { UseShellExecute = false })) { }
        });

        OpenInBrowserCommand = ReactiveCommand.Create(() =>
        {

        });

        AdvanceSlideShowCommand = ReactiveCommand.Create(() => WallpaperService.AdvanceWallpaperSlideShow());

        SelectMonitorCommand = ReactiveCommand.Create<Monitor>(monitor => SelectedMonitor = monitor);
    }

    Monitor? selectedMonitor;
    public Monitor? SelectedMonitor { get => selectedMonitor; set => this.RaiseAndSetIfChanged(ref selectedMonitor, value); }

    public ICommand OpenFolderCommand { get; }
    public ICommand OpenInBrowserCommand { get; }
    public ICommand AdvanceSlideShowCommand { get; }
    public ICommand SelectMonitorCommand { get; }
}
