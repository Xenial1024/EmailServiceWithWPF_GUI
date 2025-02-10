using System;
using System.Globalization;
using System.Windows.Data;

namespace ServiceManagementWithGUI.Views
{
    public class UshortToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value?.ToString() ?? "0";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (ushort.TryParse(value as string, out ushort result))
                return result;
            return (ushort)10;
        }
    }
}