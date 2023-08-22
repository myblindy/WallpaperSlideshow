using ReactiveUI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace WallpaperSlideshow.Models;

class Monitor : ReactiveObject
{
    public int Index { get; init; }

    public Screen? Screen => Screen.AllScreens.ElementAtOrDefault(Index);

    bool active;
    public bool Active { get => active; set => this.RaiseAndSetIfChanged(ref active, value); }

    string? path;
    public string? Path { get => path; set => this.RaiseAndSetIfChanged(ref path, value); }

    string? currentWallpaperPath;
    public string? CurrentWallpaperPath { get => currentWallpaperPath; set => this.RaiseAndSetIfChanged(ref currentWallpaperPath, value); }

    public static ObservableCollection<Monitor> AllMonitors { get; } = new();

    static Rectangle? allBounds;
    public static Rectangle? AllBounds { get => allBounds; set { allBounds = value; StaticPropertyChanged?.Invoke(null, new(nameof(AllBounds))); } }

    static double intervalSeconds = 60;
    public static double IntervalSeconds { get => intervalSeconds; set { intervalSeconds = value; StaticPropertyChanged?.Invoke(null, new(nameof(IntervalSeconds))); } }

    public static event PropertyChangedEventHandler? StaticPropertyChanged;
}
