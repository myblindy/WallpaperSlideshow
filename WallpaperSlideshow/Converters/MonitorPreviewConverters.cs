﻿using System;
using System.Globalization;
using System.Windows.Data;
using WallpaperSlideshow.Models;

namespace WallpaperSlideshow.Converters;

sealed class MonitorPreviewLeftConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.Length == 3 && values[0] is int left && values[1] is Rectangle allBounds && values[2] is double actualWidth
            ? (double)(left - allBounds.Left) / allBounds.Width * actualWidth : Binding.DoNothing;

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

sealed class MonitorPreviewTopConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.Length == 3 && values[0] is int top && values[1] is Rectangle allBounds && values[2] is double actualHeight
            ? (double)(top - allBounds.Top) / allBounds.Width * actualHeight : Binding.DoNothing;

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

sealed class MonitorPreviewWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.Length == 3 && values[0] is int width && values[1] is Rectangle allBounds && values[2] is double actualWidth
            ? (double)width / allBounds.Width * actualWidth : Binding.DoNothing;

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

sealed class MonitorPreviewHeightConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.Length == 3 && values[0] is int height && values[1] is Rectangle allBounds && values[2] is double actualHeight
            ? (double)height / allBounds.Height * actualHeight : Binding.DoNothing;

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
