using System;
using System.Globalization;
using System.Windows.Data;

namespace ServiceManagementWithGUI.Views
{
    public class HoursOrMinutesToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is byte b ? b.ToString("D2") : "00";
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (byte.TryParse(value as string, out byte result))
                return result;
            return (byte)0;
        }
    }
}
