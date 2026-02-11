using LandValueScraper.Models;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using System.Text;

namespace LandValueScraper.Services;

public sealed class DeserializeAddressGeoJson
{
    private static readonly string _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Resources\\WestLinnAddresses.ndgeojson");

    private readonly IConfiguration _configuration;
    private readonly IPreparedGeometry _bounds;

    public DeserializeAddressGeoJson(IConfiguration configuration)
    {
        _configuration = configuration;

        _bounds = GetBounds();
    }

    private IPreparedGeometry GetBounds()
    {
        string boundsWkt = _configuration["boundsWkt"] ?? throw new Exception("\'boundsWkt\' does not exist in appsettings.json.");
        WKTReader reader = new WKTReader();
        Geometry polygon = reader.Read(boundsWkt);

        return (polygon is Polygon) ? PreparedGeometryFactory.Prepare(polygon) : throw new Exception("bounds must be polygon!");
    }

    //Only has support for regular json structure and ndgeojson at the moment :C
    public List<DeserializedAddressGeoJsonDTO> Deserialize(string city)
    {
        using StreamReader streamReader = new StreamReader(_filePath, new UTF8Encoding());
        var serializer = GeoJsonSerializer.Create();
        List<DeserializedAddressGeoJsonDTO> deserializedGeoJson = new List<DeserializedAddressGeoJsonDTO>();

        using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
        {   
            //Allow multiple json objects to be deserialized
            jsonTextReader.SupportMultipleContent = true;

            while (jsonTextReader.Read())
            {
                deserializedGeoJson.Add(serializer.Deserialize<DeserializedAddressGeoJsonDTO>(jsonTextReader));
            }
        }

        //return entries within bounds + with non-null entries
        return deserializedGeoJson.Where(x => IsWithinBounds(x.geometry.coordinates)).ToList() ??
            throw new NullReferenceException("There is no geojson shared the in the file, or structure is invalid!");
    }

    private bool IsWithinBounds(double[] coords)
    {
        Point point = new Point(new Coordinate(coords[0], coords[1]));
        return _bounds.Contains(point);
    }
}
