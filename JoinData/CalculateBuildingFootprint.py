from turtle import fillcolor
import numpy as np
import multiprocessing as mp
import geopandas as gp
from pandas import concat, Series
from shapely import MultiPolygon, Polygon

import GeomHelpers as gh

def CalculateFootprints(lotsFrame, footprintsFrame, boundsFrame) -> gp.GeoDataFrame:
    #cast footprint datas and taxlot datas into multipolygons for the overlay operation to work
    #overlay needs all the same geometry type
    #also because overlay is slow sjoin with WL bounds really quick before both processings
    footprintsFrame = footprintsFrame.sjoin(boundsFrame)
    footprintsFrame = footprintsFrame.drop(["index_right"], axis=1)
    footprintsFrame["geometry"] = [MultiPolygon([feature]) if isinstance(feature, Polygon) \
        else feature for feature in footprintsFrame["geometry"]]
    lotsFrame["geometry"] = [MultiPolygon([feature]) if isinstance(feature, Polygon) \
        else feature for feature in lotsFrame["geometry"]]

    #assign index to get accurate building footprints for lots with multiple buildings (via aggregation
    lotsFrame["ID"] = lotsFrame.index

    #preprocess to only get intersecting footprints
    footprintsFrame = footprintsFrame[["geometry"]]

    #get individual intersection polygons between building footprints and lots
    buildingIntersections = __MultithreadedOverlay(lotsFrame, footprintsFrame)

    #overlay is a little bad at not having duplicate geometries so get rid of them
    buildingIntersections = gh.DropDuplicatesWithSamePoints(buildingIntersections)

    buildingIntersections["intersection areas"] = buildingIntersections.geometry.area

    #aggregate datas on the same lot
    aggregateFunctions = { 
        'intersection areas': 'sum',
        'geometry': 'first',
        'land use': 'first',
        'lot size (hectare)': 'first',
        'net present value/hectare ($)': 'first',
        'address': 'first'
        }

    buildingIntersections = buildingIntersections.groupby("ID", as_index=False).agg(aggregateFunctions)

    #get rid of unneeded columns
    buildingIntersections = buildingIntersections[["intersection areas", "ID"]]

    #merge data together
    lotsFrame = lotsFrame.merge(buildingIntersections, on="ID")

    #turn back into geodataframe
    lotsFrame = gp.GeoDataFrame(lotsFrame)

    #calculate building footprints
    lotsFrame["building footprint"] = lotsFrame["intersection areas"] / lotsFrame.geometry.area

    lotsFrame = lotsFrame.drop(["ID", "intersection areas"], axis=1)
    return lotsFrame

'''
multithreaded overlay since gp overlay is slow
splits up df1 into chunks and parallel processes each against df2
will slow down the rest of your computer - be warned!
'''
def __MultithreadedOverlay(df1, df2) -> gp.GeoDataFrame:
    #determine CPU core counts
    coreCount = mp.cpu_count()
    
    #split datas
    dataChunks = np.array_split(df1, coreCount)

    #initialize threads
    pool = mp.Pool(coreCount)

    #overlay each data
    processes = [pool.apply_async(gp.overlay, args=(data, df2, "intersection"))
        for data in dataChunks]

    #get list of datas
    results = [process.get() for process in processes]

    #concatenate all processes and return the data
    return gp.GeoDataFrame(concat(results), crs = df1.crs)
