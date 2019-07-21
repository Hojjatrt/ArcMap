﻿using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.UI.GeoAnalysis;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ArcMap.View
{
    /// <summary>
    /// Interaction logic for DistanceMeasurement.xaml
    /// </summary>
    public partial class DistanceMeasurement : UserControl
    {
        // URLs to various services used to provide an interesting scene for the sample.
        private readonly Uri _worldElevationService =
            new Uri("https://elevation3d.arcgis.com/arcgis/rest/services/WorldElevation3D/Terrain3D/ImageServer");

        private readonly Uri _buildingService =
            new Uri(
                "https://tiles.arcgis.com/tiles/P3ePLMYs2RVChkJx/arcgis/rest/services/Buildings_Brest/SceneServer/layers/0");

        // Measurement.
        private LocationDistanceMeasurement _distanceMeasurement;
        public DistanceMeasurement()
        {
            InitializeComponent();
            // Create the UI, setup the control references and execute initialization.
            Initialize();
        }

        private void Initialize()
        {
            // Create a scene with elevation.
            Surface sceneSurface = new Surface();
            sceneSurface.ElevationSources.Add(new ArcGISTiledElevationSource(_worldElevationService));
            Scene myScene = new Scene(Basemap.CreateImagery())
            {
                BaseSurface = sceneSurface
            };

            // Create and add a building layer.
            ArcGISSceneLayer buildingsLayer = new ArcGISSceneLayer(_buildingService);
            myScene.OperationalLayers.Add(buildingsLayer);

            // Create and add an analysis overlay.
            AnalysisOverlay measureAnalysisOverlay = new AnalysisOverlay();
            MySceneView.AnalysisOverlays.Add(measureAnalysisOverlay);

            // Create an initial distance measurement and show it.
            MapPoint start = new MapPoint(-4.494677, 48.384472, 24.772694, SpatialReferences.Wgs84);
            MapPoint end = new MapPoint(-4.495646, 48.384377, 58.501115, SpatialReferences.Wgs84);
            _distanceMeasurement = new LocationDistanceMeasurement(start, end);
            measureAnalysisOverlay.Analyses.Add(_distanceMeasurement);
            MySceneView.SetViewpointCamera(new Camera(start, 200, 0, 45, 0));

            // Keep the UI updated.
            _distanceMeasurement.MeasurementChanged += (o, e) =>
            {
                // This is needed because measurement change events occur on a non-UI thread and this code accesses UI object.
                Dispatcher.Invoke(() =>
                {
                    // Update the labels with new values in the format {value} {unit system}.
                    DirectMeasureLabel.Content = string.Format("{0:F} {1}", _distanceMeasurement.DirectDistance.Value,
                        _distanceMeasurement.DirectDistance.Unit.Abbreviation);
                    VerticalMeasureLabel.Content = string.Format("{0:F} {1}", _distanceMeasurement.VerticalDistance.Value,
                        _distanceMeasurement.VerticalDistance.Unit.Abbreviation);
                    HorizontalMeasureLabel.Content = string.Format("{0:F} {1}",
                        _distanceMeasurement.HorizontalDistance.Value,
                        _distanceMeasurement.HorizontalDistance.Unit.Abbreviation);
                });
            };

            // Configure the unit system selection box.
            UnitSystemCombo.ItemsSource = Enum.GetValues(typeof(UnitSystem));
            UnitSystemCombo.SelectedItem = _distanceMeasurement.UnitSystem;

            // Update the unit system selection.
            UnitSystemCombo.SelectionChanged += (sender, args) =>
            {
                _distanceMeasurement.UnitSystem = (UnitSystem)UnitSystemCombo.SelectedItem;
            };

            // Show the scene in the view.
            MySceneView.Scene = myScene;

            // Enable the 'New measurement' button.
            NewMeasureButton.IsEnabled = true;
        }

        private void MySceneView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            // Unsubscribe from events to finish the measurement.
            MySceneView.MouseMove -= MySceneView_MouseMoved;
            MySceneView.GeoViewTapped -= MySceneView_GeoViewTapped;

            // Re-enable the new measurement button.
            NewMeasureButton.IsEnabled = true;

            // Re-set the help label.
            HelpLabel.Content = "Tap 'New measurement' to start.";
        }

        private async void MySceneView_MouseMoved(object sender, MouseEventArgs e)
        {
            try
            {
                // Get the geographic location for the current mouse position.
                MapPoint geoPoint = await MySceneView.ScreenToLocationAsync(e.GetPosition(MySceneView));

                if (geoPoint == null)
                {
                    return;
                }

                // Update the location distance measurement.
                _distanceMeasurement.EndLocation = geoPoint;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        private void NewMeasureButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Subscribe to mouse move to change the measurement.
            MySceneView.MouseMove += MySceneView_MouseMoved;

            // Subscribe to the tap method to enable finishing a measurement.
            MySceneView.GeoViewTapped += MySceneView_GeoViewTapped;

            // Disable the button.
            NewMeasureButton.IsEnabled = false;

            // Update the help label.
            HelpLabel.Content = "Move the mouse to update the end point. Tap again to finish.";
        }
    }
}