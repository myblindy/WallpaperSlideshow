using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WallpaperSlideshow.Support;

internal static class Extensions
{
    public static void ForEach<T>(this IList source, Action<T> action)
    {
        foreach (T item in source)
            action(item);
    }
}
