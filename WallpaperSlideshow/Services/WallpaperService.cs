using DynamicData;
using DynamicData.Binding;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
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

        static void updatePaths()
        {
            var paths = new List<string>();
            if (!string.IsNullOrWhiteSpace(Monitor.SourcePath))
                paths.Add(Monitor.SourcePath);
            fileCacheService.Update(paths);
        }

        // bind the timer interval
        setTimer();
        updatePaths();
        Monitor.StaticPropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Monitor.IntervalSeconds))
                setTimer();
            else if (e.PropertyName is nameof(Monitor.SourcePath))
            {
                updatePaths();
                setTimer();
            }
        };
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
        Monitor[]? monitors;
        Models.Rectangle? allScreenBounds;

        UpdateGeometry();

        lock (Monitor.AllMonitors)
        {
            monitors = Monitor.AllMonitors.Where(s => s.Active && s.Screen is not null).Select(s => s.Clone()).ToArray();
            allScreenBounds = Monitor.AllBounds;
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
        SystemParametersInfoW(SPI.SPI_SETDESKWALLPAPER, 1, imgPath, SPIF.None);
        try { tempDirectory?.Delete(true); } catch { }
        tempDirectory = newTempDirectory;

        async Task drawMonitorWallpaper(int monitorIdx)
        {
            if (workImage is not null && Monitor.SourcePath is not null)
                for (int retry = 0; retry < 200; ++retry)
                    if (fileCacheService.GetRandomFilePath(Monitor.SourcePath) is { } wallpaperPath)
                        try
                        {
                            // get the image information
                            float imageAR;
                            using (var stream = File.OpenRead(wallpaperPath))
                            {
                                var bitmapFrame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                                imageAR = (float)bitmapFrame.PixelWidth / bitmapFrame.PixelHeight;
                                if (!((imageAR <= 1 && !Monitor.AllMonitors[monitorIdx].IsHorizontal)
                                    || (imageAR > 1 && Monitor.AllMonitors[monitorIdx].IsHorizontal)))
                                {
                                    continue;
                                }
                            }

                            // load the image and resize it
                            using var image = await Image.LoadAsync(wallpaperPath);
                            wallpaperPaths[monitorIdx] = wallpaperPath;
                            int screenWidth = monitors[monitorIdx].Screen!.Bounds.Width;
                            int screenHeight = monitors[monitorIdx].Screen!.Bounds.Height;
                            var monitorAR = (float)screenWidth / screenHeight;
                            var (newWidth, newHeight) = imageAR < monitorAR ? (screenWidth, 0) : (0, screenHeight);
                            image.Mutate(ctx => ctx.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));

                            // crop it
                            var (offsetX, offsetY) = imageAR < monitorAR ? (0, -(screenHeight - image.Height) / 2) : (-(screenWidth - image.Width) / 2, 0);
                            image.Mutate(ctx => ctx.Crop(new(offsetX, offsetY, screenWidth, screenHeight)));

                            workImage.Mutate(ctx => ctx.DrawImage(image,
                                new SixLabors.ImageSharp.Point(
                                    monitors[monitorIdx].Screen!.Bounds.Left - allScreenBounds.Left, monitors[monitorIdx].Screen!.Bounds.Top - allScreenBounds.Top),
                                1f));

                            break;
                        }
                        catch { }
        }
    }

    [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SystemParametersInfoW(SPI uiAction, uint uiParam, string pvParam, SPIF fWinIni);

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
