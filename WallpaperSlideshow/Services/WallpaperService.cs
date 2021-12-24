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
using System.Windows;
using System.Windows.Threading;
using WallpaperSlideshow.Models;
using WallpaperSlideshow.Support;

namespace WallpaperSlideshow.Services;

class WallpaperService
{
    static IDesktopWallpaper desktopWallpaper = (IDesktopWallpaper)new DesktopWallpaper();
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

    public static void UpdateGeometry()
    {
        var count = desktopWallpaper.GetMonitorDevicePathCount();

        lock (Monitor.AllMonitors)
        {
            for (var idx = (int)count; idx < Monitor.AllMonitors.Count; ++idx)
                Monitor.AllMonitors[idx].Active = false;
            while (count > Monitor.AllMonitors.Count)
                Monitor.AllMonitors.Add(new() { Index = Monitor.AllMonitors.Count });

            // update the paths
            for (var idx = 0; idx < count; idx++)
            {
                Monitor.AllMonitors[idx].Active = true;
                Monitor.AllMonitors[idx].MonitorPath = desktopWallpaper.GetMonitorDevicePathAt((uint)idx);

                try
                {
                    var rect = desktopWallpaper.GetMonitorRECT(Monitor.AllMonitors[idx].MonitorPath!);
                    Monitor.AllMonitors[idx].Bounds = new()
                    {
                        Left = rect.Left,
                        Top = rect.Top,
                        Width = rect.Right - rect.Left,
                        Height = rect.Bottom - rect.Top
                    };
                }
                catch { }
            }

            var left = Monitor.AllMonitors.Where(w => w.Bounds is not null).Min(w => w.Bounds!.Left);
            int top = Monitor.AllMonitors.Where(w => w.Bounds is not null).Min(w => w.Bounds!.Top);
            Monitor.AllBounds = new()
            {
                Left = left,
                Top = top,
                Width = Monitor.AllMonitors.Where(w => w.Bounds is not null).Max(w => w.Bounds!.Left + w.Bounds!.Width) - left,
                Height = Monitor.AllMonitors.Where(w => w.Bounds is not null).Max(w => w.Bounds!.Top + w.Bounds!.Height) - top
            };
        }
    }

    public static string GetWallpaperPath(string monitorPath) => desktopWallpaper.GetWallpaper(monitorPath);

    public static async Task AdvanceWallpaperSlideShow()
    {
        (string monitorPath, string? path)[] monitors;

        UpdateGeometry();
        lock (Monitor.AllMonitors)
            monitors = Monitor.AllMonitors.Where(s => s.Active).Select(s => (s.MonitorPath!, s.Path)).ToArray();

        var wallpaperPaths = new string?[monitors.Length];

        int monitorIdx = 0;
        foreach (var (monitorPath, path) in monitors)
        {
            if (!string.IsNullOrWhiteSpace(path))
                for (int retry = 0; retry < 100; ++retry)
                    if (fileCacheService.GetRandomFilePath(path) is { } wallpaperPath)
                    {
                        try
                        {
                            // this can fail while monitors are offline or when the RPC server is unavailable
                            desktopWallpaper.SetPosition(DesktopWallpaperPosition.Fill);
                            desktopWallpaper.SetWallpaper(monitorPath, wallpaperPath);
                            wallpaperPaths[monitorIdx] = wallpaperPath;
                        }
                        catch (COMException ex) when ((uint)ex.ErrorCode == 0x800706BA)
                        {
                            // RPC server unavailable, recreate the object
                            try { desktopWallpaper = (IDesktopWallpaper)new DesktopWallpaper(); } catch { }
                            continue;
                        }
                        catch { }
                        break;
                    }
            ++monitorIdx;
        }

        await Application.Current.Dispatcher.BeginInvoke(() =>
        {
            lock (Monitor.AllMonitors)
                for (int idx = 0; idx < wallpaperPaths.Length && idx < Monitor.AllMonitors.Count; ++idx)
                    if (wallpaperPaths[idx] is { } currentWallpaperPath)
                        Monitor.AllMonitors[idx].CurrentWallpaperPath = currentWallpaperPath;
        });
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
