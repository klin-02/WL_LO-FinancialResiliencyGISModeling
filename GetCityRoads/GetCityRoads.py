import geopandas as gp
from pathlib import Path
import json
import matplotlib

#fix some compatibility issues
matplotlib.use("qt5agg",force=True)
from matplotlib import pyplot as plt

#initialize directory
dir = Path(__file__).resolve().parent

alternativeLotDataPath = dir / "tax_parcels.zip"
lotDataPath = dir / "WestLinnLotsData.ndgeojson"
cityBoundsDataPath = dir / "WestLinnBoundaries.geojson"

#make alternate road dataset (using similar technique but using Clackamas GIS Portal Taxlots datas)
alternativeTaxLotsDataframe = gp.read_file(alternativeLotDataPath, use_arrow=True)

#read line-delimited geojson into geodataframe
lotDataList = []

with open(lotDataPath, encoding='utf-8-sig') as file:
    for line in file:
        dict = json.loads(line)
        lotDataList.append(dict)

lotDataFrame = gp.GeoDataFrame.from_features(lotDataList)

cityBoundsDataFrame = gp.read_file(cityBoundsDataPath, use_arrow=True)

#ensure coordinate system consistency
alternativeTaxLotsDataframe = alternativeTaxLotsDataframe.to_crs(epsg=2913)
cityBoundsDataFrame = cityBoundsDataFrame.to_crs(epsg=2913)
lotDataFrame = lotDataFrame.set_crs(epsg=2913, allow_override=True)

print(cityBoundsDataFrame.crs)
print(lotDataFrame.crs)

#cont. alternative dataset creation
alternativeTaxLotsDataframe = alternativeTaxLotsDataframe[alternativeTaxLotsDataframe["MAPTAXLOT"].astype(str).str.contains("ROADS")]
print(alternativeTaxLotsDataframe)

basemap = alternativeTaxLotsDataframe.plot(color="lightblue", edgecolor="black")

alternativeRoadsDataFrame = alternativeTaxLotsDataframe.overlay(cityBoundsDataFrame, how="intersection")

alternativeRoadsDataFrame = alternativeRoadsDataFrame.to_crs(epsg=4326)
alternativeRoadsDataFrame.to_file("AlternativeWestLinnRoadNetwork.geojson", driver="GeoJSON")
#end alt dataset creation

alternativeRoadsDataFrame.plot(ax=basemap, color="lightblue", edgecolor="black")
