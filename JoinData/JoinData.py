from typing import final
import geopandas as gp
import numpy as np
import pandas as pd
from pathlib import Path
import json
import matplotlib
from decimal import Decimal

def main():
    import CalculateBuildingFootprint as cbf
    import FixTaxlots as ft
    import ConfigureZoning as cz

    #gets the directory
    dir = Path(__file__).resolve().parent

    #join directory & file path together
    taxlotPath = dir / "LotsData(initial).geojson"
    footprintsPath = dir / "WL_BuildingFootprints(Fixed).geojson"
    zoningPath = dir / "Zoning.zip"
    boundsPath = dir / "WestLinnBoundaries.geojson"
    fixedTaxlotsPath = dir / "WestLinnLots(Fixed).geojson"

    clackamasGeoDataFrame = gp.read_file(taxlotPath, use_arrow=True)
    footprintsData = gp.read_file(footprintsPath, use_arrow=True)
    zoningData = gp.read_file(zoningPath, use_arrow=True)
    boundsData = gp.read_file(boundsPath, use_arrow=True)
    fixedLotsPolygons = gp.read_file(fixedTaxlotsPath, use_arrow=True)

    '''
    reproject since geopandas is funny with reference systems
    also get rid of unneeded columns
    '''
    footprintsData = footprintsData[["geometry"]]
    zoningData = zoningData[["geometry", "ZONE", "FID"]]
    boundsData = boundsData[["geometry"]]
    fixedLotsPolygons = fixedLotsPolygons[["geometry"]]
    footprintsData = footprintsData.to_crs(epsg=2913)
    clackamasGeoDataFrame = clackamasGeoDataFrame.to_crs(epsg=2913)
    zoningData = zoningData.to_crs(epsg=2913)
    boundsData = boundsData.to_crs(epsg=2913)
    fixedLotsPolygons = fixedLotsPolygons.to_crs(epsg=2913)

    clackamasGeoDataFrame.plot(color="lightblue", edgecolor="black")

    finalTaxlotsFrame = ft.FixTaxlotsData(clackamasGeoDataFrame, fixedLotsPolygons)
    finalTaxlotsFrame = cbf.CalculateFootprints(finalTaxlotsFrame, footprintsData, boundsData)
    finalTaxlotsFrame = cz.AssignZones(finalTaxlotsFrame, zoningData)
    finalTaxlotsFrame = cz.QuantifyZoning(finalTaxlotsFrame, zoningData)

    finalTaxlotsFrame = finalTaxlotsFrame.to_crs(epsg=4326)

    finalTaxlotsFrame.to_file("WestLinnLandValueDatas.gpkg", layer="West Linn Lots", driver="GPKG")
    finalTaxlotsFrame.to_file("WestLinnDatas.geojson", driver="GeoJSON")

if (__name__ == "__main__"):
    matplotlib.use("qt5agg",force=True)
    main()
