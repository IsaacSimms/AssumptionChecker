///// converts a boolean to HorizontalAlignment (true = Right, false = Left) /////

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AssumptionChecker.WPFApp.Converters
{
    public class BoolToAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? HorizontalAlignment.Right : HorizontalAlignment.Left;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}