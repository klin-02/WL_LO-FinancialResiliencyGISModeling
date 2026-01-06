using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using System.Text;

namespace LandValueScraper.Services;

public sealed class DatasetSerializer
{
    private static readonly string _filePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "WestLinnLotsData.geojson"
    );

    public async Task Serialize(List<Feature> clackamasFeatures)
    {
        if (clackamasFeatures.Count != 0)
        {
            List<string> geoJson = FeatureCollectionToGeoJsonString(clackamasFeatures);
            await WriteToGeoJSONFile(geoJson, _filePath);
        }
    }

    private List<string> FeatureCollectionToGeoJsonString(List<Feature> features)
    {
        //using string collection to format in newline geojson
        List<string> stringFeatures = new List<string>();

        var serializer = GeoJsonSerializer.Create();

        foreach (Feature feature in features)
        {
            using StringWriter stringWriter = new StringWriter();
            using JsonTextWriter jsonTextWriter = new JsonTextWriter(stringWriter);

            serializer.Serialize(jsonTextWriter, feature);
            stringFeatures.Add(stringWriter.ToString());
        }
        return stringFeatures;
    }

    //Writes new lines to or creates geojson file depending on if it exists or not
    private async Task WriteToGeoJSONFile(List<string> stringFeatures, string filePath)
    {
        await File.AppendAllLinesAsync(filePath, stringFeatures, Encoding.UTF8);
    }
}
