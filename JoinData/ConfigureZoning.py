import geopandas as gp
import pandas as pd

def AssignZones(lotsFrame, zoningFrame) -> gp.GeoDataFrame:
    lotsFrame = gp.GeoDataFrame(lotsFrame)
    zoningFrame = gp.GeoDataFrame(zoningFrame)

    #get centroids to avoid the double overlap problem
    centroids = lotsFrame.set_geometry(lotsFrame.geometry.centroid)
    joinedFrame = centroids.sjoin(zoningFrame)
    
    #reset geometry
    joinedFrame = joinedFrame.set_geometry(lotsFrame.geometry)
    
    joinedFrame = joinedFrame.drop(["index_right", "FID"], axis=1)
    return joinedFrame

'''
Provide quantifiable values on zoning so I can do a Geographic Regression Discontinuity Design
These ensure that there is a distinct 'cusp' for causative inference
'''
def QuantifyZoning(lotsFrameWithZones, zoningFrame) -> gp.GeoDataFrame:
    lotsGdf = gp.GeoDataFrame(lotsFrameWithZones)
    zoningGdf = gp.GeoDataFrame(zoningFrame)

    #convert to centroid to avoid the double overlap problem (again lol)
    centroids = lotsGdf.set_geometry(lotsGdf.geometry.centroid)
    
    #the density are allowances under commercial zoning are a little ambigious
    #I fix this by inferring it through the average building footprint of the zone
    joinedFrame = zoningGdf.sjoin(centroids)
    joinedFrame = joinedFrame[["building footprint", "FID", "geometry"]]
    joinedFrame = joinedFrame.set_crs(epsg=2913, allow_override=True)

    joinedFrame = joinedFrame.dissolve(by="FID", aggfunc="mean")
    joinedFrame["zone average building footprint"] = joinedFrame["building footprint"]
    joinedFrame = joinedFrame.drop(["building footprint"], axis=1)
    print(joinedFrame.iloc[0])

    #combine data again (so the lots, or centroids are spatially aware of the zone average building footprint)
    joinedFrame = centroids.sjoin(joinedFrame)

    #quantify
    joinedFrame = __ZoneToValue(joinedFrame)

    #set geometry back to lots
    joinedFrame = joinedFrame.set_geometry(lotsGdf.geometry)
    return joinedFrame

'''
quantifies zoning in order of how liberalizing it is
check out this if you would like: https://westlinnoregon.gov/sites/default/files/fileattachments/building/page/7171/zoning_rev..12.30_0.pdf
the experimental quantified zoning values are all based off that
'''
def __ZoneToValue(lotsFrameWithZoneAndAverageFootprints) -> gp.GeoDataFrame:
    WL_zoningResDict = {
        "MU": 10,
        "R2.1": 8,
        "R3": 7,
        "R4.5": 6,
        "R5": 6,
        "R7": 4,
        "R10": 4,
        "R15": 2,
        "R20": 2,
        "R40": 1
    }
    WL_zoningComList = ["NC", "GC", "OBC"]

    gdf = gp.GeoDataFrame(lotsFrameWithZoneAndAverageFootprints)

    gdf["zoning liberties index"] = gdf.apply(lambda row : 
        WL_zoningResDict[row["ZONE"]] if (row["ZONE"] in WL_zoningResDict) 
        else __CommercialZoningLiberalness(row["zone average building footprint"])
        , axis=1)

    gdf = gdf.drop(["zone average building footprint", "FID"], axis=1)
    return gdf

'''
Commercial zoning is a little ambigious
I therefore determine it using the average building footprint of these zones
'''
def __CommercialZoningLiberalness(zoneAverageFootprint) -> int:
    average = float(zoneAverageFootprint)

    if (average < 0.35):
        return 4
    else:
        return 8