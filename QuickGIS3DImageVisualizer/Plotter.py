import geopandas as gp
from matplotlib.colors import _ColorMapping
from mpl_toolkits.mplot3d import Axes3D
from matplotlib import pyplot as plt
import numpy as np

from mpl_toolkits.mplot3d.art3d import Line3DCollection, Poly3DCollection

class Plotter:
    def __init__(self, zCoordIncrement, zStartValue):
        self.increment = zCoordIncrement
        self.startValue = zStartValue

        fig = plt.figure()
        ax = fig.add_subplot(projection='3d')
        ax.view_init(elev=20)
        self.fig = fig
        self.ax = ax

    def Plot3D(self, *gdfList):
        z = self.startValue

        for gdf in gdfList:
            gdf = gp.GeoDataFrame(gdf)

            self.__AddToGraph(gdf, z)
            z += self.increment

        self.ax.set_zlim(0, z + 100)
        plt.show()  

    def __AddToGraph(self, gdf, zValue):
        gdf = gp.GeoDataFrame(gdf)

        for geom in gdf.geometry:
            polygons = self.__CheckGeometry(geom)

            for poly in polygons:
                x, y = poly.exterior.xy
                z = np.empty(len(x))
                z.fill(zValue)
                
                verticies = [list(zip(x, y, z))]

                collection = Poly3DCollection(verticies, alpha=0.8)
                self.ax.add_collection3d(collection)


    def __CheckGeometry(self, geom):
        if (geom.geom_type == "Polygon"):
            return list([geom])
        elif (geom.geom_type == "MultiPolygon"):
            return list(geom.geoms)
        else:
            raise TypeError("Only Polygons and Multipolygons supported")
