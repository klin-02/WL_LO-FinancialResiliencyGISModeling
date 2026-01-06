using System.Globalization;
using System.Windows.Data;

namespace LandValueAnalysis.Views.Converters;

public class SingleValueMarginLeftFormatter : IValueConverter
{
    public object Convert(object value, Type type, object parameter, CultureInfo culture) 
        => value + ",0,0,0";

    public object ConvertBack(object value, Type type, object parameter, CultureInfo culture) 
        => throw new NotImplementedException();
}
