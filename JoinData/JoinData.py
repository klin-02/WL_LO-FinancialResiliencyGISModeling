import geopandas as gp
import numpy as np
import pandas as pd
from pathlib import Path
import json
from shapely.geometry.polygon import Polygon
from shapely.geometry.multipolygon import MultiPolygon
import Functions_WLZoning as WLFuncs
import SpeedySpatialProcessors as ssp
import matplotlib
from re import sub
from decimal import Decimal

def main():
    #gets the directory
    dir = Path(__file__).resolve().parent

    #join directory & file path together
    taxlotPath = dir / "WL_LotsDatas(Metric).geojson"
    footprintsPath = dir / "WL_BuildingFootprints(Fixed).geojson"
    zoningPath = dir / "Zoning.zip"
    boundsPath = dir / "WestLinnBoundaries.geojson"

    #read line-delimited geojson into geoframe 
    clackamasDataList = []

    with open(taxlotPath, encoding='utf-8-sig') as file:
        for line in file:
            dict = json.loads(line)
            clackamasDataList.append(dict)


    clackamasGeoDataFrame = gp.GeoDataFrame.from_features(clackamasDataList)

    footprintsData = gp.read_file(footprintsPath, use_arrow=True)
    zoningData = gp.read_file(zoningPath, use_arrow=True)
    boundsData = gp.read_file(boundsPath, use_arrow=True)

    '''
    reproject since geopandas is funny with reference systems
    also get rid of unneeded columns
    '''
    footprintsData = footprintsData[["geometry"]]
    zoningData = zoningData[["geometry", "ZONE"]]
    boundsData = boundsData[["geometry"]]
    footprintsData = footprintsData.to_crs(epsg=2913)
    clackamasGeoDataFrame = clackamasGeoDataFrame.set_crs(epsg=2913)
    zoningData = zoningData.set_crs(epsg=2913)
    boundsData = boundsData.to_crs(epsg=2913)

    print(clackamasGeoDataFrame.head)
    print(footprintsData.head)
    print(zoningData.head)

    #address data may have been called twice so get rid of duplicates
    clackamasGeoDataFrame = clackamasGeoDataFrame.drop_duplicates(["geometry"], keep="first")
    clackamasGeoDataFrame = clackamasGeoDataFrame.drop_duplicates(["address"], keep="first")

    print(clackamasGeoDataFrame.shape[0])

    #cast footprint datas into multipolygon for the overlay operation to work
    #overlay needs all the same geometry type
    #also because overlay is slow overlay with WL bounds really quick before both processings
    footprintsData = footprintsData.overlay(boundsData, how="intersection")
    footprintsData["geometry"] = [MultiPolygon([feature]) if isinstance(feature, Polygon) \
        else feature for feature in footprintsData["geometry"]]

    print(footprintsData.head)

    #preprocess to only get intersecting footprints
    #speed up overlay operation
    footprintsData = footprintsData[["geometry"]]

    #get individual intersection polygons between building footprints and lots
    buildingIntersections = ssp.MultithreadedOverlay(clackamasGeoDataFrame, footprintsData)

    buildingIntersections["intersection areas"] = buildingIntersections.geometry.area

    #overlay is a little bad at not having duplicate geometries so get rid of them
    buildingIntersections = buildingIntersections.drop_duplicates(["geometry"], keep="first")

    print(buildingIntersections.shape[0])

    print(buildingIntersections.head)

    #aggregate datas on the same lot
    aggregateFunctions = { 
        'intersection areas': 'sum',
        'geometry': 'first',
        'land use': 'first',
        'lot size (hectare)': 'first',
        'net present value/hectare ($)': 'first'
        }

    buildingIntersections = buildingIntersections.groupby("address", as_index=False).agg(aggregateFunctions)

    #get rid of unneeded columns
    buildingIntersections = buildingIntersections[["address", "intersection areas"]]

    #merge data together
    clackamasGeoDataFrame = clackamasGeoDataFrame.merge(buildingIntersections, on="address")

    #turn back into geodataframe
    clackamasGeoDataFrame = gp.GeoDataFrame(clackamasGeoDataFrame)

    clackamasGeoDataFrame.plot(color="lightblue", edgecolor="black")

    #calculate building footprints
    clackamasGeoDataFrame["building footprint"] = round(clackamasGeoDataFrame["intersection areas"] / clackamasGeoDataFrame.geometry.area, 3)

    #sjoin to assign zoning to each property
    clackamasGeoDataFrame = clackamasGeoDataFrame.sjoin(zoningData)

    '''
    taxlot bounds aren't perfect, so sjoin may inadvertly indicate that a taxlot intersects two zones
    this will help find which zone is intersected the most to get the correct zoning
    '''
    clackamasGeoDataFrame["zoningIntersections"] = clackamasGeoDataFrame.apply(
        lambda row : row.geometry.intersection(zoningData.iloc[row["index_right"]].geometry).area
        , axis=1
        )
    '''
    sort by largest to smallest to put the highest intersections on top
    get rid of the smaller intersections by dropping anything below the top intersection
    '''
    clackamasGeoDataFrame = clackamasGeoDataFrame.sort_values("zoningIntersections", ascending=False) \
        .drop_duplicates("address", keep="first")

    #'quantify' zoning so I can map it in arcgis
    clackamasGeoDataFrame["zoning liberties index"] = clackamasGeoDataFrame.apply(lambda row :
        WLFuncs.QuantifyZoning(row["ZONE"], row["building footprint"])
        , axis=1
        )

    clackamasGeoDataFrame = clackamasGeoDataFrame.drop(["index_right", "zoningIntersections", "intersection areas"], axis=1)

    clackamasGeoDataFrame = clackamasGeoDataFrame.to_crs(epsg=4326)

    #get rid of currency formatting because it crashes arcgis
    clackamasGeoDataFrame["net present value/hectare ($)"] = clackamasGeoDataFrame.apply(lambda row :
        Decimal(sub(r'[^\d.]', '', row["net present value/hectare ($)"]))
        , axis=1
        )

    print(clackamasGeoDataFrame.shape[0])

    clackamasGeoDataFrame.to_file("WestLinnLandValueDatas.gpkg", layer="West Linn Lots", driver="GPKG")
    clackamasGeoDataFrame.to_file("WestLinnDatas.geojson", driver="GeoJSON")

if (__name__ == "__main__"):
    matplotlib.use("qt5agg",force=True)
    main()
