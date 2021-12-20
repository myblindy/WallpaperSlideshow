using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using WallpaperSlideshow.Models;
using WallpaperSlideshow.Support;

namespace WallpaperSlideshow.Services;

class WallpaperService
{
    static readonly IDesktopWallpaper desktopWallpaper = (IDesktopWallpaper)new DesktopWallpaper();
    static readonly FileCacheService fileCacheService = new();
    static readonly DispatcherTimer timer = new();
    static WallpaperService()
    {
        timer.Tick += (s, e) => AdvanceWallpaperSlideShow();

        static void setTimer()
        {
            timer.Interval = TimeSpan.FromSeconds(Monitor.IntervalSeconds);
            if (Monitor.IntervalSeconds > 0)
                timer.Start();
            else
                timer.Stop();
        }

        // bind the timer interval
        setTimer();
        Monitor.StaticPropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Monitor.IntervalSeconds))
                setTimer();
        };

        // bind the file cache
        Monitor.AllMonitors.ToObservableChangeSet().AutoRefresh().Subscribe(_ => fileCacheService.Update(Monitor.AllMonitors.Select(w => w.Path)));
    }

    public static void UpdateGeometry(ObservableCollection<Monitor> monitors)
    {
        var count = desktopWallpaper.GetMonitorDevicePathCount();
        for (var idx = (int)count; idx < monitors.Count; ++idx)
            monitors[idx].Active = false;
        while (count > monitors.Count)
            monitors.Add(new() { Index = monitors.Count });

        // update the paths
        for (var idx = 0; idx < monitors.Count; idx++)
        {
            monitors[idx].Active = true;
            monitors[idx].MonitorPath = desktopWallpaper.GetMonitorDevicePathAt((uint)idx);

            try
            {
                var rect = desktopWallpaper.GetMonitorRECT(monitors[idx].MonitorPath!);
                (monitors[idx].Left, monitors[idx].Right, monitors[idx].Top, monitors[idx].Bottom) =
                    (rect.Left, rect.Right, rect.Top, rect.Bottom);
            }
            catch { }
        }
    }

    public static string GetWallpaperPath(string monitorPath) => desktopWallpaper.GetWallpaper(monitorPath);

    public static void AdvanceWallpaperSlideShow()
    {
        (string monitorPath, string? path)[] monitors;
        lock (Monitor.AllMonitors)
            monitors = Monitor.AllMonitors.Select(s => (s.MonitorPath!, s.Path)).ToArray();

        foreach (var (monitorPath, path) in monitors)
            if (!string.IsNullOrWhiteSpace(path))
                for (int retry = 0; retry < 100; ++retry)
                    if (fileCacheService.GetRandomFilePath(path) is { } wallpaperPath)
                    {
                        desktopWallpaper.SetPosition(DesktopWallpaperPosition.Fill);

                        try
                        {
                            // this can fail while monitors are offline
                            desktopWallpaper.SetWallpaper(monitorPath, wallpaperPath);
                        }
                        catch { }
                        break;
                    }
    }

    [ComImport]
    [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDesktopWallpaper
    {
        void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID);

        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetMonitorDevicePathAt(uint monitorIndex);

        [return: MarshalAs(UnmanagedType.U4)]
        uint GetMonitorDevicePathCount();

        [return: MarshalAs(UnmanagedType.Struct)]
        Rect GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID);

        void SetBackgroundColor([MarshalAs(UnmanagedType.U4)] uint color);

        [return: MarshalAs(UnmanagedType.U4)]
        uint GetBackgroundColor();

        void SetPosition([MarshalAs(UnmanagedType.I4)] DesktopWallpaperPosition position);

        [return: MarshalAs(UnmanagedType.I4)]
        DesktopWallpaperPosition GetPosition();

        void SetSlideshow(IntPtr items);

        IntPtr GetSlideshow();

        void SetSlideshowOptions(DesktopSlideshowDirection options, uint slideshowTick);

        void GetSlideshowOptions(out DesktopSlideshowDirection options, out uint slideshowTick);

        void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.I4)] DesktopSlideshowDirection direction);

        DesktopSlideshowDirection GetStatus();

        bool Enable();
    }

    [ComImport]
    [Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
    class DesktopWallpaper
    {
    }

    [Flags]
    enum DesktopSlideshowOptions
    {
        None = 0,
        ShuffleImages = 0x01
    }

    [Flags]
    enum DesktopSlideshowState
    {
        None = 0,
        Enabled = 0x01,
        Slideshow = 0x02,
        DisabledByRemoteSession = 0x04
    }

    enum DesktopSlideshowDirection
    {
        Forward = 0,
        Backward = 1
    }

    enum DesktopWallpaperPosition
    {
        Center = 0,
        Tile = 1,
        Stretch = 2,
        Fit = 3,
        Fill = 4,
        Span = 5,
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Rect
    {
        public readonly int Left;
        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;
    }
}
