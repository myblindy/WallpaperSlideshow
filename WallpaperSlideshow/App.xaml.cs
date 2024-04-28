using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text.Json;
using System.Windows;
using WallpaperSlideshow.Models;
using WallpaperSlideshow.Services;

namespace WallpaperSlideshow;

public partial class App : Application
{
    TaskbarIcon? notifyIcon;

    const string configurationFileName = "config.json";

    sealed class Configuration
    {
        public string? SourcePath { get; set; }
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
                Monitor.SourcePath = configuration.SourcePath;
            }
        }

        DisplaySettingsChanged(null, EventArgs.Empty);
        SystemEvents.DisplaySettingsChanged += DisplaySettingsChanged;
    }

    private void DisplaySettingsChanged(object? sender, EventArgs e) =>
        WallpaperService.UpdateGeometry();

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
            IntervalSeconds = Monitor.IntervalSeconds,
            SourcePath = Monitor.SourcePath,
        });
    }
}
