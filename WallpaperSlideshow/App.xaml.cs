using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using WallpaperSlideshow.Models;
using WallpaperSlideshow.Services;

namespace WallpaperSlideshow;

public partial class App : Application
{
    TaskbarIcon? notifyIcon;

    const string configurationFileName = "config.json";

    class MonitorConfiguration
    {
        public int Index { get; set; }
        public List<string> Paths { get; set; } = null!;
    }

    class Configuration
    {
        public List<MonitorConfiguration> Monitors { get; set; } = null!;
        public double IntervalSeconds { get; set; }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");

        // load stored settings
        var store = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null);
        if (store.FileExists(configurationFileName))
        {
            using var stream = store.OpenFile(configurationFileName, FileMode.Open);
            if (JsonSerializer.Deserialize<Configuration>(stream) is { } configuration)
            {
                Monitor.IntervalSeconds = configuration.IntervalSeconds;
                foreach (var monitor in configuration.Monitors)
                {
                    while (Monitor.AllMonitors.Count - 1 < monitor.Index)
                        Monitor.AllMonitors.Add(new() { Index = Monitor.AllMonitors.Count });
                    Monitor.AllMonitors[^1].Path = monitor.Paths.FirstOrDefault();
                }
            }
        }

        DisplaySettingsChanged(null, EventArgs.Empty);
        SystemEvents.DisplaySettingsChanged += DisplaySettingsChanged;
    }

    private void DisplaySettingsChanged(object? sender, EventArgs e)
    {
        WallpaperService.UpdateGeometry(Monitor.AllMonitors);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SaveSettings();
        SystemEvents.DisplaySettingsChanged -= DisplaySettingsChanged;
        notifyIcon?.Dispose();
        base.OnExit(e);
    }

    private static void SaveSettings()
    {
        var store = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null);
        using var stream = store.CreateFile(configurationFileName);
        JsonSerializer.Serialize(stream, new Configuration
        {
            Monitors = Monitor.AllMonitors.Select(w => new MonitorConfiguration
            {
                Index = w.Index,
                Paths = string.IsNullOrWhiteSpace(w.Path) ? new() : new() { w.Path },
            }).ToList(),
            IntervalSeconds = Monitor.IntervalSeconds,
        });
    }
}
