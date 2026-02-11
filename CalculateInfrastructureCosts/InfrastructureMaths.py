from math import e
'''
adjust for maintenance costs depending on intensity of usage 
intensity is assumed from building footprint because more building -> more usage of infrastructure
bounds are based on lifetime estimates for road and utility infrastructure
roads are based on an exponential decay function because greater density means more micromobility
utilities are also based on the same exponential decay function because denser buildings are more energy efficient
'''

#asymptote at 1.43 and y-int at 0.57 (y-int is derived from asymptote - constant)
def ExponentialDecayMaintenanceAdjuster(blockBuildingFootprint):
    return 1.43 - 0.86 * e ** (-5 * blockBuildingFootprint)