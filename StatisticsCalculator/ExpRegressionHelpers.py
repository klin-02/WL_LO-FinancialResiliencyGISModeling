'''
from shapely import linear
from statsmodels.regression.linear_model import RegressionResultsWrapper
import statsmodels.formula.api as smf
import pandas as pd
from matplotlib import pyplot as plt
from math import e
from numpy import linspace


First performs least squares regression on an x value and a log transformed y value
Then rewrites in exponential decay form and plots
This is to capture the exponential trends of some of my data

def RunExpRegress(data, xName, yName, linearizedColName, graphTitle):
    df = pd.DataFrame(data)
    
    #Run least squares regression between x and linearlized column first
    result = smf.ols(formula=f"Q('{linearizedColName}')~Q('{xName}')", data=df).fit()
    print(result.summary())

    df["model prediction"] = result.fittedvalues
    basemap = df.plot(x=xName, y=linearizedColName, kind="scatter")
    df.plot(x=xName, y="model prediction", ax=basemap, kind="line", color="red")
    plt.show()

    m = result.params["Q('census lot building footprint')"]
    intercept = result.params['Intercept']

    #get exponential/logarithmic regression line
    exponentialEqParams = __ConvertLinearToExponential(m, intercept)
    a = exponentialEqParams[0]
    b = exponentialEqParams[1]

    #get data for the exponential line of best fit then plot the exp regression
    x = linspace(-0.15, 0.5, 6000)
    y = a * b ** x

    basemap = df.plot(x=xName, y=yName, kind="scatter")
    basemap.plot(x, y, data=df, color="red")
    plt.title(graphTitle)
    plt.show()


Finds the parameters of the exponential line of best fit from a linear line of best fit
Does this by exponentiating with euler's number

def __ConvertLinearToExponential(m, intercept) -> list:
    a = e ** intercept
    b = e ** m
    
    print(f"Equation: y={a}*{b}^x")
    return [ a, b ]
'''