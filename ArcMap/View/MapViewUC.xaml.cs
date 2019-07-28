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
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing.Imaging;

namespace ArcMap.View
{
    /// <summary>
    /// Interaction logic for MapViewUC.xaml
    /// </summary>
    public partial class MapViewUC : UserControl
    {
        #region parameters
        // Graphics overlay to host sketch graphics
        private Map myMap;
        private GraphicsOverlay _sketchOverlay;
        private GraphicsOverlay _distanceOverlay;
        // Colors.
        private Color[] colors;
        // Distance variables
        private bool start_distance;
        private MapPoint first, last;
        private double _distance, _angle;
        private string distance_type;

        private WmsLayer _wmsLayer;
        // Create and hold the URL to the WMS service showing EPA water info
        private Uri _wmsUrl = new Uri("http://localhost:6060/geoserver/wms?");//"https://watersgeo.epa.gov/arcgis/services/OWPROGRAM/SDWIS_WMERC/MapServer/WMSServer?request=GetCapabilities&service=WMS");

        // Create and hold a list of uniquely-identifying WMS layer names to display
        private List<String> _wmsLayerNames = new List<string> { "topp:states", "test:roads", "test:DEM_of_Iran_30m" };
        #endregion
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

        #region Initialize
        private async void Initialize()
        {
            // Create Uri for feature layer
            //Uri featureLayerUri = new Uri(
            //    "https://sampleserver6.arcgisonline.com/arcgis/rest/services/Recreation/FeatureServer/0");

            //// Create a feature layer using url
            //FeatureLayer myFeatureLayer = new FeatureLayer(featureLayerUri)
            //{
            //    Name = "Feature Layer"
            //};
            // Apply an imagery basemap to the map
            //myMap.Basemap = Basemap.CreateImagery();
            // Add the feature layer to map
            //myMap.OperationalLayers.Add(myFeatureLayer);

            try
            {
                // Create a new WMS layer displaying the specified layers from the service
                _wmsLayer = new WmsLayer(_wmsUrl, _wmsLayerNames);
                // Load the layer
                await _wmsLayer.LoadAsync();
            }
            catch (Exception)
            {

                MessageBox.Show("Layer names not set!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // Add the layer to the map
            myMap.OperationalLayers.Add(_wmsLayer);

            // Create a map point the map should zoom to
            MapPoint mapPoint = new MapPoint(-11000000, 4500000, SpatialReferences.WebMercator);

            // Set the initial viewpoint for map
            //myMap.InitialViewpoint = new Viewpoint(57.22, 29.1, 8762.7156655228955);
            //myMap.Basemap = Basemap.CreateNationalGeographic();
            // Event for layer view state changed
            //MyMapView.LayerViewStateChanged += OnLayerViewStateChanged;

            // Provide used Map to the MapView
            MyMapView.Map = myMap;
            // Set Viewpoint so that it is centered on the coordinates defined bottom
            await MyMapView.SetViewpointCenterAsync(29.70, 57.22);

            // Set the Viewpoint scale to match the specified scale 
            await MyMapView.SetViewpointScaleAsync(876000);
            LayersListView.ItemsSource = myMap.OperationalLayers;
        }
        private void InitializeDistance()
        {
            // Add a graphics overlay for showing the tapped point.
            _distanceOverlay = new GraphicsOverlay();
            start_distance = false;
            distance_type = "NM";
            SimpleMarkerSymbol markerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Black, 5);
            _distanceOverlay.Renderer = new SimpleRenderer(markerSymbol);
            MyMapView.GraphicsOverlays.Add(_distanceOverlay);
            // Respond to user taps.
            MyMapView.GeoViewTapped += MapView_Tapped;

            //Tiff2Text.ExifMetadata(@"C:\Users\Lancer\Downloads\DEM 01\DEM_of_Iran_30m.tif", @"D:\Geo\DEM\test5.txt");
            //Tiff2Text.EnumratesTiffTags(@"C:\Users\Lancer\Downloads\DEM 01\DEM_of_Iran_30m.tif", @"D:\Geo\DEM\test6.txt");
            //Tiff2Text.PrintDirectiory(@"C:\Users\Lancer\Downloads\DEM 01\DEM_of_Iran_30m.tif", @"D:\Geo\DEM\test7.txt");
            //Tiff2Text.RgbShow(@"D:\Geo\DEM\test1.tif", @"D:\Geo\DEM\test4.bmp");

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
            DataGrid_Graphiclayers.ItemsSource = _sketchOverlay.Graphics;

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
        #endregion

        #region toolbar buttons
        private void Ruler_btn_Click(object sender, RoutedEventArgs e)
        {
            if (distance_panel.Visibility == Visibility.Collapsed)
            {
                sketch_panel.Visibility = Visibility.Collapsed;
                layers_panel.Visibility = Visibility.Collapsed;
                distance_panel.Visibility = Visibility.Visible;

                Mouse.OverrideCursor = Cursors.Pen;
                _distanceOverlay.Graphics.Clear();
                MyMapView.DismissCallout();
                start_distance = true;
                first = last = null;
            }
            else
            {
                distance_panel.Visibility = Visibility.Collapsed;
                Mouse.OverrideCursor = null;
                _distanceOverlay.Graphics.Clear();
                MyMapView.DismissCallout();
                start_distance = false;
                first = last = null;
            }

        }

        #endregion

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

        #region edit graphic btns
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
        #endregion

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
        #endregion

        #region edit layers btns
        private int FindGreatGraphicId()
        {
            return 0;
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
        #endregion

        #region Ruler parts
        private void MapView_Tapped(object sender, GeoViewInputEventArgs e)
        {
            // Get the tapped point - this is in the map's spatial reference,
            // which in this case is WebMercator because that is the SR used by the included basemaps.
            MapPoint tappedPoint = MyMapView.ScreenToLocation(e.Position);
            // Update the graphics.
            if (start_distance == true)
            {
                _distanceOverlay.Graphics.Clear();

                // Project the point to WGS84
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
                string projectedCoords = string.Format("{0:F4}, {1:F4}", projectedPoint.X, projectedPoint.Y);

                //Define a callout and show it in the map view.

                if (projectedPoint == first)
                {
                    //CalloutDefinition calloutDef = new CalloutDefinition("Coordinate:",
                    //    string.Format("{0}", projectedCoords));
                    //MyMapView.ShowCalloutAt(tappedPoint, calloutDef);
                }
                else
                {
                    Show_results(projectedPoint);
                    start_distance = false;
                    Mouse.OverrideCursor = null;
                }

            }
        }
        private void MyMapView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (first != null && last == null)
            {
                _distanceOverlay.Graphics.Clear();
                MyMapView.DismissCallout();
                first = last = null;
            }
        }
        private void MyMapView_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                MapPoint Point = MyMapView.ScreenToLocation(e.GetPosition(MyMapView));
                MapPoint projectedPoint = (MapPoint)GeometryEngine.Project(Point, SpatialReferences.Wgs84);
                string projectedCoords = string.Format("lat: {1:F4}, lng: {0:F4}", projectedPoint.X, projectedPoint.Y);
                if (start_distance && first != null)
                {
                    _distanceOverlay.Graphics.Clear();
                    Show_results(projectedPoint);
                }
                lblCursorPosition.Text = projectedCoords;
            }
            catch (Exception)
            {
                //
            }
        }
        private void Show_results(MapPoint projectedPoint)
        {
            // Format the results in strings.
            string first_projectedCoord = string.Format("{0:F4}, {1:F4}", first.X, first.Y);
            string last_projectedCoord = string.Format("{0:F4}, {1:F4}", projectedPoint.X, projectedPoint.Y);

            // show the results in the text blocks
            coordinate_text.Text = string.Format("{0} : {1} && {2}", "Coordiantes",
                first_projectedCoord, last_projectedCoord);
            _distance = distance(first.Y, first.X, projectedPoint.Y, projectedPoint.X, distance_type[0]);
            distance_text.Text = string.Format("{0} : {1:F1} {2}", "Distance",
                _distance, distance_type);
            _angle = DegreeBearing(first.Y, first.X, projectedPoint.Y, projectedPoint.X);
            angle_text.Text = string.Format("{0} : {1:F0}", "Radial/Bearing", _angle);

            MapPoint[] arr = new MapPoint[2];
            arr[0] = first;
            arr[1] = projectedPoint;
            Polyline pline = new Polyline(arr);
            Color creationColor = Color.Red;
            // Create and add a graphic from the geometry the user drew
            Graphic graphic = CreateGraphic(pline, creationColor);
            _distanceOverlay.Graphics.Add(new Graphic(first));
            _distanceOverlay.Graphics.Add(new Graphic(projectedPoint));
            _distanceOverlay.Graphics.Add(graphic);
        }
        private void distance_type_combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (distance_type_combo.SelectedIndex)
            {
                case 0:
                    distance_type = "NM";
                    break;
                case 1:
                    distance_type = "Km";
                    break;
                case 2:
                    distance_type = "M";
                    break;
                default:
                    break;
            }
            if (first != null && last != null)
            {
                _distance = distance(first.Y, first.X, last.Y, last.X, distance_type[0]);
                distance_text.Text = string.Format("{0} : {1:F1} {2}", "Distance",
                    _distance, distance_type);
            }
        }
        #endregion

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

        #region ZoomBtns

        private async void ZoomIn_btn_Click(object sender, RoutedEventArgs e)
        {
            Viewpoint viewpoint = MyMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);
            await MyMapView.SetViewpointScaleAsync(viewpoint.TargetScale - 100000);
        }

        private async void ZoomOut_btn_Click(object sender, RoutedEventArgs e)
        {
            Viewpoint viewpoint = MyMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale);
            await MyMapView.SetViewpointScaleAsync(viewpoint.TargetScale + 100000);
        }

        #endregion
    }
}
