using Esri.ArcGISRuntime.Mapping;
using System.Globalization;
using System.Windows.Data;

namespace LandValueAnalysis.Views.Converters;

public sealed class MapBasemapStringToBasemapStyle : IValueConverter
{
    public static readonly Dictionary<string, BasemapStyle> _stringObjectRepresentations = new Dictionary<string, BasemapStyle>()
    {
        ["Satellite"] = BasemapStyle.ArcGISImagery,
        ["Basic Map"] = BasemapStyle.ArcGISLightGray,
        ["Streets"] = BasemapStyle.ArcGISStreets,
        ["Dark Mode"] = BasemapStyle.ArcGISDarkGrayBase,
    };
    public static readonly Dictionary<BasemapStyle, string> _objectStringRepresentations = new Dictionary<BasemapStyle, string>()
    {
        [BasemapStyle.ArcGISImagery] = "Satellite",
        [BasemapStyle.ArcGISLightGray] = "Basic Map",
        [BasemapStyle.ArcGISStreets] = "Streets",
        [BasemapStyle.ArcGISDarkGrayBase] = "Dark Mode",
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) 
        => _objectStringRepresentations[(BasemapStyle)value];

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) 
        => _stringObjectRepresentations[(string)value];
}
