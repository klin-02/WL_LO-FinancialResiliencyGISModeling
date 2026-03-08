using System.Globalization;
using System.Windows.Data;
using LandValueAnalysis.Services.Factories;
using LandValueAnalysis.Models.Shared;

namespace LandValueAnalysis.Views.Converters;

public sealed class MapStringNameToMapEnum : IValueConverter
{
    public static readonly Dictionary<string, DataView> _stringEnumRepresentations = new Dictionary<string, DataView>()
    {
        ["Infrastructure Cost/Building Area ($)"] = DataView.Infrastructure,
        ["Zoning"] = DataView.Zoning,
        ["Building Footprints"] = DataView.Footprints,
        ["Net Present Value per Hectare ($)"] = DataView.NetPresentValuePerHectare
    };
    public static readonly Dictionary<DataView, string> _enumStringRepresentations = new Dictionary<DataView, string>()
    {
        [DataView.Infrastructure] = "Infrastructure Cost/Building Area ($)",
        [DataView.Zoning] = "Zoning",
        [DataView.Footprints] = "Building Footprints",
        [DataView.NetPresentValuePerHectare] = "Net Present Value per Hectare ($)"
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) 
        => _enumStringRepresentations[(DataView)value];

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) 
        => _stringEnumRepresentations[(string)value];
}
