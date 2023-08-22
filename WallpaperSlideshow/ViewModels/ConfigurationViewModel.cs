using ReactiveUI;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using WallpaperSlideshow.Models;
using WallpaperSlideshow.Services;

namespace WallpaperSlideshow.ViewModels;

sealed class ConfigurationViewModel : ReactiveObject
{
    public ConfigurationViewModel()
    {
        OpenFolderCommand = ReactiveCommand.Create<Monitor>(selectedMonitor =>
        {
            if (WallpaperService.GetWallpaperPath(selectedMonitor!.Index) is { } path)
                using (Process.Start(new ProcessStartInfo("explorer", $"/select,\"{path}\"") { UseShellExecute = false })) { }
        });

        OpenInBrowserCommand = ReactiveCommand.Create(() =>
        {
            if (WallpaperService.GetWallpaperPath(selectedMonitor!.Index) is { } path)
                using (Process.Start(new ProcessStartInfo(new Uri(path).ToString()) { UseShellExecute = true })) { }
        });

        AdvanceSlideShowCommand = ReactiveCommand.Create(WallpaperService.AdvanceWallpaperSlideShow);

        SelectMonitorCommand = ReactiveCommand.Create<Monitor>(monitor => SelectedMonitor = monitor);
    }

    Monitor? selectedMonitor;
    public Monitor? SelectedMonitor { get => selectedMonitor; set => this.RaiseAndSetIfChanged(ref selectedMonitor, value); }

    public ICommand OpenFolderCommand { get; }
    public ICommand OpenInBrowserCommand { get; }
    public ICommand AdvanceSlideShowCommand { get; }
    public ICommand SelectMonitorCommand { get; }
}
