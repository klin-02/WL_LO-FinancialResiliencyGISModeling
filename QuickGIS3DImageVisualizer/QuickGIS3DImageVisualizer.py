import matplotlib
import geopandas as gp

def main():
    import Plotter as p

    boundsPath = r"WestLinnBoundaries.geojson"
    lotsPath = r"WestLinnLandValueDatas.gpkg"

    boundsGdf = gp.read_file(boundsPath, use_arrow=True)
    lotsGdf = gp.read_file(lotsPath, use_arrow=True)

    #local projection so the plot wont look weird
    boundsGdf = boundsGdf.to_crs(epsg=2913)
    lotsGdf = lotsGdf.to_crs(epsg=2913)

    plotter = p.Plotter(zCoordIncrement=1000000, zStartValue=100000)
    plotter.Plot3D(boundsGdf, lotsGdf)

if (__name__ == "__main__"):
    matplotlib.use("qt5agg",force=True)
    main()

