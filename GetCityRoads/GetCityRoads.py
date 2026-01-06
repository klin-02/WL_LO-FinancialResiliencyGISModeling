import geopandas as gp
from pathlib import Path
import json
import matplotlib

#fix some compatibility issues
matplotlib.use("qt5agg",force=True)
from matplotlib import pyplot as plt

#initialize directory
dir = Path(__file__).resolve().parent

lotDataPath = dir / "WestLinnLotsData.ndgeojson"
cityBoundsDataPath = dir / "WestLinnBoundaries.geojson"

#read line-delimited geojson into geodataframe
lotDataList = []

with open(lotDataPath, encoding='utf-8-sig') as file:
    for line in file:
        dict = json.loads(line)
        lotDataList.append(dict)

lotDataFrame = gp.GeoDataFrame.from_features(lotDataList)

cityBoundsDataFrame = gp.read_file(cityBoundsDataPath, use_arrow=True)

#ensure coordinate system consistency
cityBoundsDataFrame = cityBoundsDataFrame.to_crs(epsg=2913)
lotDataFrame = lotDataFrame.set_crs(epsg=2913, allow_override=True)

print(cityBoundsDataFrame.crs)
print(lotDataFrame.crs)

#plot what the map looks like before
basemap = cityBoundsDataFrame.plot(color="lightblue", edgecolor="black")
lotDataFrame.boundary.plot(ax=basemap, edgecolor="black")

#remove lot polygons from the larger jurisdiction polygon to get the road network
#anything that isn't a taxlot is most of the time, a road
#I'll fix the errors with some manual editing in maptiler
roadNetworkDataFrame = cityBoundsDataFrame.overlay(lotDataFrame, how="difference")

#reproject
roadNetworkDataFrame = roadNetworkDataFrame.to_crs(epsg=4326)

roadNetworkDataFrame.to_file("WestLinnRoadNetwork.geojson", driver="GeoJSON")

#plot after operation
basemap.clear()

roadNetworkDataFrame.plot(ax=basemap, color="lightblue", edgecolor="black")
