using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using LandValueAnalysis.Common;
using LandValueAnalysis.Models;
using LandValueAnalysis.Services;
using LandValueAnalysis.Models.Shared;
using LandValueAnalysis.Services.Factories;
using System.Diagnostics;

namespace LandValueAnalysis.ViewModels;

[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class MapViewModel : BaseViewModel
{
    //Injections
    private readonly LayerFactory _layerFactory;
    private readonly NavigationService _navigationService;
    private MapFactory _mapFactory;

    //backing fields
    private MapStatus _currentMap;
    private DataView _currentLayer;
    private BasemapStyle _currentBasemap;
    private bool _isSettingsVisible;

    //properties for combobox so they function correctly
    public DataView[] DataViews { get; } =
    {
        DataView.Infrastructure,
        DataView.Zoning,
        DataView.Footprints,
        DataView.NetPresentValuePerHectare
    };

    public BasemapStyle[] BasemapStyles { get; } =
    {
        BasemapStyle.ArcGISImagery,
        BasemapStyle.ArcGISStreets,
        BasemapStyle.ArcGISStreets,
        BasemapStyle.ArcGISDarkGrayBase
    };

    public bool IsSettingsVisible
    {   
        get => _isSettingsVisible;
        private set
        {
            _isSettingsVisible = value;
            OnPropertyChanged();
        }
    }
    public MapStatus CurrentMapStatus
    {
        get => _currentMap;
        private set
        {
            _currentMap = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsThreeDimensional));
            OnPropertyChanged(nameof(IsTwoDimensional));
        }
    }
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
    public Viewpoint CurrentViewpoint { get; set; }

    public bool IsThreeDimensional => CurrentMapStatus.CurrentMap is Scene;
    public bool IsTwoDimensional => CurrentMapStatus.CurrentMap is Map;

    public MapViewModel(LayerFactory layerFactory, NavigationService navigationService, MapFactory mapFactory)
    {
        _layerFactory = layerFactory;
        _navigationService = navigationService;
        _mapFactory = mapFactory;

        InitializeDefaults();
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

    public async Task UpdateMapMode(MapMode mapMode)
    {
        //Clear initial map's layers to prevent ownership issues
        CurrentMapStatus.CurrentMap.OperationalLayers.Clear();

        CurrentMapStatus = _mapFactory.Build(
            mapMode,
            CurrentViewpoint);
        UpdateBasemap();
        await UpdateLayerAsync();
    }

    public override void Dispose()
    {
        GC.SuppressFinalize(this);

        base.Dispose();
    }

    //private methods
    private async Task UpdateLayerAsync()
    {
        //Wrap in try catch since the Task return value is discarded in its calls
        try
        {
            FeatureLayer newLayer = await _layerFactory.BuildAsync(CurrentLayer);

            await newLayer.LoadAsync();
            CurrentMapStatus.CurrentMap.OperationalLayers.Clear();
            CurrentMapStatus.CurrentMap.OperationalLayers.Add(newLayer);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Adding layer to map failed! {ex.ToString()}");
        }
    }

    private void UpdateBasemap()
    {
        CurrentMapStatus.CurrentMap.Basemap = new Basemap(CurrentBasemapStyle);
    }

    private void InitializeDefaults()
    {
        IsSettingsVisible = false;
        CurrentMapStatus = _mapFactory.Build(
            MapMode.TwoDimensional);

        CurrentBasemapStyle = BasemapStyle.ArcGISImagery;
        CurrentLayer = DataView.Footprints;

        CurrentViewpoint = new Viewpoint(new MapPoint(-122.636336, 45.367024, SpatialReferences.Wgs84), 10000);
    }
}
