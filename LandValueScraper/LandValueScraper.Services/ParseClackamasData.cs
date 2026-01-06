using LandValueScraper.Models;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;

namespace LandValueScraper.Services;

public sealed class ParseClackamasData
{
    private static readonly Dictionary<string, string> _landUseLookups = new Dictionary<string, string>()
    {
        ["101"] = "SINGLE-FAMILY",
        ["102"] = "MULTI-FAMILY",
        ["201"] = "COMMERCIAL",
        ["202"] = "MULTI-FAMILY",
        ["701"] = "MULTI-FAMILY"
    };

    public Feature? ParseLandValueData(
        string taxlotJson, 
        string assessmentJson,
        DeserializedAddressGeoJsonDTO deserializedAddressGeoJsonDTO, 
        string city
        )
    {
        //null check
        Geometry? geometry = DeserializeGeometry(assessmentJson);
        if (geometry == null) { return null; }

        JObject parsedTaxlotJson = JObject.Parse(taxlotJson); 
        JObject parsedAssessmentJson = JObject.Parse(assessmentJson); 

        string landUse = ParseOutLandUseString(parsedTaxlotJson);
        double lotSizeInAcres = GetLotSizeInAcres(geometry);
        double? totalValue = ParseOutTotalValue(parsedAssessmentJson);

        //more null checks
        if (totalValue == null || landUse == null) { return null; }

        string address = FormatAddress(
            deserializedAddressGeoJsonDTO.properties.number,
            deserializedAddressGeoJsonDTO.properties.street,
            city,
            deserializedAddressGeoJsonDTO.properties.postcode
            );

        return new Feature(
            geometry,
            new AttributesTable(new Dictionary<string, object>
            {
                ["address"] = address,
                ["land use"] = landUse,
                ["lot size (acres)"] = Math.Round(lotSizeInAcres, 4).ToString(),
                ["land value/acre ($)"] = Moneyfy(totalValue / lotSizeInAcres)
            }));
    }

    private Geometry? DeserializeGeometry(string rawAssessmentJson)
    {
        using JsonDocument jsonDoc = JsonDocument.Parse(rawAssessmentJson);
        string rawAssessmentGeometry = jsonDoc.RootElement
            .GetProperty("features")[0]
            .GetProperty("geometry")
            .GetRawText();
        var serializer = GeoJsonSerializer.Create();

        using StringReader stringReader = new StringReader(rawAssessmentGeometry);
        using JsonTextReader jsonReader = new JsonTextReader(stringReader);

        Geometry? geometry = serializer.Deserialize<Geometry?>(jsonReader);

        return geometry;
    }

    private string? ParseOutLandUseString(JObject parsedTaxlotJson)
    {
        string parsedLandDataString = (string)parsedTaxlotJson.SelectToken("features[0].properties.landclass");
        if (parsedLandDataString == null) { return null; }
        return _landUseLookups[parsedLandDataString];
    }

    //Convert EPSG:2913 projection units (which is in sq ft) to acres
    private double GetLotSizeInAcres(Geometry geometry) => geometry.Area / 43560;

    private double? ParseOutTotalValue(JObject parsedAssessmentJson)
    {
        string parsedLandDataString = (string)parsedAssessmentJson.SelectToken("features[0].properties.market_total_value");
        if (parsedLandDataString == null) { return null; }
        return double.Parse(parsedLandDataString, NumberStyles.Currency);
    }

    private string FormatAddress(string number, string street, string city, string postcode)
    {
        string partiallyFormattedAddress = $"{number} " + 
            $"{street} " + 
            $"{city}, OR " + 
            $"{postcode}";
        return Regex.Replace(partiallyFormattedAddress, @"\s+", " ").Trim(' ');
    }

    private string Moneyfy(double? currency)
    {
        decimal decimalCurrency = Convert.ToDecimal(currency);
        return decimalCurrency.ToString("C");
    }
}
