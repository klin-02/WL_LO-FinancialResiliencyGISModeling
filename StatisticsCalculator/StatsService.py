import pandas as pd
import geopandas as gp
import matplotlib.pyplot as plt
from numpy import float64

'''
Evaluate causative effects between zoning and municipal finance
Do this via geographic regression discontinuity design
Distance serves as the running variable and 
'''
def GeoRDDAnalysis(df1, zoningData):
    import RDDHelpers as rdd

    df1 = gp.GeoDataFrame(df1)
    zoningData = gp.GeoDataFrame(zoningData)

    #set data points to centroids to avoid weird overlaps between multiple zoning boundaries >:C
    #also makes distance calculations a little less ambigious
    lotsCentroids = df1.set_geometry(df1.centroid)

    #get lots in high density (laxer) and lower density (stricter)
    #based on a zoning liberties index I calculated previously
    laxerLots = lotsCentroids[lotsCentroids["zoning liberties index"] > 5]
    stricterLots = lotsCentroids[lotsCentroids["zoning liberties index"] < 5]

    #get geodataframes with only geometries to make the sjoin less painful
    laxGeometries = laxerLots[["geometry"]]
    strictGeometries = stricterLots[["geometry"]]

    #get the high density (laxer) and the low density (stricter) zone polygons
    laxerZoningFrame = zoningData.sjoin(laxerLots, predicate="contains")
    laxerZoningFrame = laxerZoningFrame.drop(["index_right"], axis=1)
    stricterZoningFrame = zoningData.sjoin(strictGeometries, predicate="contains")
    stricterZoningFrame = stricterZoningFrame.drop(["index_right"], axis=1)

    #dissolve each set of zoning polygons into one to ensure that there is a singular geographic 'cusp'
    #this simplifies the calculation of the running variable in GeoRDD (distance) a lot
    laxerZoningFrame = laxerZoningFrame.geometry.union_all()
    stricterZoningFrame = stricterZoningFrame.geometry.union_all()

    #calc distance between low density properties and high density zoning (convert ft to kilometers too because metric)
    #multiply values by -1 to make GeoRDD easier (because two lines on the same graph are nice bc high density can just go on the left & low density on the right)
    laxerLots["dist. to notable zoning border (km)"] = laxerLots.distance(stricterZoningFrame) * -0.0003048

    #calc distance of high density properties to low density zoning (ft to kilometers conversions here too)
    stricterLots["dist. to notable zoning border (km)"] = stricterLots.distance(laxerZoningFrame) * 0.0003048
    
    #concat datas together
    #conveniently turns it into a pandas df for GeoRDD analysis. sweet!
    processedLotsFrame = pd.concat([laxerLots, stricterLots], ignore_index=True)
    
    processedLotsFrame["net present value/hectare ($)"] = processedLotsFrame["net present value/hectare ($)"].astype(float64)

    #run :D
    rdd.RunGeoRDD(processedLotsFrame, "dist. to notable zoning border (km)", "net present value/hectare ($)", "Zoning and Net Present Value/Hectare (GeoRDD)")
    rdd.RunGeoRDD(processedLotsFrame, "dist. to notable zoning border (km)", "building footprint", "Zoning and Building Footprint (GeoRDD)")
    
'''
Look at the distributions of zoning
'''
def CreateLotsZoningHistogram(lotsFrame):
    lotsFrame["ZONE"].value_counts().plot.bar(title="Properties per Zone")
    plt.show()
