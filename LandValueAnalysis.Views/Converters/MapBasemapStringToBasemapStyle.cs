using Esri.ArcGISRuntime.Mapping;
using System.Globalization;
using System.Windows.Data;

namespace LandValueAnalysis.Views.Converters;

public sealed class MapBasemapStringToBasemapStyle : IValueConverter
{
    public static readonly Dictionary<string, BasemapStyle> _stringObjectRepresentations = new Dictionary<string, BasemapStyle>()
    {
        ["Satellite"] = BasemapStyle.ArcGISImageryStandard,
        ["Basic Map"] = BasemapStyle.ArcGISLightGrayBase,
        ["Terrain"] = BasemapStyle.ArcGISTerrainBase,
        ["Dark Mode"] = BasemapStyle.ArcGISDarkGrayBase,
    };
    public static readonly Dictionary<BasemapStyle, string> _objectStringRepresentations = new Dictionary<BasemapStyle, string>()
    {
        [BasemapStyle.ArcGISImageryStandard] = "Satellite",
        [BasemapStyle.ArcGISLightGrayBase] = "Basic Map",
        [BasemapStyle.ArcGISTerrainBase] = "Terrain",
        [BasemapStyle.ArcGISDarkGrayBase] = "Dark Mode",
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) 
        => _objectStringRepresentations[(BasemapStyle)value];

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) 
        => _stringObjectRepresentations[(string)value];
}
