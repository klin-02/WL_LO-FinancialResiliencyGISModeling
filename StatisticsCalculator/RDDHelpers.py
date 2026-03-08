import pandas as pd
import numpy as np
import statsmodels.formula.api as smf
from statsmodels.regression.linear_model import RegressionResults, RegressionResultsWrapper
import statsmodels.stats.weightstats as ws
import matplotlib.pyplot as plt
from math import e

'''
Takes in the data, the name of the distance column, and the y variable name
IMPORTANT: Assumes that the cusp is at 0
'''
def RunGeoRDD(data, distanceColName, yName, geoRDDGraphTitle):
    data = pd.DataFrame(data)
    distanceColName = str(distanceColName)
    yName = str(yName)

    #get only properties within 0.22 km of a zoning border because many high density zones are quite small
    #also ensures that we are only analyzing the properties with the most similar services, amenities, and desirability due to short geographic distance
    #the same reason is why I apply a distance decay-based exponential kernel as well
    #it means no confounding variables! we're effectively just comparing zoning and our y variable
    data = data[(np.abs(data[distanceColName]) <= 0.22)]
    weights = __ExponentialKernelEstimation(data[distanceColName], -2.5, 1)

    #split dataset and weights into treatment (high density zoning) and control (low density zoning)
    treatmentFrame = data[(data[distanceColName] > 0)]
    controlFrame = data[((data[distanceColName]) < 0)]
    treatmentWeights = weights[data[distanceColName] > 0]
    controlWeights = weights[data[distanceColName] < 0]

    #get rid of outliers
    treatmentFrame = __EliminateOutliers(treatmentFrame, yName, treatmentWeights)
    controlFrame = __EliminateOutliers(controlFrame, yName, controlWeights)

    #concatenate data back together
    data = pd.concat([treatmentFrame, controlFrame], ignore_index=True)

    #assign threshold so statsmodel can do regressions in quadrant 1 and 2
    data = data.assign(threshold=(data[distanceColName] > 0).astype(int))

    #redo weights after outliers cleared to steady the index
    weights = __ExponentialKernelEstimation(data[distanceColName], -2.5, 1)

    #run GeoRDD via weighted least squares using my exponential kernel model
    #take in two x variables via the patsy syntax of *
    #two regressions will then be run, and via .summary, I can identify the RDD relationship
    #put the variables in quotes too to account for special characters
    result = smf.wls(formula=f"Q('{yName}')~Q('{distanceColName}')*threshold", data=data, weights=weights).fit()
    print(result.summary())

    #create new column for the regression y values so it can be plotted
    data["model prediction"] = result.fittedvalues

    data = data.sort_values(distanceColName)

    #plot
    basemap = data.plot(x=distanceColName, y=yName, kind="scatter")
    data.plot(x=distanceColName, y="model prediction", ax=basemap, color="red")
    plt.title(geoRDDGraphTitle)
    plt.show()

    input("Press enter to continue: ")

'''
some data points can be unusually high
this is usually the result of historical buildings that are high density and create a lot of value
I eliminate these outliers using interquartle ranges on both sides of the graph
'''
def __EliminateOutliers(data, yName, weights) -> pd.DataFrame:  
    data = pd.DataFrame(data)
    
    descriptiveStats = ws.DescrStatsW(data[yName], weights)
    quartiles = descriptiveStats.quantile([0.25, 0.75], return_pandas=False)

    Q1 = quartiles[0]
    Q3 = quartiles[1]
    IQR = Q3 - Q1

    lower = Q1 - (1.5 * IQR)
    upper = Q3 + (1.5 * IQR)

    print(f"Q1: {Q1}, Q3: {Q3}, IQR: {IQR}")
    print(f"lower: {lower}, upper: {upper}")

    data = data[(data[yName] < upper)]
    data = data[(data[yName] > lower)]

    return data

'''
Give higher weight to data points near the cusp using a distance decay function
Takes in the distance data list (which should be in kilometers), vertical streching factor and a max for the cusp
The vertical stretching factor can be modified to give more or less weight to properties away from the cusp
Experiment! <|:)
'''
def __ExponentialKernelEstimation(distanceDataListInKm, stretchingFactor, yMax) -> np.float64:
    return yMax * e ** (stretchingFactor * np.abs(distanceDataListInKm))