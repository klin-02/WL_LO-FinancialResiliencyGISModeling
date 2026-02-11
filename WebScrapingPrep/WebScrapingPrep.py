import geopandas as gp
from pathlib import Path

dir = Path(__file__).resolve().parent

parcelPath = dir / "tax_parcels.zip"
boundariesPath = dir / "WestLinnBoundaries.geojson"

taxParcelFrame = gp.read_file(parcelPath, use_arrow=True)
boundariesFrame = gp.read_file(boundariesPath, use_arrow=True)

taxParcelFrame = taxParcelFrame.to_crs(epsg=2913)
boundariesFrame = boundariesFrame.to_crs(epsg=2913)

boundariesFrame = boundariesFrame[["geometry"]]

#only get address datas within west linn
joinedData =  taxParcelFrame.sjoin(boundariesFrame, predicate="intersects")

#get rid of roads
joinedData = joinedData[~joinedData["MAPTAXLOT"].str.contains("ROADS")]

joinedData = joinedData[["geometry", "SITUS", "SITUS_CITY", "SITUS_ZIP"]]

#get rid of no situs entries
joinedData = joinedData[joinedData["SITUS"] != "NO SITUS"]

#get center points
joinedData = joinedData.set_geometry(joinedData.centroid)

joinedData = joinedData.to_crs(epsg=4326)
joinedData.to_file("WestLinnAddresses.geojson", driver="GeoJSON")

print(joinedData)

