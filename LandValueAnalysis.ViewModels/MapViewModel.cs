using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using LandValueAnalysis.Common;
using LandValueAnalysis.Models;
using LandValueAnalysis.Services;
using LandValueAnalysis.Services.Factories;
using System.Windows.Input;
using Esri.ArcGISRuntime.Mapping.Popups;

namespace LandValueAnalysis.ViewModels;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class MapViewModel : BaseViewModel
{
    //Injections
    private readonly LayerFactory _layerFactory;
    private readonly NavigationService _navigationService;

    //backing fields
    private DataView _currentLayer;
    private BasemapStyle _currentBasemap;
    private bool _isSettingsVisible;

    //properties for combobox so they function correctly
    public DataView[] DataViews { get; } =
    {
        DataView.BuildingFootprint,
        DataView.LandValuePerAcre,
        DataView.NetInfrastructureDeficit,
        DataView.LandUseClassification,
        DataView.AverageBuildingFootprint,
        DataView.AverageLandValuePerAcre,
        DataView.LotScaleFootprints,
        DataView.LotScale_LV_PerAcre
    };

    public BasemapStyle[] BasemapStyles { get; } =
    {
        BasemapStyle.ArcGISImagery,
        BasemapStyle.ArcGISLightGray,
        BasemapStyle.ArcGISStreets,
        BasemapStyle.ArcGISDarkGray
    };

    public Map CurrentMap { get; }
    public DataView CurrentLayer
    {
        get => _currentLayer;
        set
        {
            _currentLayer = value;
            _ = UpdateLayerAsync();
            OnPropertyChanged();
        }
    }
    public BasemapStyle CurrentBasemapStyle
    {
        get => _currentBasemap;
        set
        {
            _currentBasemap = value;
            UpdateBasemap();
            OnPropertyChanged();
        }
    }
    public bool IsSettingsVisible
    {
        get => _isSettingsVisible;
        set
        {
            _isSettingsVisible = value;
            OnPropertyChanged();
        }
    }

    public MapViewModel(LayerFactory layerFactory, NavigationService navigationService)
    {
        IsSettingsVisible = false;
        CurrentMap = new Map(BasemapStyle.ArcGISImagery)
        {
            InitialViewpoint = new Viewpoint(new MapPoint(-122.636336, 45.367024, SpatialReferences.Wgs84), 10000)
        };
        _layerFactory = layerFactory;
        _navigationService = navigationService;

        InitializeDefaultsAsync();
    }

    //public methods
    public void UpdateSettingsVisibility()
    {
        IsSettingsVisible = !IsSettingsVisible;
    }

    public void SwitchView<TViewModel>() where TViewModel : BaseViewModel
    {
        _navigationService.Navigate<TViewModel>();
    }

    public override void Dispose()
    {
        GC.SuppressFinalize(this);

        base.Dispose();
    }

    //private methods
    private async Task UpdateLayerAsync()
    {
        FeatureLayer newLayer = await _layerFactory.BuildAsync(CurrentLayer);

        CurrentMap.OperationalLayers.Clear();
        await newLayer.LoadAsync();
        CurrentMap.OperationalLayers.Add(newLayer);
    }

    private void UpdateBasemap()
    {
        CurrentMap.Basemap = new Basemap(CurrentBasemapStyle);
    }

    private void InitializeDefaultsAsync()
    {
        CurrentBasemapStyle = BasemapStyle.ArcGISImagery;
        CurrentLayer = DataView.LotScale_LV_PerAcre;
    }
}
