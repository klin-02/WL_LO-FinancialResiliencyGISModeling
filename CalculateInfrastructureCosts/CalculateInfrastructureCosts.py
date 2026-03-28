import geopandas as gp
from pathlib import Path
import matplotlib.pyplot
import pandas as pd
import shapely
import matplotlib
from matplotlib import pyplot as plt
matplotlib.use("qt5agg",force=True)

'''
Data processing script I did in 3 hours lmao
'''

#initialize directory
dir = Path(__file__).resolve().parent

'''
obtained from the West Linn annual financial report
I subtract a bit of cost from the infrastructure asset cost 
#because I don't count drainage systems
'''
infrastructureAssetCost = 50000000
utilityAssetCost = 111000000

oregonCensusBlocksDataPath = dir / "CensusBlocksOregon.zip"
cityBoundsDataPath = dir / "WestLinnBoundaries.geojson"
roadsDataPath = dir / "WestLinnRoads(Validated&Segmented).geojson"
lotsDataPath = dir / "WestLinnDatas.geojson"
waterMainsData = dir / "WaterMains.geojson"
waterLateralsData = dir / "WaterLaterals.geojson"
utilitiesDataList = [ waterMainsData, waterLateralsData ]

oregonCensusBlocksDataFrame = gp.read_file(oregonCensusBlocksDataPath, use_arrow=True)
cityBoundsDataFrame = gp.read_file(cityBoundsDataPath, use_arrow=True)
roadsDataFrame = gp.read_file(roadsDataPath, use_arrow=True)
lotsDataFrame = gp.read_file(lotsDataPath, use_arrow=True)

#concatenate utilities data together - ignore index because that info is useless
utilitiesDataFrame = gp.GeoDataFrame(pd.concat([gp.read_file(path, use_arrow=True) 
    for path in utilitiesDataList], ignore_index=True), crs=4326)

#get rid of unneeded columns
oregonCensusBlocksDataFrame = oregonCensusBlocksDataFrame[["geometry", "POP20", "OBJECTID_1"]]
cityBoundsDataFrame = cityBoundsDataFrame[["geometry"]]
roadsDataFrame = roadsDataFrame[["geometry"]]
utilitiesDataFrame = utilitiesDataFrame[["geometry"]]


oregonCensusBlocksDataFrame = oregonCensusBlocksDataFrame.to_crs(epsg=2913)
cityBoundsDataFrame = cityBoundsDataFrame.to_crs(epsg=2913)
roadsDataFrame = roadsDataFrame.to_crs(epsg=2913)
lotsDataFrame = lotsDataFrame.to_crs(epsg=2913)
utilitiesDataFrame = utilitiesDataFrame.to_crs(epsg=2913)

#get census blocks datas for only West Linn
westLinnCensusBlocksDataFrame = oregonCensusBlocksDataFrame.sjoin(cityBoundsDataFrame)

westLinnCensusBlocksDataFrame = westLinnCensusBlocksDataFrame[["geometry", "POP20", "OBJECTID_1"]]

print(westLinnCensusBlocksDataFrame.shape[0])

#fix invalid geometries
roadsDataFrame['geometry'] = roadsDataFrame['geometry'].apply(lambda geom : geom if geom.is_valid else geom.buffer(0))
#remove z coordinates
roadsDataFrame['geometry'] = roadsDataFrame['geometry'].apply(lambda geom : shapely.force_2d(geom))

'''
get rid of repeat polygons by consilidating everything into a larger polygon
also helps make the spatial join a little less painful
'''
roadsDataFrame = roadsDataFrame.dissolve()
utilitiesDataFrame = utilitiesDataFrame.dissolve()

print(roadsDataFrame.shape[0])
print(utilitiesDataFrame.shape[0])

roadsDataFrame.plot(color="gray")
plt.show()

cityBoundsDataFrame.plot(color="lightblue")
plt.show()

#calculate costs per sq m for roads metrics (epsg 2913 is in sq ft so there needs a conversion)
roadsArea = roadsDataFrame.geometry.area
infrastructureCostPerSqMeter = infrastructureAssetCost / (roadsArea * 0.09290304)

#I haven't matched the utilities to the bounds of west linn yet, so I'll do it here
utilitiesDataFrame = utilitiesDataFrame.overlay(cityBoundsDataFrame)

#calculate costs per km for utility pipes - they don't have variability in width and pavement
#convert to km prior to processing - epsg 2913 is in ft
networkLengthInKm = utilitiesDataFrame.geometry.length * 0.0003048
utilityCostPerKm = utilityAssetCost / networkLengthInKm

print(infrastructureCostPerSqMeter)
print(utilityCostPerKm)

'''
Find census block net present value/acre and building footprint
This is for statistical analysis and reference
'''
#lots centroids make this a bit easier - get rid of duplicate overlap issues
lotsDataFrame = lotsDataFrame.set_geometry(lotsDataFrame.geometry.centroid)

joinedData = westLinnCensusBlocksDataFrame.sjoin(lotsDataFrame)
joinedData = joinedData.set_crs(epsg=2913)
joinedData["census lot building footprint"] = joinedData.apply(lambda row :
    (lotsDataFrame.iloc[row["index_right"]]["lot size (hectare)"]
    * lotsDataFrame.iloc[row["index_right"]]["building footprint"])
    / (row.geometry.area * 0.00000929) #hectare conversion really quick
    , axis=1
    )
joinedData["census lot total value"] = joinedData.apply(lambda row :
    lotsDataFrame.iloc[row["index_right"]]["net present value/hectare ($)"]
    * lotsDataFrame.iloc[row["index_right"]]["lot size (hectare)"]
    , axis=1
    )
joinedData = joinedData.drop(["index_right"], axis=1)

#drop unneeded columns to make the aggregate function work
joined = joinedData[["census lot building footprint", "census lot total value", "geometry", "POP20", "OBJECTID_1"]]

#then aggregate
aggregateFunctions = { 
    'census lot building footprint': 'sum',
    'census lot total value': 'sum',
    'geometry': 'first',
    'POP20': 'first',
    'OBJECTID_1': 'first'
    }

joinedData = joinedData.groupby("OBJECTID_1", as_index=False).agg(aggregateFunctions)

#put back into geodataframe
joinedData = gp.GeoDataFrame(joinedData, geometry="geometry")

#finish census lot net present value per acre calculation
#don't forget ft to hectare conversion :D
joinedData["census lot net present value/hectare ($)"] = joinedData["census lot total value"] / (joinedData.geometry.area * 0.00000929)

joinedData = joinedData.drop(["OBJECTID_1", "census lot total value"], axis=1)

joinedData.plot(color="lightblue", edgecolor="black")

#join with road data
joinedData = joinedData.sjoin(roadsDataFrame)

#calculate road costs
joinedData["road costs"] = joinedData.apply(lambda row :
    row.geometry.intersection(roadsDataFrame.geometry.iloc[0]).area * 0.09290304 #convert to sq m really quick
    * infrastructureCostPerSqMeter
    , axis=1
    )

joinedData = joinedData.drop(["index_right"], axis=1)

#join with utilities data
joinedData = joinedData.sjoin(utilitiesDataFrame)

#calculate utility costs
joinedData["utility costs"] = joinedData.apply(lambda row :
    row.geometry.intersection(utilitiesDataFrame.geometry.iloc[0]).length
    * 0.0003048 #convert to km really quick
    * utilityCostPerKm
    , axis=1
    )

#adjust by built out area
joinedData["infrastructure cost/building area ($)"] = (joinedData["utility costs"] + joinedData["road costs"]) / (joinedData.geometry.area * 0.00000929 * joinedData["census lot building footprint"])

joinedData = joinedData.drop(["index_right", "utility costs", "road costs"], axis=1)

joinedData = joinedData.set_crs(epsg=2913, allow_override=True)
joinedData = joinedData.to_crs(epsg=4326)

joinedData.to_file("WL_InfrastructureData.geojson", driver="GeoJSON")
joinedData.to_file("WL_InfrastructureData.gpkg", layer="WL Infrastructure Costs", driver="gpkg")