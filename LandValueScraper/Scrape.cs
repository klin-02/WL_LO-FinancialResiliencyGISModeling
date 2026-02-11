using LandValueScraper.Models;
using LandValueScraper.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Features;

namespace LandValueScraper;

public sealed class Scrape : BackgroundService
{
    private static readonly string[] _cities = { "BARLOW", "CANBY", "DAMASCUS", "ESTACADA", "GLADSTONE", "HAPPY VALLEY", "JOHNSON CITY", "LAKE OSWEGO", "MILWAUKIE", "MOLALLA", "OREGON CITY", "PORTLAND", "RIVERGROVE", "SANDY", "TUALATIN", "WEST LINN", "WILSONVILLE", "BEAVERCREEK", "BORING", "CLACKAMAS", "GOVERNMENT CAMP", "JENNINGS LODGE", "MOUNT HOOD VILLAGE", "MULINO", "OAK GROVE", "OATFIELD", "RHODODENDRON", "STAFFORD", "SUNNYSIDE", "MOLALLA PRAIRIE" };

    private readonly ScrapeLandValues _scrapeLandValues;
    private readonly DeserializeAddressGeoJson _deserializeAddressGeoJson;
    private readonly ParseClackamasData _parseClackamasData;
    private readonly DatasetSerializer _datasetSerializer;
    private readonly IConfiguration _configuration;

    private readonly string _city;

    public Scrape(
        ScrapeLandValues scrapeLandValues, 
        DeserializeAddressGeoJson deserializeGeoJson, 
        ParseClackamasData parseClackamasData,
        DatasetSerializer datasetSerializer,
        IConfiguration configuration
        )
    {
        _scrapeLandValues = scrapeLandValues;
        _deserializeAddressGeoJson = deserializeGeoJson;
        _parseClackamasData = parseClackamasData;
        _datasetSerializer = datasetSerializer;
        _configuration = configuration;

        _city = GetCity();
    }

    private string GetCity()
    {
        string city = _configuration["city"] ?? throw new Exception("\'city\' does not exist in appsettings.json.");

        return (_cities.Contains(city, StringComparer.OrdinalIgnoreCase)) ? city : throw new Exception("enter a city in Clackamas County!");
    }

    protected override async Task ExecuteAsync(CancellationToken stopToken)
    {
        List<DeserializedAddressGeoJsonDTO> addressDTOs = _deserializeAddressGeoJson.Deserialize(_city);

        List<Feature> clackamasFeatures = await GetClackamasObjects(addressDTOs);
        await _datasetSerializer.Serialize(clackamasFeatures);

        //stop application
        await base.StopAsync(stopToken);
    }

    private async Task<List<Feature>> GetClackamasObjects(List<DeserializedAddressGeoJsonDTO> addressDTOs)
    {
        List<Feature> clackamasFeatures = new List<Feature>();

        foreach (var addressDTO in addressDTOs)
        {
            string[]? jsonResponse = await _scrapeLandValues.ScrapeObject(addressDTO, _city);

            //index [0] = taxlot json, index [1] = assessment json
            Feature? feature = (jsonResponse != null) ?
                _parseClackamasData.ParseLandValueData(jsonResponse[0], jsonResponse[1], addressDTO, _city) : null;

            if (feature != null)
            {
                clackamasFeatures.Add(feature);
            }

            //prevent memory leaks by clearing the feature collection every 1000 records + appending it to the file
            if ((clackamasFeatures.Count) % 1000 == 0 && clackamasFeatures.Count != 0)
            {
                await _datasetSerializer.Serialize(clackamasFeatures);
                clackamasFeatures.Clear();
            }
        }
        return clackamasFeatures;
    }
}