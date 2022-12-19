using ReactiveUI;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace WallpaperSlideshow.Models;

class Monitor : ReactiveObject
{
    public int Index { get; init; }

    string? monitorPath;
    public string? MonitorPath { get => monitorPath; set => this.RaiseAndSetIfChanged(ref monitorPath, value); }

    Rectangle? bounds;
    public Rectangle? Bounds { get => bounds; set => this.RaiseAndSetIfChanged(ref bounds, value); }

    bool active;
    public bool Active { get => active; set => this.RaiseAndSetIfChanged(ref active, value); }

    string? path;
    public string? Path { get => path; set => this.RaiseAndSetIfChanged(ref path, value); }

    string? currentWallpaperPath;
    public string? CurrentWallpaperPath { get => currentWallpaperPath; set => this.RaiseAndSetIfChanged(ref currentWallpaperPath, value); }

    public static ObservableCollection<Monitor> AllMonitors { get; } = new();

    static Rectangle? allBounds;
    public static Rectangle? AllBounds { get => allBounds; set { allBounds = value; StaticPropertyChanged?.Invoke(null, new(nameof(AllBounds))); } }

    static double intervalSeconds = 300;
    public static double IntervalSeconds { get => intervalSeconds; set { intervalSeconds = value; StaticPropertyChanged?.Invoke(null, new(nameof(IntervalSeconds))); } }

    public static event PropertyChangedEventHandler? StaticPropertyChanged;
}
