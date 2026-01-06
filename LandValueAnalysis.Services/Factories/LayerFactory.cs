using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Symbology;
using System.Collections.Concurrent;
using System.Data;
using System.Drawing;
using System.Globalization;
using Wacton.Unicolour;

namespace LandValueAnalysis.Services.Factories;

public enum DataView
{
    BuildingFootprint = 0,
    LandValuePerAcre = 1,
    NetInfrastructureDeficit = 2,
    LandUseClassification = 3,
    AverageBuildingFootprint = 4,
    AverageLandValuePerAcre = 5,
    LotScaleFootprints = 6,
    LotScale_LV_PerAcre = 7,
    Neighborhoods = 8
}

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class LayerFactory : IFactory<DataView, FeatureLayer>
{
    private record OklchColor
    (
        double L,
        double C,
        double H
    );

    //Data paths
    private static readonly string _lotData = Path.Combine(Directory.GetCurrentDirectory(), 
        "Resources\\WestLinnLots.gpkg");
    private static readonly string _censusBlockData = Path.Combine(Directory.GetCurrentDirectory(), 
        "Resources\\CensusBlockData.gpkg");
    private static readonly string _neighborhoodPolygonsData = Path.Combine(Directory.GetCurrentDirectory(), 
        "Resources\\NeighborhoodPolygonsData.gpkg");

    //fields for popups
    //Takes in FieldName (the field to render on), label (alias of the field); rest is self-explanatory
    private static readonly PopupField[] _lotsFields =
    {
        new PopupField()
        {
            FieldName = "building footprint",
            Label = "Building Footprint",
            IsVisible = true,
            IsEditable = false,
            StringFieldOption = PopupStringFieldOption.SingleLine
        },
        new PopupField()
        {
            FieldName = "land use",
            Label = "Land Use",
            IsVisible = true,
            IsEditable = false,
            StringFieldOption = PopupStringFieldOption.SingleLine
        },
        new PopupField()
        {
            FieldName = "lot size (acres)",
            Label = "Lot Size (acres)",
            IsVisible = true,
            IsEditable = false,
            StringFieldOption = PopupStringFieldOption.SingleLine
        },
        new PopupField()
        {
            FieldName = "land value/acre ($)",
            Label = "Land Value/acre ($)",
            IsVisible = true,
            IsEditable = false,
            StringFieldOption = PopupStringFieldOption.SingleLine
        },
    };

    //Popup title definition field
    //Wrap title field in curly braces to indicate it's a field-derived title
    private const string _lotsPopupTitle = "{address}";
    private const string _censusBlockTitle = "{blockID}";
    private const string _neighborhoodPolygonsTitle = "{neighborhood_name}";

    //Oklch triplets for diverging color gradient
    private static readonly OklchColor _color1Oklch = new OklchColor //light yellow
    (
        L: 0.9904,
        C: 0.0491,
        H: 107.26
    );
    private static readonly OklchColor _color2Oklch = new OklchColor //blue
    (
        L: 0.2604,
        C: 0.1097,
        H: 264.57
    );

    private static readonly ConcurrentDictionary<DataView, Lazy<Task<FeatureLayer>>> _defaultLayers =
        new ConcurrentDictionary<DataView, Lazy<Task<FeatureLayer>>>();

    public LayerFactory()
    {
        LoadDefaultLayers();
    }

    public async Task<FeatureLayer> BuildAsync(DataView layerType)
    {
        if (_defaultLayers.TryGetValue(layerType, out var layer))
        {
            return await layer.Value;
        }
        throw new Exception("Layer Doesn't Exist");
    }

    private void LoadDefaultLayers()
    { /*
        _defaultLayers.TryAdd(
            DataView.LandValuePerAcre,
            new Lazy<Task<FeatureLayer>>(() => 
                CreateLayerAsync(_censusBlockData, "land value/acre ($)")
            ));
        _defaultLayers.TryAdd(
            DataView.BuildingFootprint,
            new Lazy<Task<FeatureLayer>>(() =>
                CreateLayerAsync(_censusBlockData, "building footprint")
            ));
        _defaultLayers.TryAdd(
            DataView.NetInfrastructureDeficit,
            new Lazy<Task<FeatureLayer>>(() => 
                CreateLayerAsync(_neighborhoodPolygonsData, "net neighborhood infrastructure deficit ($)")
            )); */
        _defaultLayers.TryAdd(
            DataView.LotScaleFootprints,
            new Lazy<Task<FeatureLayer>>(() =>
                CreateLayerAsync(_lotData, 0, 1, _lotsFields, _lotsPopupTitle, "building footprint")
            ));
        _defaultLayers.TryAdd(
            DataView.LotScale_LV_PerAcre,
            new Lazy<Task<FeatureLayer>>(() =>
                CreateLayerAsync(_lotData, 0, 20000000, _lotsFields, _lotsPopupTitle, "land value/acre ($)")
            )); /*
        _defaultLayers.TryAdd(
            DataView.Neighborhoods,
            new Lazy<Task<FeatureLayer>>(() =>
                CreateLayerAsync(_neighborhoodPolygonsData)
            )); */
    }

    //Takes in data source path, min max data bounds for rendering gradient, popup fields, popup title field, and field to render on
    private async Task<FeatureLayer> CreateLayerAsync(string dataSource, int min, int max, PopupField[] popupFields, string popupTitleField, string renderingField = "")
    {
        //Load data into table
        FeatureTable table = await LoadGeoPackageTable(dataSource);

        //Create layer from table
        FeatureLayer layer = new FeatureLayer(table);
        layer.IsPopupEnabled = true;
        //Dynamic to support 3d rendering
        layer.RenderingMode = FeatureRenderingMode.Dynamic;

        //Configure popup definition
        PopupDefinition popupDef = ConfigurePopupDefinition(popupFields, popupTitleField);
        table.PopupDefinition = popupDef;

        //Render symbols if field for rendering is specified
        if (!string.IsNullOrEmpty(renderingField))
        {
            FeatureQueryResult layerFeatures = await GetFeatures(table);

            //get data of the rendering field
            List<double> uniqueValues = layerFeatures.Select(feature => feature
                .GetAttributeValue(renderingField))
                .Distinct()
                .Select(value => ParseDouble(value))
                .ToList();

            layer.Renderer = CreateRenderer(renderingField, uniqueValues, min, max);
        } 
        return layer;
    }

    private PopupDefinition ConfigurePopupDefinition(PopupField[] popupFields, string popupTitleField)
    {
        PopupDefinition popupDef = new PopupDefinition();
        popupDef.Title = popupTitleField;

        //add fields to popup elements to create a popup table so that the fields show
        popupDef.Elements.Add(new FieldsPopupElement(popupFields));
        return popupDef;
    }

    private double ParseDouble(object? value)
    {
        if (value == null) { return 0; }

        bool isValid = double.TryParse(
            value.ToString(),
            NumberStyles.AllowCurrencySymbol | NumberStyles.Currency,
            CultureInfo.CurrentCulture,
            out double numericValue
            );

        return isValid ? numericValue : 0;
    }

    private async Task<FeatureTable> LoadGeoPackageTable(string path)
    {
        GeoPackage geoPackage = await GeoPackage.OpenAsync(path);
        return geoPackage.GeoPackageFeatureTables[0];
    }

    private async Task<FeatureQueryResult> GetFeatures(FeatureTable table)
    {
        QueryParameters queryParameters = new QueryParameters()
        {
            WhereClause = "1=1"
        };
        return await table.QueryFeaturesAsync(queryParameters);
    }

    //Make the render for a continously colored choropleth with UniqueValueRenderer
    //Continous rendering personally implemented because the .NET Sdk does not support it at the moment
    private Renderer CreateRenderer(string renderingField, List<double> uniqueValues, int min, int max)
    {

        UniqueValueRenderer uniqueValueRenderer = new UniqueValueRenderer();
        uniqueValueRenderer.FieldNames.Add(renderingField);

        foreach (double value in uniqueValues)
        {
            //normalize from 0-1 to plug into gradient
            double normalizedValue = Math.Clamp((value - min) / (max - min), 0, 1);

            Color interpolatedOklchColorAsRgb = InterpolateColor(normalizedValue);

            SimpleFillSymbol fillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, interpolatedOklchColorAsRgb, null);
            uniqueValueRenderer.UniqueValues.Add(new UniqueValue("", "", fillSymbol, value));
        }
        return uniqueValueRenderer;
    }

    //Takes a number from 0-1 and plugs it into an oklch gradient for a color that's converted back to rgb
    private Color InterpolateColor(double normalizedValue)
    {
        //apply bezier curve-like function to smooth out gradient
        double smoothedValue = ApplyParametricCurve(normalizedValue);

        //define color stops for diverging color gradient
        OklchColor interpolatedColor = InterpolateOklch(smoothedValue);

        //turn values into oklch color (values converted from radians to degrees)
        Unicolour oklchColor = new Unicolour
            (
            ColourSpace.Oklch, 
            interpolatedColor.L, 
            interpolatedColor.C, 
            interpolatedColor.H
            );

        //convert to rgb since that's what arcgis speaks
        var rgb = oklchColor.Rgb;
        return Color.FromArgb(Math.Clamp((int)(rgb.R * 255), 0, 255), Math.Clamp((int)(rgb.G * 255), 0, 255), Math.Clamp((int)(rgb.B * 255), 0, 255));
    }

    private double ApplyParametricCurve(double normalizedValue)
    {
        double square = normalizedValue * normalizedValue;
        return square / (2 * (square - normalizedValue) + 1);
    }

    //interpolate the oklch color on a gradient from a number between 0-1
    private OklchColor InterpolateOklch(double normalizedValue)
    {
        double hue1 = _color1Oklch.H % 360.0;
        double hue2 = _color2Oklch.H % 360.0;

        double hueDifference = hue2 - hue1;

        //shortest path rendering
        //If longest path rendering is desired, make the hue difference <= 180 & add instead of subtract 360
        if (Math.Abs(hueDifference) > 180.0)
        {
            hueDifference = hueDifference - 360.0 * Math.Sign(hueDifference);
        }

        return new OklchColor
        (
            L: _color1Oklch.L + normalizedValue * (_color2Oklch.L - _color1Oklch.L),
            C: _color1Oklch.C + normalizedValue * (_color2Oklch.C - _color1Oklch.C),
            H: (hue1 + normalizedValue * hueDifference + 360.0) % 360.0
        );
    }
}
