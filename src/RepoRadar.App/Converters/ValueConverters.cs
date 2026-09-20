using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RepoRadar.App.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            var result = Invert ? !b : b;
            return result ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class NonEmptyStringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class DirtyStateToBorderBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush CleanBrush = new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10B981"));
    private static readonly SolidColorBrush DirtyBrush = new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F59E0B"));
    private static readonly SolidColorBrush ErrorBrush = new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444"));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isDirty)
        {
            return isDirty ? DirtyBrush : CleanBrush;
        }
        return CleanBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
