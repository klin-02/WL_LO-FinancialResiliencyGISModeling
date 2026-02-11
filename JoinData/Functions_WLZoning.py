from numpy import float64

#quantify zoning in order of how liberalizing it is and such
#check out this if you want: https://westlinnoregon.gov/sites/default/files/fileattachments/building/page/7171/zoning_rev..12.30_0.pdf
WL_zoningResDict = {
    "MU": 10,
    "R2.1": 9,
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

'''
commercial units are a little ambigious
in the case of a non-residential mixed use building
I will adjust the liberalness by density of the building itself
'''
def QuantifyZoning(code, building_footprint) -> float64:
    if (code in WL_zoningResDict):
        return WL_zoningResDict[code]
    elif(code in WL_zoningComList):
        return __CommercialZoningLiberalness(building_footprint)
    else:
        return None


def __CommercialZoningLiberalness(building_footprint) -> float64:
    #low density commercial
    if (building_footprint < 0.3):
        return 2
    #low-ish mid density commercial
    elif (building_footprint < 0.500):
        return 4
    #high-ish mid density commercial
    elif (building_footprint < 0.7):
        return 6
    #high density commercial
    else:
        return 10