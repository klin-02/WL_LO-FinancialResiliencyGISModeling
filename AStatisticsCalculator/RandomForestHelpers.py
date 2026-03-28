import pandas as pd
from sklearn.ensemble import RandomForestRegressor
from sklearn.model_selection import GridSearchCV
from sklearn.metrics import mean_squared_error, r2_score, root_mean_squared_error
from sklearn.model_selection import train_test_split
from sklearn.tree import plot_tree
from math import sqrt, e
import matplotlib.pyplot as plt

'''
Random Forest Analysis to predict the amount of density needed to pay off school debt
Also helps capture non-linear trends within density vs. wealth creation
No outlier removal necessary because random forest is able to isolate them from partitioning and local model fitting 
'''
def DevelopRandomForestModel(data, xLabel, yLabel, graphTitle):
    data = pd.DataFrame(data)
    xLabel = str(xLabel)
    yLabel = str(yLabel)
    graphTitle = str(graphTitle)

    data.plot(x=xLabel, y=yLabel, kind="scatter")
    plt.show()

    #train test split of 70/30
    xTrain, xTest, yTrain, yTest = train_test_split(data[[xLabel]], data[yLabel], test_size=0.3, random_state=42)

    #optimize parameters with GridSearchCV
    gridParameters = {
        "n_estimators": [100, 300], #amount of 'voters'
        "max_depth": [5, 25, None], #node depth
        "min_samples_split": [2, 10], 
        "min_samples_leaf": [1, 3],
        "max_features": ["sqrt", "log2", None]
    }

    model = GridSearchCV(RandomForestRegressor(oob_score=True, random_state=42), gridParameters)
    model.fit(xTrain, yTrain)

    yPrediction = model.predict(xTest)

    #descriptive statistics evaluation
    meanSquaredError = mean_squared_error(yTest, yPrediction)
    rootMeanSquaredError = root_mean_squared_error(yTest, yPrediction) #average error in terms of how much it is off in percent (bc log transform)
    rSquared = r2_score(yTest, yPrediction) #amount of variance explained by the model

    print(meanSquaredError)
    print(rootMeanSquaredError)
    print(rSquared)
    print(model.best_estimator_.oob_score_)
    print(model.best_estimator_)

    #plot cool lines and the tree structure
    data["model prediction"] = model.predict(data[[xLabel]])
    
    #make x values in order so I don't get a crap ton of lines
    data = data.sort_values(xLabel)

    basemap = data.plot(x=xLabel, y=yLabel, kind="scatter")
    data.plot(x=xLabel, y="model prediction", ax=basemap, kind="line", color="green", linewidth=6)
    plt.ylabel(f"log({yLabel})")
    plt.title(graphTitle)
    plt.show()

    plot_tree(model.best_estimator_.estimators_[0], filled=True)
    plt.show()

    __PredictHypotheticals(model)


def __PredictHypotheticals(model):
    #print density predictions
    df = [[0.03], [0.191], [0.5]]
    df = pd.DataFrame(data=df, columns=["building footprint"])
    df["prediction"] = model.predict(df)

    for row, col in df.iterrows():
        print(row["building footprint"])
        print(row["prediction"])
    