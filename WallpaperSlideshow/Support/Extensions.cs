using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WallpaperSlideshow.Support;

internal static class Extensions
{
    public static void ForEach<T>(this IList source, Action<T> action)
    {
        foreach (T item in source)
            action(item);
    }

    private const int GWL_STYLE = -16, WS_MAXIMIZEBOX = 0x10000, WS_MINIMIZEBOX = 0x20000;

    [DllImport("user32.dll")]
    extern private static int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    extern private static int SetWindowLong(IntPtr hwnd, int index, int value);
    public static void DisableMinimizeMaximizeButtons(this Window window, bool canMinimize, bool canMaximize) =>
        window.SourceInitialized += (s, e) =>
        {
            var hWnd = new WindowInteropHelper(window).Handle;

            _ = SetWindowLong(hWnd, GWL_STYLE, GetWindowLong(hWnd, GWL_STYLE)
                & (!canMinimize ? ~WS_MINIMIZEBOX : -1) & (!canMaximize ? ~WS_MAXIMIZEBOX : -1));
        };
}
