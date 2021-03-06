﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace Emby.WindowsPhone.Converters
{
    public class LoggedInDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Utils.DaysAgo(value) : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
