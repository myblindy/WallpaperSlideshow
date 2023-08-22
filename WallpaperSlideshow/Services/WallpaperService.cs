using DynamicData;
using DynamicData.Binding;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using WallpaperSlideshow.Models;

namespace WallpaperSlideshow.Services;

sealed partial class WallpaperService
{
    static readonly FileCacheService fileCacheService = new();
    static readonly DispatcherTimer timer = new();

    static WallpaperService()
    {
        timer.Tick += (s, e) => _ = AdvanceWallpaperSlideShow();

        static void setTimer()
        {
            timer.Stop();
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
        Monitor.AllMonitors.ToObservableChangeSet().AutoRefresh().Subscribe(_ =>
        {
            fileCacheService.Update(Monitor.AllMonitors.Select(w => w.Path));
            setTimer();
        });
    }

    public static void UpdateGeometry()
    {
        var count = Screen.AllScreens.Length;

        lock (Monitor.AllMonitors)
        {
            for (var idx = count; idx < Monitor.AllMonitors.Count; ++idx)
                Monitor.AllMonitors[idx].Active = false;

            while (count > Monitor.AllMonitors.Count)
                Monitor.AllMonitors.Add(new() { Index = Monitor.AllMonitors.Count });

            // activate the real monitors
            for (var idx = 0; idx < count; idx++)
                Monitor.AllMonitors[idx].Active = true;

            var left = Monitor.AllMonitors.Where(w => w.Screen is not null).Min(w => w.Screen!.Bounds.Left);
            int top = Monitor.AllMonitors.Where(w => w.Screen is not null).Min(w => w.Screen!.Bounds.Top);
            Monitor.AllBounds = new()
            {
                Left = left,
                Top = top,
                Width = Monitor.AllMonitors.Where(w => w.Screen is not null).Max(w => w.Screen!.Bounds.Left + w.Screen!.Bounds.Width) - left,
                Height = Monitor.AllMonitors.Where(w => w.Screen is not null).Max(w => w.Screen!.Bounds.Top + w.Screen!.Bounds.Height) - top
            };
        }
    }

    public static string? GetWallpaperPath(int index) => Monitor.AllMonitors.ElementAtOrDefault(index)?.CurrentWallpaperPath;

    static Image? workImage;
    static DirectoryInfo? tempDirectory;
    public static async Task AdvanceWallpaperSlideShow()
    {
        (int index, string? path)[] monitors;
        Models.Rectangle? allScreenBounds;
        Monitor[]? screenBounds;

        UpdateGeometry();

        lock (Monitor.AllMonitors)
        {
            monitors = Monitor.AllMonitors.Where(s => s.Active && !string.IsNullOrWhiteSpace(s.Path) && s.Screen is not null).Select(s => (s.Index, s.Path)).ToArray();
            allScreenBounds = Monitor.AllBounds;
            screenBounds = Monitor.AllMonitors.Where(m => m.Active && m.Screen is not null).ToArray();
        }

        if (allScreenBounds is null) return;

        var wallpaperPaths = new string?[monitors.Length];

        if (workImage is null || workImage?.Width != allScreenBounds.Width || workImage?.Height != allScreenBounds.Height)
        {
            workImage?.Dispose();
            workImage = new Image<Rgb24>(allScreenBounds.Width, allScreenBounds.Height);
        }
        else
            workImage.Mutate(ctx => ctx.Clear(Color.Black));

        // draw all images
        await Task.WhenAll(Enumerable.Range(0, monitors.Length)
            .Select(async monitorIdx =>
                await Task.Run(() => drawMonitorWallpaper(monitorIdx))));

        // update the main state
        await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            lock (Monitor.AllMonitors)
                for (int idx = 0; idx < wallpaperPaths.Length && idx < Monitor.AllMonitors.Count; ++idx)
                    if (wallpaperPaths[idx] is { } currentWallpaperPath)
                        Monitor.AllMonitors[idx].CurrentWallpaperPath = currentWallpaperPath;
        });

        // set the wallpaper
        var newTempDirectory = Directory.CreateTempSubdirectory();
        string imgPath = Path.Combine(newTempDirectory.FullName, @"img.jpg");
        await workImage.SaveAsJpegAsync(imgPath, new JpegEncoder { Quality = 96 });
        SystemParametersInfo(SPI.SPI_SETDESKWALLPAPER, 1, imgPath, SPIF.None);
        try { tempDirectory?.Delete(true); } catch { }
        tempDirectory = newTempDirectory;

        async Task drawMonitorWallpaper(int monitorIdx)
        {
            if (workImage is not null && !string.IsNullOrWhiteSpace(monitors[monitorIdx].path))
                for (int retry = 0; retry < 100; ++retry)
                    if (fileCacheService.GetRandomFilePath(monitors[monitorIdx].path!) is { } wallpaperPath)
                    {
                        try
                        {
                            wallpaperPaths[monitorIdx] = wallpaperPath;

                            // load the image and resize it
                            var image = await Image.LoadAsync(wallpaperPath);
                            var imageAR = (float)image.Width / image.Height;
                            int screenWidth = screenBounds[monitorIdx].Screen!.Bounds.Width;
                            int screenHeight = screenBounds[monitorIdx].Screen!.Bounds.Height;
                            var monitorAR = (float)screenWidth / screenHeight;
                            var (newWidth, newHeight) = imageAR < monitorAR ? (screenWidth, 0) : (0, screenHeight);
                            image.Mutate(ctx => ctx.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));

                            // crop it
                            var (offsetX, offsetY) = imageAR < monitorAR ? (0, -(screenHeight - image.Height) / 2) : (-(screenWidth - image.Width) / 2, 0);
                            image.Mutate(ctx => ctx.Crop(new(offsetX, offsetY, screenWidth, screenHeight)));

                            workImage.Mutate(ctx => ctx.DrawImage(image,
                                new SixLabors.ImageSharp.Point(
                                    screenBounds[monitorIdx].Screen!.Bounds.Left - allScreenBounds.Left, screenBounds[monitorIdx].Screen!.Bounds.Top - allScreenBounds.Top),
                                1f));
                        }
                        catch { }
                        break;
                    }
        }
    }

    [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SystemParametersInfo(SPI uiAction, uint uiParam, string pvParam, SPIF fWinIni);

#pragma warning disable CA1712 // Do not prefix enum values with type name
    [Flags]
    enum SPIF
    {
        None = 0x00,
        /// <summary>Writes the new system-wide parameter setting to the user profile.</summary>
        SPIF_UPDATEINIFILE = 0x01,
        /// <summary>Broadcasts the WM_SETTINGCHANGE message after updating the user profile.</summary>
        SPIF_SENDCHANGE = 0x02,
        /// <summary>Same as SPIF_SENDCHANGE.</summary>
#pragma warning disable CA1069 // Enums values should not be duplicated
        SPIF_SENDWININICHANGE = 0x02
#pragma warning restore CA1069 // Enums values should not be duplicated
    }

    enum SPI : uint
    {
        /// <summary>
        /// Sets the desktop wallpaper. The value of the pvParam parameter determines the new wallpaper. To specify a wallpaper bitmap,
        /// set pvParam to point to a null-terminated string containing the name of a bitmap file. Setting pvParam to "" removes the wallpaper.
        /// Setting pvParam to SETWALLPAPER_DEFAULT or null reverts to the default wallpaper.
        /// </summary>
        SPI_SETDESKWALLPAPER = 0x0014,
    }
#pragma warning restore CA1712 // Do not prefix enum values with type name
}
