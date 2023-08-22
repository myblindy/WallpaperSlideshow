using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace WallpaperSlideshow.Services;

class FileCacheService
{
    class Data
    {
        public List<string> Files { get; } = new();
        public FileSystemWatcher Watcher { get; init; } = null!;
        public CancellationTokenSource CancellationTokenSource { get; } = new();
    }
    readonly Dictionary<string, Data> cache = new();
    static readonly Regex validFiles = new(@"\.(?:gif|png|bmp|jpg|jpeg|webp)$", RegexOptions.IgnoreCase);

    public void Update(IEnumerable<string?> paths)
    {
        foreach (var path in cache.Keys.Except(paths.Where(p => !string.IsNullOrWhiteSpace(p))))
        {
            cache[path!].Watcher.Dispose();
            cache[path!].CancellationTokenSource.Cancel();
            cache.Remove(path!);
        }

        foreach (var path in paths.Where(p => !string.IsNullOrWhiteSpace(p)).Except(cache.Keys))
        {
            Data data = new()
            {
                Watcher = new(path!)
                {
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true,
                }
            };
            FileSystemWatcher fsw = data.Watcher;
            fsw.Renamed += (s, e) =>
            {
                lock (data.Files)
                {
                    data.Files.Remove(e.OldFullPath);
                    if (validFiles.IsMatch(e.FullPath))
                        data.Files.Add(e.FullPath);
                }
            };
            fsw.Deleted += (s, e) =>
            {
                lock (data.Files)
                    data.Files.Remove(e.FullPath);
            };
            fsw.Created += (s, e) =>
            {
                lock (data.Files)
                    if (validFiles.IsMatch(e.FullPath))
                        data.Files.Add(e.FullPath);
            };

            new Thread(() =>
            {
                const int maxCacheSize = 100;
                var tempList = new List<string>(maxCacheSize);

                void dump()
                {
                    lock (data.Files)
                        data.Files.AddRange(tempList);
                    tempList.Clear();
                }

                foreach (var file in Directory.EnumerateFiles(path!).Where(p => validFiles.IsMatch(p)))
                {
                    tempList.Add(file);
                    if (tempList.Count > maxCacheSize)
                    {
                        if (data.CancellationTokenSource.Token.IsCancellationRequested)
                            return;
                        dump();
                    }
                }

                dump();
            })
            { Name = $"File Enumeration Thread for {path}", IsBackground = true }.Start();

            cache[path!] = data;
        }
    }

    public string? GetRandomFilePath(string key)
    {
        if (!cache.TryGetValue(key, out var data)) return null;
        lock (data.Files)
            return data.Files.Count == 0 ? null : data.Files[Random.Shared.Next(data.Files.Count)];
    }
}
