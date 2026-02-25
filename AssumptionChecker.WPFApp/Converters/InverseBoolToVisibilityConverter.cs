///// Converts a value (bool) to Visibility (true = Collapsed, false = Visible) /////

// == namespaces == //
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AssumptionChecker.WPFApp.Converters
{
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
