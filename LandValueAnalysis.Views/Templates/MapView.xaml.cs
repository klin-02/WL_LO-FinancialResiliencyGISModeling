using LandValueAnalysis.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Toolkit.UI.Controls;
using System.Windows.Media.Animation;
using LandValueAnalysis.Services.Factories;
using LandValueAnalysis.Models.Shared;
using Esri.ArcGISRuntime.Symbology;
using System.Runtime.CompilerServices;

namespace LandValueAnalysis.Views.Templates;

//The page in the UI for the mapping operations
//A little messy since ArcGIS sdk's architecture isn't very decoupled
public partial class MapView : UserControl
{
    //keybpard inputs for tilting in 3D
    private static readonly Dictionary<Key, Action> _keyboardInputs = new Dictionary<Key, Action>();

    //backing fields
    private readonly PopupViewer _featurePopupViewer;

    private MapViewModel _mapViewModel;

    public MapView()
    {
        InitializeComponent();
        LoadKeybinds();

        _featurePopupViewer = new PopupViewer();
    }

    //Workaround for getting datacontext of UserControl for things I can't use ICommand on
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _mapViewModel = this.DataContext as MapViewModel
            ?? throw new Exception("Data Context doesn't exist");
    }
 
    private void KeyPressed(object sender, KeyEventArgs e)
    {
        if (_keyboardInputs.ContainsKey(e.Key))
        {
            _keyboardInputs[e.Key].Invoke();

            e.Handled = true;
        }
    }

    private void LoadKeybinds()
    {
        _keyboardInputs.Add(
            Key.W,
            () => Rotate(amount: 2)
            );
        _keyboardInputs.Add(
            Key.S,
            () => Rotate(amount: -2)
            );

    }

    //When map is clicked a popup, if exists, will show
    private async void MapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
    {
        try
        {
            //Pixel-relative click position
            Point clickPosition = e.Position;

            //Lat-long relative click location
            MapPoint clickLocation = e.Location;

            //Get layer where click was at
            FeatureLayer currentLayer = MyMapView.Map.OperationalLayers[0] as FeatureLayer;

            //Clear selected features
            currentLayer.ClearSelection();

            Popup? popup = await GetPopupAsync(clickPosition, currentLayer);

            if (popup != null)
            {
                //Feature that was clicked
                Feature feature = popup.GeoElement as Feature;

                await ShowPopupAsync(popup, clickLocation, feature);
                currentLayer.SelectFeature(feature);
                return;
            }
            //Hide callout if no popup exists
            MyMapView.DismissCallout();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Loading popup definition for feature failed!\n\n{ex.ToString()}");
        }
    }

    private async Task<Popup?> GetPopupAsync(Point clickLocation, FeatureLayer currentLayer)
    {
        //Get the clicked feature
        IdentifyLayerResult identifiedFeature = await MyMapView.IdentifyLayerAsync(currentLayer, clickLocation, 1, true);

        return identifiedFeature?.Popups.FirstOrDefault() ?? null;
    }

    //show popup via callout
    private async Task ShowPopupAsync(Popup popup, MapPoint clickLocation, Feature feature)
    {
        await MyMapView.SetViewpointCenterAsync(clickLocation);

        _featurePopupViewer.Popup = popup;
        MyMapView.ShowCalloutAt(clickLocation, _featurePopupViewer);
    }

    //workaround event handler to prevent settings from hiding prior to animation
    private void ChangeSettingsVisibility(object sender, EventArgs e)
    {
        _mapViewModel.UpdateSettingsVisibility();
    }

        private async void On3DEnabled(object sender, EventArgs e)
    {
        try
        {
            await _mapViewModel.UpdateMapMode(MapMode.ThreeDimensional);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"3D Mapping Failed! {ex.ToString()}");
        }
    }
    
    private async void On3DDisabled(object sender, EventArgs e)
    {
        try
        {
            await _mapViewModel.UpdateMapMode(MapMode.TwoDimensional);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"2D Mapping Failed! {ex.ToString()}");
        }
    }

    private void Map_ViewpointChanged(object sender, EventArgs e)
    {
        GeoView map = sender as GeoView;
        Viewpoint currentViewpoint = map.GetCurrentViewpoint(ViewpointType.CenterAndScale);

        _mapViewModel.CurrentViewpoint = currentViewpoint;
    }

    //Should be in viewmodel but ArcGIS's architecture design is not good
    //implementation of rotation - insert negative to rotate down, positive to rotate up
    private void Rotate(double amount)
    {
        if (_mapViewModel.IsThreeDimensional)
        {
            Camera camera = MySceneView.Camera;

            camera.RotateTo(
                camera.Heading,
                Math.Clamp(camera.Pitch + amount, 0, 180),
                camera.Roll
                );
            MySceneView.SetViewpointCamera(camera);
        }
    }
}
