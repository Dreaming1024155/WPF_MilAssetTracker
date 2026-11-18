using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WPF_프로젝트;

/// <summary>값이 있으면 Visible, null/빈 문자열이면 Collapsed. Invert=True면 반대.</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isEmpty = value is null || (value is string s && s.Length == 0);
        var visible = Invert ? isEmpty : !isEmpty;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
