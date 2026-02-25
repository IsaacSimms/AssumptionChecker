///// converts a value (bool) to HorizontalAlignment (true = Right, false = Left)                                      /////
///// used to align the content of a cell in the DataGrid based on a boolean value (e.g., IsAssumptionMet)             /////
///// has inverse (InverseBoolToAlignmentConverter) to align content in the opposite way (true = Left, false = Right)  /////

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