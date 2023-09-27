using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace WallpaperSlideshow.Models;

sealed class Monitor : ReactiveObject
{
    public int Index { get; init; }

    public Screen? Screen => Screen.AllScreens.ElementAtOrDefault(Index);

    bool active;
    public bool Active { get => active; set => this.RaiseAndSetIfChanged(ref active, value); }

    string? currentWallpaperPath;
    public string? CurrentWallpaperPath { get => currentWallpaperPath; set => this.RaiseAndSetIfChanged(ref currentWallpaperPath, value); }

    public bool IsHorizontal => Screen is null ? true : Screen.Bounds.Width > Screen.Bounds.Height;

    public static ObservableCollection<Monitor> AllMonitors { get; } = new();

    static Rectangle? allBounds;
    public static Rectangle? AllBounds { get => allBounds; set { allBounds = value; StaticPropertyChanged?.Invoke(null, new(nameof(AllBounds))); } }

    static double intervalSeconds = 60;
    public static double IntervalSeconds { get => intervalSeconds; set { intervalSeconds = value; StaticPropertyChanged?.Invoke(null, new(nameof(IntervalSeconds))); } }

    static string? pathHorizontal, pathVertical;
    public static string? PathHorizontal { get => pathHorizontal; set { pathHorizontal = value; StaticPropertyChanged?.Invoke(null, new(nameof(PathHorizontal))); } }
    public static string? PathVertical { get => pathVertical; set { pathVertical = value; StaticPropertyChanged?.Invoke(null, new(nameof(PathVertical))); } }

    public static event PropertyChangedEventHandler? StaticPropertyChanged;

    public Monitor Clone() => new()
    {
        Index = Index,
        Active = Active,
        CurrentWallpaperPath = CurrentWallpaperPath,
    };
}
