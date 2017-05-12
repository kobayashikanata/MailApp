using System;
using System.Windows.Data;
using System.Globalization;

namespace MailApp
{
    public class ValidStringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach(var value in values)
            {
                if (value == null) continue;
                var v = value as string;
                if (string.IsNullOrEmpty(v)) continue;
                return v;
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
