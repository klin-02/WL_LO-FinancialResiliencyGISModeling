import numpy as np
import multiprocessing as mp
import geopandas as gp
from pandas import concat, Series

'''
multithreaded overlay since gp overlay is slow
splits up df1 into chunks and parallel processes each against df2
will slow down the rest of your computer - be warned!
'''
def MultithreadedOverlay(df1, df2) -> gp.GeoDataFrame:
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

