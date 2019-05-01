using System.Windows.Controls;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using Point = System.Windows.Point;
using System.Windows.Input;

namespace ArcMap.View
{
    /// <summary>
    /// Interaction logic for MapViewUC.xaml
    /// </summary>
    public partial class MapViewUC : UserControl
    {
        // Graphics overlay to host sketch graphics
        private Map myMap;
        private GraphicsOverlay _sketchOverlay;
        // Colors.
        private Color[] colors;
        public MapViewUC()
        {
            colors = new Color[]
            {
                Color.Red,
                Color.Orange,
                Color.Yellow,
                Color.Green,
                Color.Blue,
                Color.Indigo,
                Color.Purple,
                Color.Black
            };
            myMap = new Map();
            InitializeComponent();
            Initialize();
            InitializeSketch();
        }

        private void Initialize()
        {
            // Create new Map

            // Create the uri for the tiled layer
            Uri tiledLayerUri = new Uri(
                "https://sampleserver6.arcgisonline.com/arcgis/rest/services/WorldTimeZones/MapServer");

            // Create a tiled layer using url
            ArcGISTiledLayer tiledLayer = new ArcGISTiledLayer(tiledLayerUri)
            {
                Name = "Tiled Layer"
            };

            //Add the tiled layer to map
            myMap.OperationalLayers.Add(tiledLayer);

            Uri serviceUri = new Uri(
               "https://sampleserver6.arcgisonline.com/arcgis/rest/services/SampleWorldCities/MapServer");

            // Create new image layer from the url
            ArcGISMapImageLayer imageLayer1 = new ArcGISMapImageLayer(serviceUri)
            {
                Name = "World Cities Population",
                Opacity = 0.5
            };

            // Add created layer to the basemaps collection
            myMap.OperationalLayers.Add(imageLayer1);

            // Create the uri for the ArcGISMapImage layer
            Uri imageLayerUri = new Uri(
                "https://sampleserver6.arcgisonline.com/arcgis/rest/services/Census/MapServer");

            // Create ArcGISMapImage layer using a url
            ArcGISMapImageLayer imageLayer = new ArcGISMapImageLayer(imageLayerUri)
            {
                Name = "Image Layer",

                // Set the visible scale range for the image layer
                //MinScale = 40000000,
                //MaxScale = 2000000
            };

            // Add the image layer to map
            myMap.OperationalLayers.Add(imageLayer);

            // Create Uri for feature layer
            Uri featureLayerUri = new Uri(
                "https://sampleserver6.arcgisonline.com/arcgis/rest/services/Recreation/FeatureServer/0");

            // Create a feature layer using url
            FeatureLayer myFeatureLayer = new FeatureLayer(featureLayerUri)
            {
                Name = "Feature Layer"
            };

            // Add the feature layer to map
            myMap.OperationalLayers.Add(myFeatureLayer);

            // Create a map point the map should zoom to
            MapPoint mapPoint = new MapPoint(-11000000, 4500000, SpatialReferences.WebMercator);

            // Set the initial viewpoint for map
            myMap.InitialViewpoint = new Viewpoint(mapPoint, 50000000);

            // Event for layer view state changed
            //MyMapView.LayerViewStateChanged += OnLayerViewStateChanged;

            // Provide used Map to the MapView
            MyMapView.Map = myMap;
            LayersListView.ItemsSource = myMap.OperationalLayers;
        }

        private void InitializeSketch()
        {
            // Create a light gray canvas map
            //Map myMap = new Map(Basemap.CreateLightGrayCanvas());

            // Create graphics overlay to display sketch geometry
            _sketchOverlay = new GraphicsOverlay() {
                Id = "layer 1"
            };
            MyMapView.GraphicsOverlays.Add(_sketchOverlay);
            _sketchOverlay = new GraphicsOverlay()
            {
                Id = "layer 2"
            };
            MyMapView.GraphicsOverlays.Add(_sketchOverlay);
            _sketchOverlay = new GraphicsOverlay()
            {
                Id = "layer 3"
            };
            MyMapView.GraphicsOverlays.Add(_sketchOverlay);
            DataGrid_Graphiclayers.ItemsSource = MyMapView.GraphicsOverlays;

            // Assign the map to the MapView
            MyMapView.Map = myMap;

            // Fill the combo box with choices for the sketch modes (shapes)
            SketchModeComboBox.ItemsSource = Enum.GetValues(typeof(SketchCreationMode));
            SketchModeComboBox.SelectedIndex = 0;

            // Fill the color combo box with choices for the sketch colors
            SketchColorComboBox.ItemsSource = colors;
            SketchColorComboBox.SelectedIndex = 0;

            // Set the sketch editor configuration to allow vertex editing, resizing, and moving
            SketchEditConfiguration config = MyMapView.SketchEditor.EditConfiguration;
            config.AllowVertexEditing = true;
            config.ResizeMode = SketchResizeMode.Uniform;
            config.AllowMove = true;

            // Set the sketch editor as the page's data context
            DataContext = MyMapView.SketchEditor;
        }

        #region Graphic and symbol helpers
        private Graphic CreateGraphic(Geometry geometry, Color color)
        {
            // Create a graphic to display the specified geometry
            Symbol symbol = null;
            switch (geometry.GeometryType)
            {
                // Symbolize with a fill symbol
                case GeometryType.Envelope:
                case GeometryType.Polygon:
                    {
                        symbol = new SimpleLineSymbol()
                        {
                            Color = color,
                            Style = SimpleLineSymbolStyle.Solid,
                            Width = 3
                        };
                        break;
                    }
                // Symbolize with a line symbol
                case GeometryType.Polyline:
                    {
                        symbol = new SimpleLineSymbol()
                        {
                            Color = color,
                            Style = SimpleLineSymbolStyle.Solid,
                            Width = 5d
                        };
                        break;
                    }
                // Symbolize with a marker symbol
                case GeometryType.Point:
                case GeometryType.Multipoint:
                    {

                        symbol = new SimpleMarkerSymbol()
                        {
                            Color = color,
                            Style = SimpleMarkerSymbolStyle.Circle,
                            Size = 15d
                        };
                        break;
                    }
            }

            // pass back a new graphic with the appropriate symbol
            return new Graphic(geometry, symbol);
        }

        private async Task<Graphic> GetGraphicAsync()
        {
            // Wait for the user to click a location on the map
            Geometry mapPoint = await MyMapView.SketchEditor.StartAsync(SketchCreationMode.Point, false);

            // Convert the map point to a screen point
            Point screenCoordinate = MyMapView.LocationToScreen((MapPoint)mapPoint);

            // Identify graphics in the graphics overlay using the point
            IReadOnlyList<IdentifyGraphicsOverlayResult> results = await MyMapView.IdentifyGraphicsOverlaysAsync(screenCoordinate, 2, false);

            // If results were found, get the first graphic
            Graphic graphic = null;
            IdentifyGraphicsOverlayResult idResult = results.FirstOrDefault();
            if (idResult != null && idResult.Graphics.Count > 0)
            {
                graphic = idResult.Graphics.FirstOrDefault();
            }

            // Return the graphic (or null if none were found)
            return graphic;
        }
        #endregion

        private async void DrawButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Let the user draw on the map view using the chosen sketch mode
                SketchCreationMode creationMode = (SketchCreationMode)SketchModeComboBox.SelectedItem;
                Color creationColor = colors[SketchColorComboBox.SelectedIndex];
                Geometry geometry = await MyMapView.SketchEditor.StartAsync(creationMode, true);

                // Create and add a graphic from the geometry the user drew
                Graphic graphic = CreateGraphic(geometry, creationColor);
                _sketchOverlay.Graphics.Add(graphic);

                // Enable/disable the clear and edit buttons according to whether or not graphics exist in the overlay
                ClearButton.IsEnabled = _sketchOverlay.Graphics.Count > 0;
                EditButton.IsEnabled = _sketchOverlay.Graphics.Count > 0;
            }
            catch (TaskCanceledException)
            {
                // Ignore ... let the user cancel drawing
            }
            catch (Exception ex)
            {
                // Report exceptions
                MessageBox.Show("Error drawing graphic shape: " + ex.Message);
            }
        }

        private void ClearButtonClick(object sender, RoutedEventArgs e)
        {
            // Remove all graphics from the graphics overlay
            _sketchOverlay.Graphics.Clear();

            // Disable buttons that require graphics
            ClearButton.IsEnabled = false;
            EditButton.IsEnabled = false;
        }

        private async void EditButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // Allow the user to select a graphic
                Graphic editGraphic = await GetGraphicAsync();
                if (editGraphic == null) { return; }

                // Let the user make changes to the graphic's geometry, await the result (updated geometry)
                Geometry newGeometry = await MyMapView.SketchEditor.StartAsync(editGraphic.Geometry);

                // Display the updated geometry in the graphic
                editGraphic.Geometry = newGeometry;
            }
            catch (TaskCanceledException)
            {
                // Ignore ... let the user cancel editing
            }
            catch (Exception ex)
            {
                // Report exceptions
                MessageBox.Show("Error editing shape: " + ex.Message);
            }
        }

        private void draw_sketch_btn_Click(object sender, RoutedEventArgs e)
        {
            if (sketch_panel.Visibility == Visibility.Collapsed)
            {
                layers_panel.Visibility = Visibility.Collapsed;
                sketch_panel.Visibility = Visibility.Visible;
            }
            else
                sketch_panel.Visibility = Visibility.Collapsed;
        }

        private void layers_btn_Click(object sender, RoutedEventArgs e)
        {
            if (layers_panel.Visibility == Visibility.Collapsed)
            {
                sketch_panel.Visibility = Visibility.Collapsed;
                layers_panel.Visibility = Visibility.Visible;
            }
            else
                layers_panel.Visibility = Visibility.Collapsed;
        }

        private void plusbtn_Click(object sender, RoutedEventArgs e)
        {
            GraphicsOverlay g = ((Button)sender).DataContext as GraphicsOverlay;
            int idx = MyMapView.GraphicsOverlays.IndexOf(g);
            _sketchOverlay = new GraphicsOverlay()
            {
                Id = "layer " + (MyMapView.GraphicsOverlays.Count + 1)
            };
            MyMapView.GraphicsOverlays.Insert(idx + 1, _sketchOverlay);
        }

        private void minesbtn_Click(object sender, RoutedEventArgs e)
        {
            if (MyMapView.GraphicsOverlays.Count <= 1)
            {
                MessageBox.Show("this is last layer, you can not delete this!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            GraphicsOverlay g = ((Button)sender).DataContext as GraphicsOverlay;
            if (_sketchOverlay == g)
            {
                int idx = MyMapView.GraphicsOverlays.IndexOf(g);
                if (idx == 0)
                    idx = 1;
                else
                    idx = idx - 1;
                _sketchOverlay = MyMapView.GraphicsOverlays[idx];
            }
            MyMapView.GraphicsOverlays.Remove(g);
        }

        private void DataGrid_Graphiclayers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataGrid_Graphiclayers.UnselectAll();
        }
    }
}
