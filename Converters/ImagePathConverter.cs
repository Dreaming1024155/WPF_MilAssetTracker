using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace WPF_프로젝트;

/// <summary>
/// 파일 경로 → BitmapImage.
/// OnLoad 캐시로 읽어서 파일이 잠기지 않으므로 사진 교체/삭제가 가능합니다.
/// ConverterParameter에 숫자를 주면 그 폭으로 축소 디코딩합니다(썸네일 메모리 절약).
/// </summary>
public class ImagePathConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            if (parameter is string s && int.TryParse(s, out var width) && width > 0)
                bmp.DecodePixelWidth = width;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;   // 손상된 이미지면 '사진 없음' 표시
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
