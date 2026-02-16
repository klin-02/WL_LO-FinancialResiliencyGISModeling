import geopandas as gp
import pandas as pd
from shapely.wkt import loads
from numpy import arange, float64
from re import sub

import GeomHelpers as gh

'''
Some taxlot data does not accurately reflect the private ownership boundaries
This causes some data to have insanely high building footprints and values
This fixes this by sjoining the initial lots data
to a dataset with polygons that have these errors fixed manually by me
'''
def FixTaxlotsData(initialFrame, fixedFrame) -> gp.GeoDataFrame:
    initialFrame = gp.GeoDataFrame(initialFrame)
    fixedFrame = gp.GeoDataFrame(fixedFrame)

    #get rid of duplicate geometries right over each other (drop_duplicates gets rid of geometries that are of the same proportions, which is bad)
    initialFrame = gh.DropDuplicatesWithSamePoints(initialFrame)
    fixedFrame = gh.DropDuplicatesWithSamePoints(fixedFrame)

    #turn into centroids to prevent double overlaps
    initialFrame = initialFrame.set_geometry(initialFrame.geometry.centroid)

    #set an id because geometry is not reliable for groupby operations - it only compares shapes
    fixedFrame["ID"] = fixedFrame.index

    joinedFrame = fixedFrame.sjoin(initialFrame)
    joinedFrame.drop(["index_right"], axis=1)

    #combine data for lots with multiple points inside them
    joinedFrame = joinedFrame.groupby("ID", as_index=False).apply(__CombineData)

    joinedFrame = gp.GeoDataFrame(joinedFrame, geometry="geometry")

    joinedFrame = joinedFrame.set_crs(epsg=2913)
    return joinedFrame

'''
Function for fixing lots data
'''
def __CombineData(group) -> gp.GeoDataFrame:
    gdf = gp.GeoDataFrame({"geometry": [None]}, geometry="geometry")
    group = gp.GeoDataFrame(group)

    if (len(group) > 1):
        gdf["address"] = "NO SITUS ADDRESS, OR 97068"
    else:
        gdf["address"] = group.iloc[0]["address"]

    gdf["land use"] = group.iloc[0]["land use"]
    
    #get rid of money formatting
    group["net present value/hectare ($)"] = group["net present value/hectare ($)"].str.replace("$", "")
    group["net present value/hectare ($)"] = group["net present value/hectare ($)"].str.replace(",", "")
    group["net present value/hectare ($)"] = group["net present value/hectare ($)"].astype(float64)

    #reverse engineer out the total value then divide it by the area of the fixed lot polygon
    #this ensures accurate net present value per acres
    #also do lot size and geometry quickly beforehand (this will find the area of the fixed lot polygon)
    gdf["lot size (hectare)"] = group.iloc[0]["geometry"].area * 0.0000093
    gdf["geometry"] = group.iloc[0]["geometry"]

    totalValue = 0.00
    for col, row in group.iterrows():
        totalValue += row["net present value/hectare ($)"] * row["lot size (hectare)"]

    gdf["net present value/hectare ($)"] = totalValue / gdf["lot size (hectare)"]
    return gdf
