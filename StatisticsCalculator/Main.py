import geopandas as gp
from pathlib import Path
import matplotlib

def main():
    import StatsService as ss

    dir = Path(__file__).resolve().parent

    lotsDataPath = dir / "WestLinnDatas.geojson"
    zoningDataPath = dir / "Zoning.zip"

    lotsFrame = gp.read_file(lotsDataPath, use_arrow=True)
    zoningFrame = gp.read_file(zoningDataPath, use_arrow=True)

    #set a local crs because the units are a little easier to work with
    lotsFrame = lotsFrame.to_crs(epsg=2913)
    zoningFrame = zoningFrame.to_crs(epsg=2913)

    #analyze!
    #ss.CreateLotsZoningHistogram(lotsFrame)
    #ss.GeoRDDAnalysis(lotsFrame, zoningFrame)
    ss.RandomForestAnalysis(lotsFrame)

if __name__ == "__main__":
    matplotlib.use("qt5agg",force=True)
    main()
