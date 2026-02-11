using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandValueScraper.Models;
/*mimics deserialized json schema,
yes I didn't follow naming conventions deal with it */
public record DeserializedAddressGeoJsonDTO(
    string type,
    Properties properties,
    Geom geometry
);

//nullable since not all fields in the geojson have these
public record Properties(
    string? SITUS,
    string? SITUS_CITY,
    int? SITUS_ZIP //this metric is a little unreliable
    );

public record Geom(
    string type,
    double[] coordinates
    );
