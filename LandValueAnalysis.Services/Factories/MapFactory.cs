using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using LandValueAnalysis.Models.Shared;
using LandValueAnalysis.Models;

namespace LandValueAnalysis.Services.Factories;

//Makes the maps
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class MapFactory
{
    private readonly Viewpoint _defaultViewpoint;

    private static readonly Dictionary<MapMode, MapStatus> _defaultMaps =
        new Dictionary<MapMode, MapStatus>();

    public MapFactory()
    {
        LoadDefaultMaps();

        //Set default viewpoints for both 2D and 3D (program starts in 2D)
        _defaultViewpoint = new Viewpoint(
            new MapPoint(-122.636336, 45.367024, SpatialReferences.Wgs84), 
            10000);
    }

    public MapStatus Build(MapMode mapMode, Viewpoint? existingViewpoint = null)
    {
        MapStatus map = _defaultMaps[mapMode];
        if (map == null) { throw new NullReferenceException("Map doesn't exist!"); }
        map.CurrentMap.InitialViewpoint = existingViewpoint ?? _defaultViewpoint;
        return map;
    }

    private void LoadDefaultMaps()
    {
        _defaultMaps.Add(
            MapMode.TwoDimensional,
            new MapStatus(
                MapMode.TwoDimensional,
                new Map()
                )
            );
        _defaultMaps.Add(
            MapMode.ThreeDimensional,
            new MapStatus(
                MapMode.ThreeDimensional, 
                new Scene()
                )
            );
    }
}
