import geopandas as gp

def DropDuplicatesWithSamePoints(gdf) -> gp.GeoDataFrame:
    gdf = gp.GeoDataFrame(gdf)

    #make sure coords order is good
    gdf["geometry"] = gdf.normalize()
    gdf["wkt"] = gdf.geometry.to_wkt()

    gdf = gdf.drop_duplicates("wkt")
    return gdf
