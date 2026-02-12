using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Symbology;
using LandValueAnalysis.Models.Shared;
using System.Collections.Concurrent;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Wacton.Unicolour;

namespace LandValueAnalysis.Services.Factories;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class LayerFactory
{
    private record OklchColor
    (
        double L,
        double C,
        double H
    );

    //Data paths
    private static readonly string _lotData = Path.Combine(Directory.GetCurrentDirectory(),
        "Resources\\WestLinnLandValueDatas.gpkg");
    private static readonly string _infrastructureData = Path.Combine(Directory.GetCurrentDirectory(),
        "Resources\\WL_InfrastructureData.gpkg");

    //fields for popups
    //Takes in FieldName (the field to render on), label (alias of the field); rest is self-explanatory
    //Add a bit of formatting for fields that have their formatting a little messed up
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
            FieldName = "lot size (hectare)",
            Label = "Lot Size (hectare)",
            IsVisible = true,
            IsEditable = false,
            StringFieldOption = PopupStringFieldOption.SingleLine
        },
        new PopupField()
        {
            FieldName = "net present value/hectare ($)",
            Label = "Net Present Value/hectare ($)",
            IsVisible = true,
            IsEditable = false,
            StringFieldOption = PopupStringFieldOption.SingleLine,
            Format = new PopupFieldFormat() { UseThousandsSeparator = true }
        },
        new PopupField()
        {
            FieldName = "ZONE",
            Label = "Zoning",
            IsVisible = true,
            IsEditable = false,
            StringFieldOption = PopupStringFieldOption.SingleLine
        },
        new PopupField()
        {
            FieldName = "zoning liberties index",
            Label = "Zoning Liberties Index (1-10)",
            IsVisible = true,
            IsEditable = false,
            StringFieldOption = PopupStringFieldOption.SingleLine
        },
    };
    private static readonly PopupField[] _infrastructureFields =
    {
        new PopupField()
        {
            FieldName = "census lot building footprint",
            Label = "Building Coverage",
            IsVisible = true,
            IsEditable = false,
            StringFieldOption = PopupStringFieldOption.SingleLine,
            Format = new PopupFieldFormat() { DecimalPlaces = 2 }
        },
        new PopupField()
        {
            FieldName = "infrastructure cost adjusted for footprint ($)",
            Label = "Infrastructure Cost (Adj. for Building Footprint) ($)",
            IsVisible = true,
            IsEditable = false,
            StringFieldOption = PopupStringFieldOption.SingleLine,
            Format = new PopupFieldFormat() { UseThousandsSeparator = true, DecimalPlaces = 2 }
        },
        new PopupField()
        {
            FieldName = "POP20",
            Label = "Population (2020)",
            IsVisible = true,
            IsEditable = false,
            StringFieldOption = PopupStringFieldOption.SingleLine
        }
    };

    //Popup title definition field
    //Wrap title field in curly braces to indicate it's a field-derived title
    private const string _lotsPopupTitle = "{address}";
    private const string _infrastructurePopupTitle = "Details";

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

    private static readonly ConcurrentDictionary<Models.Shared.DataView, Lazy<Task<FeatureLayer>>> _defaultLayers =
        new ConcurrentDictionary<Models.Shared.DataView, Lazy<Task<FeatureLayer>>>();

    public LayerFactory()
    {
        LoadDefaultLayers();
    }

    public async Task<FeatureLayer> BuildAsync(Models.Shared.DataView layerType)
    {
        if (_defaultLayers.TryGetValue(layerType, out var layer))
        {
            return await layer.Value;
        }
        throw new Exception("Layer Doesn't Exist");
    }

    private void LoadDefaultLayers()
    {
        _defaultLayers.TryAdd(
            Models.Shared.DataView.Infrastructure,
            new Lazy<Task<FeatureLayer>>(() => 
                CreateLayerAsync(_infrastructureData, 0, 3000000, _infrastructureFields, _infrastructurePopupTitle, "infrastructure cost adjusted for footprint ($)", "[infrastructure cost adjusted for footprint ($)] / 250000")
            ));
        _defaultLayers.TryAdd(
            Models.Shared.DataView.Zoning,
            new Lazy<Task<FeatureLayer>>(() =>
                CreateLayerAsync(_lotData, 0, 10, _lotsFields, _lotsPopupTitle, "zoning liberties index", "[zoning liberties index] * 50")
            ));
        _defaultLayers.TryAdd(
            Models.Shared.DataView.Footprints,
            new Lazy<Task<FeatureLayer>>(() =>
                CreateLayerAsync(_lotData, 0, 1, _lotsFields, _lotsPopupTitle, "building footprint", "[building footprint] * 500")
            ));
        _defaultLayers.TryAdd(
            Models.Shared.DataView.NetPresentValuePerHectare,
            new Lazy<Task<FeatureLayer>>(() =>
                CreateLayerAsync(_lotData, 0, 70000000, _lotsFields, _lotsPopupTitle, "net present value/hectare ($)", "[net present value/hectare ($)] / 140000")
            ));
    }

    //Takes in data source path, min max data bounds for rendering gradient, popup fields, popup title field, and field to render on
    private async Task<FeatureLayer> CreateLayerAsync(
        string dataSource, 
        int min, 
        int max, 
        PopupField[] popupFields, 
        string popupTitleField, 
        string renderingField = "", 
        string extrusionExpression = ""
        )
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

            if (!string.IsNullOrEmpty(extrusionExpression))
            {
                ConfigureExtrusionExpression(layer.Renderer.SceneProperties, extrusionExpression);
            }
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

        return double.TryParse(value.ToString(), out var result) == true ? result : 0;
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

    private RendererSceneProperties ConfigureExtrusionExpression(
        RendererSceneProperties properties,
        string extrusionExpression
        )
    {
        properties.ExtrusionMode = ExtrusionMode.AbsoluteHeight;
        properties.ExtrusionExpression = extrusionExpression;
        return properties;
    }
}
