using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WallpaperSlideshow.Models;

class Monitor : ReactiveObject
{
    public int Index { get; init; }

    string? monitorPath;
    public string? MonitorPath { get => monitorPath; set => this.RaiseAndSetIfChanged(ref monitorPath, value); }

    int left, top, right, bottom;
    public int Left { get => left; set => this.RaiseAndSetIfChanged(ref left, value); }
    public int Top { get => top; set => this.RaiseAndSetIfChanged(ref top, value); }
    public int Right { get => right; set => this.RaiseAndSetIfChanged(ref right, value); }
    public int Bottom { get => bottom; set => this.RaiseAndSetIfChanged(ref bottom, value); }

    bool active;
    public bool Active { get => active; set => this.RaiseAndSetIfChanged(ref active, value); }

    string? path;
    public string? Path { get => path; set => this.RaiseAndSetIfChanged(ref path, value); }

    public static ObservableCollection<Monitor> AllMonitors { get; } = new();

    public static event PropertyChangedEventHandler? StaticPropertyChanged;

    static double intervalSeconds = 300;
    public static double IntervalSeconds { get => intervalSeconds; set { intervalSeconds = value; StaticPropertyChanged?.Invoke(null, new(nameof(IntervalSeconds))); } }
}
