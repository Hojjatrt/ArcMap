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
using Esri.ArcGISRuntime.UI.Controls;
using Color = System.Drawing.Color;
using Esri.ArcGISRuntime.UI.GeoAnalysis;

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
        private GraphicsOverlay _distanceOverlay;
        // Colors.
        private Color[] colors;
        private bool start_distance;
        private MapPoint first, last;
        private double _distance,_angle;
        private WmsLayer _wmsLayer;
        // Create and hold the URL to the WMS service showing EPA water info
        private Uri _wmsUrl = new Uri("http://localhost:6060/geoserver/wms?");//"https://watersgeo.epa.gov/arcgis/services/OWPROGRAM/SDWIS_WMERC/MapServer/WMSServer?request=GetCapabilities&service=WMS");

        // Create and hold a list of uniquely-identifying WMS layer names to display
        private List<String> _wmsLayerNames = new List<string> { "topp:states", "test:roads" };

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
            InitializeDistance();
            InitializeSketch();
        }

        private async void Initialize()
        {
            // Create new Map

            // Create the uri for the tiled layer
            Uri tiledLayerUri = new Uri(
            //"https://sampleserver6.arcgisonline.com/arcgis/rest/services/WorldTimeZones/MapServer");
            "http://localhost:6060/geoserver/wms");

            // Create a tiled layer using url
            ArcGISTiledLayer tiledLayer = new ArcGISTiledLayer(tiledLayerUri)
            {
                Name = "Tiled Layer"
            };

            //Add the tiled layer to map
            //myMap.OperationalLayers.Add(tiledLayer);

            Uri serviceUri = new Uri("http://localhost:6060/geoserver/wms");
            //"https://sampleserver6.arcgisonline.com/arcgis/rest/services/SampleWorldCities/MapServer");

            // Create new image layer from the url
            ArcGISMapImageLayer imageLayer1 = new ArcGISMapImageLayer(serviceUri)
            {
                Name = "World Cities Population",
                //Opacity = 0.5
            };

            // Add created layer to the basemaps collection
            //myMap.OperationalLayers.Add(imageLayer1);

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
            //myMap.OperationalLayers.Add(imageLayer);

            // Create Uri for feature layer
            Uri featureLayerUri = new Uri(
                "https://sampleserver6.arcgisonline.com/arcgis/rest/services/Recreation/FeatureServer/0");

            // Create a feature layer using url
            FeatureLayer myFeatureLayer = new FeatureLayer(featureLayerUri)
            {
                Name = "Feature Layer"
            };
            // Apply an imagery basemap to the map
            //myMap.Basemap = Basemap.CreateImagery();
            // Add the feature layer to map
            myMap.OperationalLayers.Add(myFeatureLayer);

            // Create a new WMS layer displaying the specified layers from the service
            _wmsLayer = new WmsLayer(_wmsUrl, _wmsLayerNames);

            // Load the layer
            await _wmsLayer.LoadAsync();

            // Add the layer to the map
            myMap.OperationalLayers.Add(_wmsLayer);

            // Create a map point the map should zoom to
            MapPoint mapPoint = new MapPoint(-11000000, 4500000, SpatialReferences.WebMercator);

            // Set the initial viewpoint for map
            myMap.InitialViewpoint = new Viewpoint(mapPoint, 50000000);
            //myMap.Basemap = Basemap.CreateNationalGeographic();
            // Event for layer view state changed
            //MyMapView.LayerViewStateChanged += OnLayerViewStateChanged;

            // Provide used Map to the MapView
            MyMapView.Map = myMap;
            LayersListView.ItemsSource = myMap.OperationalLayers;
        }

        private void InitializeDistance()
        {
            // Add a graphics overlay for showing the tapped point.
            _distanceOverlay = new GraphicsOverlay();
            start_distance = false;
            SimpleMarkerSymbol markerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Black, 5);
            _distanceOverlay.Renderer = new SimpleRenderer(markerSymbol);
            MyMapView.GraphicsOverlays.Add(_distanceOverlay);
            // Respond to user taps.
            MyMapView.GeoViewTapped += MapView_Tapped;
        }
        private void InitializeSketch()
        {
            // Create a light gray canvas map
            //Map myMap = new Map(Basemap.CreateLightGrayCanvas());

            // Create graphics overlay to display sketch geometry
            _sketchOverlay = new GraphicsOverlay()
            {
                Id = "layer 1"
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

        private void MapView_Tapped(object sender, GeoViewInputEventArgs e)
        {
            // Get the tapped point - this is in the map's spatial reference,
            // which in this case is WebMercator because that is the SR used by the included basemaps.
            //MapPoint tappedPoint = e.Location;
            MapPoint tappedPoint = MyMapView.ScreenToLocation(e.Position);
            // Update the graphics.
            //if (last != null && start_distance)
            //{
            //    end_distance_btn.Command.Execute(end_distance_btn.CommandParameter);
            //    start_distance = false;
            //}
            if (start_distance == true)
            {
                _distanceOverlay.Graphics.Clear();
                //// Project the point to WGS84
                MapPoint projectedPoint = (MapPoint)GeometryEngine.Project(tappedPoint, SpatialReferences.Wgs84);
                if (first == null)
                    first = projectedPoint;
                else
                {
                    last = projectedPoint;
                    _distanceOverlay.Graphics.Add(new Graphic(first));
                    
                }
                _distanceOverlay.Graphics.Add(new Graphic(tappedPoint));
                


                // Format the results in strings.
                //string originalCoords = string.Format("Original: {0:F4}, {1:F4}", tappedPoint.X, tappedPoint.Y);
                //MessageBox.Show(tappedPoint.GeometryType.ToString());
                string projectedCoords = string.Format("Projected: {0:F4}, {1:F4}", projectedPoint.X, projectedPoint.Y);
                string formattedString = string.Format("{0}", projectedCoords);

                //Define a callout and show it in the map view.

                if (projectedPoint == first)
                {
                    CalloutDefinition calloutDef = new CalloutDefinition("Coordinates:", formattedString);
                    MyMapView.ShowCalloutAt(tappedPoint, calloutDef);
                }
                else
                {
                    coordinate_text.Text = formattedString;
                    _distance = distance(first.Y, first.X, last.Y, last.X, 'N');
                    distance_text.Text = _distance.ToString();
                    _angle = DegreeBearing(first.Y, first.X, last.Y, last.X);
                    angle_text.Text = _angle.ToString();
                    MapPoint[] arr = new MapPoint[2];
                    arr[0] = first;
                    arr[1] = last;
                    Polyline pline = new Polyline(arr);
                    Color creationColor = Color.Red;
                    // Create and add a graphic from the geometry the user drew
                    Graphic graphic = CreateGraphic(pline, creationColor);
                    _distanceOverlay.Graphics.Add(graphic);
                    start_distance = false;

                }

            }
        }

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

        #region panel_btns
        private void draw_sketch_btn_Click(object sender, RoutedEventArgs e)
        {
            if (sketch_panel.Visibility == Visibility.Collapsed)
            {
                sketch_panel.Visibility = Visibility.Visible;
                layers_panel.Visibility = Visibility.Collapsed;
                distance_panel.Visibility = Visibility.Collapsed;
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
                distance_panel.Visibility = Visibility.Collapsed;
            }
            else
                layers_panel.Visibility = Visibility.Collapsed;
        }

        private void distance_btn_Click(object sender, RoutedEventArgs e)
        {
            if (distance_panel.Visibility == Visibility.Collapsed)
            {
                sketch_panel.Visibility = Visibility.Collapsed;
                layers_panel.Visibility = Visibility.Collapsed;
                distance_panel.Visibility = Visibility.Visible;
            }
            else
                distance_panel.Visibility = Visibility.Collapsed;
        }
        #endregion
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

        private void start_distance_btn_Click(object sender, RoutedEventArgs e)
        {
            _distanceOverlay.Graphics.Clear();
            MyMapView.DismissCallout();
            start_distance = true;
            first = last = null;
        }

        private void MyMapView_MouseMove(object sender, MouseEventArgs e)
        {
            if (start_distance && first != null)
            {
                MapPoint Point = MyMapView.ScreenToLocation(e.GetPosition(MyMapView));
                _distanceOverlay.Graphics.Clear();
                //// Project the point to WGS84
                MapPoint projectedPoint = (MapPoint)GeometryEngine.Project(Point, SpatialReferences.Wgs84);
                string projectedCoords = string.Format("Projected: {0:F4}, {1:F4}", projectedPoint.X, projectedPoint.Y);
                string formattedString = string.Format("{0}", projectedCoords);
                coordinate_text.Text = formattedString;
                _distance = distance(first.Y, first.X, projectedPoint.Y, projectedPoint.X, 'N');
                distance_text.Text = _distance.ToString();
                _angle = DegreeBearing(first.Y, first.X, projectedPoint.Y, projectedPoint.X);
                angle_text.Text = _angle.ToString();
                MapPoint[] arr = new MapPoint[2];
                arr[0] = first;
                arr[1] = projectedPoint;
                Polyline pline = new Polyline(arr);
                Color creationColor = Color.Red;
                // Create and add a graphic from the geometry the user drew
                Graphic graphic = CreateGraphic(pline, creationColor);
                _distanceOverlay.Graphics.Add(graphic);
            }
        }
        #region angle meaturment
        static double DegreeBearing(double lat1, double lon1, double lat2, double lon2)
        {
            var dLon = ToRad(lon2 - lon1);
            var dPhi = Math.Log(
                Math.Tan(ToRad(lat2) / 2 + Math.PI / 4) / Math.Tan(ToRad(lat1) / 2 + Math.PI / 4));
            if (Math.Abs(dLon) > Math.PI)
                dLon = dLon > 0 ? -(2 * Math.PI - dLon) : (2 * Math.PI + dLon);
            return ToBearing(Math.Atan2(dLon, dPhi));
        }

        public static double ToRad(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        public static double ToDegrees(double radians)
        {
            return radians * 180 / Math.PI;
        }

        public static double ToBearing(double radians)
        {
            // convert radians to degrees (as bearing: 0...360)
            return (ToDegrees(radians) + 360) % 360;
        }
        #endregion

        #region distance meaturment
        private double distance(double lat1, double lon1, double lat2, double lon2, char unit)
        {
            if ((lat1 == lat2) && (lon1 == lon2))
            {
                return 0;
            }
            else
            {
                double theta = lon1 - lon2;
                double dist = Math.Sin(ToRad(lat1)) * Math.Sin(ToRad(lat2)) + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Cos(ToRad(theta));
                dist = Math.Acos(dist);
                dist = ToDegrees(dist);
                dist = dist * 60 * 1.1515;
                if (unit == 'K')
                {
                    dist = dist * 1.609344;
                }
                else if (unit == 'N')
                {
                    dist = dist * 0.8684;
                }
                return (dist);
            }
        }
        #endregion
    }
}
