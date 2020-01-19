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
using Microsoft.Win32;
using System.Threading;
using Esri.ArcGISRuntime.Ogc;
using System.Collections.ObjectModel;
using ArcMap.ViewModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Reflection;
using RCCMap;

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
        private bool start_distance, start_polyline;
        private MapPoint first, last;
        private double _distance, _angle;
        private string distance_type;

        //private WmsLayer _wmsLayer;
        // Create and hold the URL to the WMS service showing EPA water info
        private Uri _wmsUrl = new Uri("http://localhost:6060/geoserver/test/wms?");
        // Hold a list of LayerDisplayVM; this is the ViewModel
        public ObservableCollection<LayerDisplayVM> _viewModelList = new ObservableCollection<LayerDisplayVM>();
        private WmsLayer _wmsLayer;
        // Create and hold a list of uniquely-identifying WMS layer names to display
        private List<String> _wmsLayerNames = new List<string> { "topp:states", "test:roads", "test:DEM_of_Iran_30m" };

        private Point _startPoint;
        private bool dragAction;
        private ListBoxItem _originatingListBoxItem;
        private PopupWmsData popupWmsData;

        private string _selectedTag = "";

        public GraphicsOverlayCollection GraphicLayers
        {
            get { return MyMapView.GraphicsOverlays; }
            set { MyMapView.GraphicsOverlays = value; }
        }

        public LayerCollection OverLayers
        {
            get { return myMap.OperationalLayers; }
        }
        
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
                Color.SkyBlue,
                Color.Purple,
                Color.Cyan,
                Color.Gray,
                Color.Khaki,
                Color.Black
            };
            myMap = new Map();
            InitializeComponent();
            Initialize();
            InitializeDistance();
            InitializeSketch();
            LoadInitialData();
        }

        #region Initialize
        private async void Initialize()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "WmsDataFile.txt";
            try
            {
                using (StreamReader sr = new StreamReader(resourceName))
                {
                    string result = sr.ReadToEnd();
                    _wmsUrl = new Uri(result);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("error in reading database file!");
            }

            // Create a new WMS layer displaying the specified layers from the service
            // Create the WMS Service.
            WmsService service = new WmsService(_wmsUrl);

            try
            {
                // Load the WMS Service.
                await service.LoadAsync();

                // Get the service info (metadata) from the service.
                WmsServiceInfo info = service.ServiceInfo;
                // Get the list of layer infos.
                //LayerDisplayVM.BuildLayerInfoList(new LayerDisplayVM(info.LayerInfos[0], null), _viewModelList);
                foreach (var layerInfo in info.LayerInfos)
                {
                    LayerDisplayVM.BuildLayerInfoList(new LayerDisplayVM(layerInfo, null), _viewModelList);

                }

                // Update the map display based on the viewModel.
                _viewModelList.RemoveAt(0);
                _viewModelList[0].IsEnabled = false;
                
                foreach (var item in _viewModelList[0].Children)
                {
                    item.IsEnabled = true;
                }
                ObservableCollection<LayerDisplayVM> _viewModelList1 = new ObservableCollection<LayerDisplayVM>(_viewModelList[0].Children);
                _viewModelList = _viewModelList1;
                _viewModelList1 = null;
                //var layernamesfile = "selectedlayers.txt";
                //var layernames = "";
                //try
                //{
                //    using (StreamReader sr = new StreamReader(layernamesfile))
                //    {
                //        layernames = sr.ReadToEnd();
                //    }
                //}
                //catch (Exception)
                //{

                //}
                //string[] lines = layernames.Split('\n');
                //// Return if no layers are selected.
                //if (lines.Length != 0)
                //{
                //    foreach (var layername in lines)
                //    {
                //        var layer = _viewModelList.Where(vm => vm.Info.Name == layername.Trim('\r') && !vm.IsEnabled).Select(vm => vm).ToList();
                //        if (layer.Any())
                //        {
                //            foreach (var item in layer)
                //            {
                //                item.IsEnabled = true;
                //            }
                //        }
                //    }
                //}

                UpdateMapDisplay(_viewModelList);

                // Update the list of layers.
                LayerTreeView.ItemsSource = _viewModelList.AsEnumerable();

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Error");
            }

            MapPoint point =
                        CoordinateFormatter.FromLatitudeLongitude("40N 63 30E", MyMapView.SpatialReference);
            MapPoint point2 =
                        CoordinateFormatter.FromLatitudeLongitude("25N 44E", MyMapView.SpatialReference);
            Envelope initialLocation = new Envelope(
                point.X, point.Y, point2.X, point2.Y,
                SpatialReferences.Wgs84);
            myMap.InitialViewpoint = new Viewpoint(initialLocation);
            // Provide used Map to the MapView
            MyMapView.Map = myMap;
            // Set Viewpoint so that it is centered on the coordinates defined bottom
            //await MyMapView.SetViewpointCenterAsync(29.70, 60.22);

            // Set the Viewpoint scale to match the specified scale 
            //await MyMapView.SetViewpointScaleAsync(876000);
        }
        private void InitializeDistance()
        {
            // Add a graphics overlay for showing the tapped point.
            _distanceOverlay = new GraphicsOverlay()
            {
                Id = "Distance layer"
            };
            start_distance = false;
            distance_type = "NM";
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
                Id = "Temp"
            };
            //MyMapView.GraphicsOverlays.Clear();
            MyMapView.GraphicsOverlays.Add(_sketchOverlay);
            GraphicLayesrListBox.ItemsSource = MyMapView.GraphicsOverlays;

            // Assign the map to the MapView
            MyMapView.Map = myMap;

            // Fill the color combo box with choices for the sketch colors
            SketchColorComboBox.ItemsSource = colors;
            SketchColorComboBox.SelectedIndex = 0;

            // sketch layer
            SketchLayerComboBox.ItemsSource = GraphicLayers;
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

        #region Load data
        private void LoadInitialData()
        {
            try
            {   // Open the text file using a stream reader.
                using (StreamReader sr = new StreamReader("datafile.txt"))
                {
                    // Read the stream to a string, and write the string to the console.
                    MyMapView.GraphicsOverlays.Clear();
                    String all = sr.ReadToEnd();
                    if (all != "")
                    {
                        string[] lines = all.Split('\n');
                        int graphiclayers = Convert.ToInt32(lines[0]);
                        int index = 1;
                        for (int i = 1; i <= graphiclayers; i++)
                        {
                            string name = lines[index].Trim('\r');
                            GraphicsOverlay overlay = new GraphicsOverlay()
                            {
                                Id = name
                            };
                            if (overlay.Id == "Distance layer")
                                _distanceOverlay = overlay;
                            MyMapView.GraphicsOverlays.Add(overlay);
                            int graphics = Convert.ToInt32(lines[index + 1]);
                            for (int j = 0; j < graphics; j++)
                            {
                                Geometry geometry = Geometry.FromJson(lines[index + 2]);
                                Symbol symbol = Symbol.FromJson(lines[index + 3]);
                                overlay.Graphics.Add(new Graphic(geometry, symbol));
                                index += 2;
                            }
                            index += 2;
                            _sketchOverlay = overlay;
                        }
                        SimpleMarkerSymbol markerSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, Color.Black, 5);
                        _distanceOverlay.Renderer = new SimpleRenderer(markerSymbol);
                        SketchLayerComboBox.ItemsSource = GraphicLayers;
                        SketchColorComboBox.SelectedIndex = GraphicLayers.Count - 1;
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show("Data file is missing!");
                InitializeDistance();
                InitializeSketch();
            }
            catch (IOException e)
            {
                MessageBox.Show("Init data is missing!");
            }
            catch (Exception e)
            {
                MessageBox.Show("Data could not be read.");
            }
        }
        #endregion Load data

        #region Wms Service config
        /// <summary>
        /// Updates the map with the latest layer selection
        /// </summary>
        private void UpdateMapDisplay(ObservableCollection<LayerDisplayVM> displayList)
        {
            // Remove all existing layers.
            MyMapView.Map.OperationalLayers.Clear();

            // Get a list of selected LayerInfos.
            List<WmsLayerInfo> selectedLayers = displayList.Where(vm => vm.IsEnabled).Select(vm => vm.Info).ToList();

            // Return if no layers are selected.
            if (!selectedLayers.Any())
            {
                return;
            }

            // Write the string array to a new file named "selectedlayers.txt".
            //using (StreamWriter outputFile = new StreamWriter("selectedlayers.txt"))
            //{
            //    foreach (var item in selectedLayers)
            //        outputFile.WriteLine(item.Name);
            //}

            // Create a new WmsLayer from the selected layers.
            _wmsLayer = new WmsLayer(selectedLayers);
            
            // Add the layer to the map.
            MyMapView.Map.OperationalLayers.Add(_wmsLayer);
        }


        private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            // Update the map. Note: updating selection is handled by the IsEnabled property on LayerDisplayVM.
            UpdateMapDisplay(_viewModelList);
        }

        private async void MyMapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            if (popupWmsData == null)
                return;
            // Hide the old result.
            popupWmsData.Browser.Visibility = Visibility.Collapsed;

            try
            {
                // Perform the identify operation.
                IdentifyLayerResult myIdentifyResult = await MyMapView.IdentifyLayerAsync(_wmsLayer, e.Position, 20, false);

                // Return if there's nothing to show.
                if (!myIdentifyResult.GeoElements.Any())
                {
                    return;
                }

                // Retrieve the identified feature, which is always a WmsFeature for WMS layers.
                WmsFeature identifiedFeature = (WmsFeature)myIdentifyResult.GeoElements[0];

                // Retrieve the WmsFeature's HTML content.
                string htmlContent = identifiedFeature.Attributes["HTML"].ToString();

                // Note that the service returns a boilerplate HTML result if there is no feature found.
                // This test should work for most arcGIS-based WMS services, but results may vary.
                //if (!htmlContent.Contains("OBJECTID"))
                //{
                //    // Return without showing the result.
                //    return;
                //}

                // Show the result.
                popupWmsData.Browser.Visibility = Visibility.Visible;
                popupWmsData.Browser.NavigateToString(htmlContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error");
            }
        }
        #endregion Wms Service config

        #region toolbar buttons
        private void Ruler_btn_Click(object sender, RoutedEventArgs e)
        {
            if (distance_panel.Visibility == Visibility.Collapsed)
            {
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

        private void Identify_btn_Click(object sender, RoutedEventArgs e)
        {
            popupWmsData = new PopupWmsData
            {
                Topmost = true
            };
            popupWmsData.Show();
        }

        public double GetMyMapViewScale
        {
            get => MyMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale).TargetScale;

        }

        private void Polyline_btn_Click(object sender, RoutedEventArgs e)
        {
            var obj = SketchLayerComboBox.SelectedValue as GraphicsOverlay;
            if (SketchLayerComboBox.SelectedIndex == -1)
            {
                MessageBox.Show("Select a layer, please!", "Warning", MessageBoxButton.OK);
                return;
            }
            else if (obj.Id == "Distance layer")
            {
                MessageBox.Show("Select another layer!\nYou can't draw on this layer.", "Warning", MessageBoxButton.OK);
                return;
            }
            var btn = (Button)sender;
            _selectedTag = btn.Tag.ToString();
            if (distance_panel.Visibility == Visibility.Collapsed)
            {
                distance_panel.Visibility = Visibility.Visible;

                //Mouse.OverrideCursor = Cursors.Pen;
                _distanceOverlay.Graphics.Clear();
            }

            Mouse.OverrideCursor = Cursors.Pen;
            MyMapView.DismissCallout();
            start_distance = true;
            start_polyline = true;
            first = last = null;
        }


        private void DrawShapes_btn_Click(object sender, RoutedEventArgs e)
        {
            var obj = SketchLayerComboBox.SelectedValue as GraphicsOverlay;
            if (SketchLayerComboBox.SelectedIndex == -1)
            {
                MessageBox.Show("Select a layer, please!", "Warning", MessageBoxButton.OK);
                return;
            }
            else if (obj.Id == "Distance layer")
            {
                MessageBox.Show("Select another layer!\nYou can't draw on this layer.", "Warning", MessageBoxButton.OK);
                return;
            }
            var btn = (Button)sender;
            _selectedTag = btn.Tag.ToString();
            switch (btn.Tag)
            {
                case "Rectangle":
                    {
                        Draw(SketchCreationMode.Rectangle);
                        break;
                    }
                case "Circle":
                    {
                        Draw(SketchCreationMode.Circle);
                        break;
                    }
                case "Polygon":
                    {
                        Draw(SketchCreationMode.Polygon);
                        break;
                    }
                //case "Polyline":
                //    {
                //        Draw(SketchCreationMode.Polyline);
                //        break;
                //    }
                case "Point":
                    {
                        Draw(SketchCreationMode.Point);
                        break;
                    }
                default:
                    break;

            }
        }

        private void DrawByValueButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = null;

                try
                {
                    MyMapView.SketchEditor.CancelCommand.Execute(null);
                }
                catch (Exception)
                {
                    // Ignore ... let the user cancel editing
                }

                if (_selectedTag == "Point")
                {
                    PopupPoint popup = new PopupPoint(MyMapView);
                    bool? result = popup.ShowDialog();
                    //if (result == true)
                    {
                        if (popup.MapPoint != null)
                            DrawPoint(popup);
                    }
                }
                else if (_selectedTag == "Polyline")
                {
                    PopupPolyline popup = new PopupPolyline(MyMapView);
                    bool? result = popup.ShowDialog();
                    //if (result == true)
                    {
                        if (popup.mapPoint != null && (popup.mapPoint2 != null || (popup.Bearing != -1 && popup.Distance != -1)))
                            DrawPolyline(popup);
                    }
                }
                else if (_selectedTag == "Circle")
                {
                    PopupCircle popup = new PopupCircle(MyMapView);
                    bool? result = popup.ShowDialog();
                    //if (result == true)
                    {
                        if (popup.Radius != 0)
                            DrawCircle(popup);
                    }
                }
                else if (_selectedTag == "Rectangle")
                {
                    PopupRect popup = new PopupRect(MyMapView);
                    bool? result = popup.ShowDialog();
                    //if (result == true)
                    {
                        if (popup.mapPoint != null && popup.mapPoint2 != null)
                            DrawRectangle(popup);
                    }
                }
                start_distance = start_polyline = false;
                first = last = null;
                _selectedTag = "";
            }
            catch(Exception)
            {

            }
        }

        private void CompleteDraw_btn_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = null;
            if (_sketchOverlay.Graphics.Count > 0)
            {
                ClearLayer_btn.IsEnabled = true;
                RenameLayer_btn.IsEnabled = true;
                DeleteLayer_btn.IsEnabled = true;
                ArrowMouse_btn.IsEnabled = true;
            }
            else
            {
                ClearLayer_btn.IsEnabled = false;
                RenameLayer_btn.IsEnabled = false;
                DeleteLayer_btn.IsEnabled = false;
                ArrowMouse_btn.IsEnabled = false;
            }
            start_distance = start_polyline = false;
            first = last = null;
            _selectedTag = "";
        }

        private void PanMouse_btn_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Hand;
            start_distance = start_polyline = false;
            first = last = null;

            try
            {
                MyMapView.SketchEditor.CancelCommand.Execute(null);
            }
            catch (Exception)
            {
                // Ignore ... let the user cancel editing
            }
        }

        private async void ArrowMouse_btn_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = null;
            start_distance = start_polyline = false;
            first = last = null;
            try
            {
                // Allow the user to select a graphic
                Graphic editGraphic = await GetGraphicAsync();
                if (editGraphic == null) { return; }
                else
                {
                    MessageBoxResult result = MessageBox.Show("Delete graphic?", "Delete shape", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        editGraphic.GraphicsOverlay.Graphics.Remove(editGraphic);
                        return;
                    }
                }
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

        private async void ZoomIn_btn_Click(object sender, RoutedEventArgs e)
        {
            await MyMapView.SetViewpointScaleAsync(GetMyMapViewScale - 100000);
        }

        private async void ZoomOut_btn_Click(object sender, RoutedEventArgs e)
        {
            await MyMapView.SetViewpointScaleAsync(GetMyMapViewScale + 100000);
        }

        private void RenameLayer_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _sketchOverlay = SketchLayerComboBox.SelectedValue as GraphicsOverlay;
                if (_sketchOverlay.Id == "Distance layer")
                {
                    MessageBox.Show("You can't rename this layer!", "Warning", MessageBoxButton.OK);
                    return;
                }
                PopupRename popup = new PopupRename(_sketchOverlay, GraphicLayers);
                popup.ShowDialog();
                if (popup.Graphics != null && GraphicLayers["Temp"] == null)
                {
                    GraphicLayers.Add(new GraphicsOverlay() { Id = "Temp" });
                }
                _sketchOverlay = GraphicLayers["Temp"];
                SketchLayerComboBox.ItemsSource = null;
                SketchLayerComboBox.ItemsSource = GraphicLayers;
                GraphicLayesrListBox.ItemsSource = null;
                GraphicLayesrListBox.ItemsSource = GraphicLayers;
            }
            catch(Exception)
            {
                MessageBox.Show("Please select a layer!", "Error", MessageBoxButton.OK);
            }
        }

        private void DeleteLayer_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _sketchOverlay = SketchLayerComboBox.SelectedValue as GraphicsOverlay;
                if (_sketchOverlay.Id == "Temp" || _sketchOverlay.Id == "Distance layer")
                {
                    MessageBox.Show("You can't delete this layer!", "Warning", MessageBoxButton.OK);
                    return;
                }
                MessageBoxResult result = MessageBox.Show("Are you sure to delete this layer?", "Delete layer", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                    return;
                GraphicLayers.Remove(_sketchOverlay);
                _sketchOverlay = GraphicLayers[0];

            if (_sketchOverlay.Graphics.Count > 0)
                {
                    ClearLayer_btn.IsEnabled = true;
                    RenameLayer_btn.IsEnabled = true;
                    DeleteLayer_btn.IsEnabled = true;
                    ArrowMouse_btn.IsEnabled = true;
                }
                else
                {
                    ClearLayer_btn.IsEnabled = false;
                    RenameLayer_btn.IsEnabled = false;
                    DeleteLayer_btn.IsEnabled = false;
                    ArrowMouse_btn.IsEnabled = false;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("can't delete this layer!", "Error", MessageBoxButton.OK);
            }
        }
        private void ClearButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _sketchOverlay = SketchLayerComboBox.SelectedValue as GraphicsOverlay;
                // Remove all graphics from the graphics overlay
                MessageBoxResult result = MessageBox.Show("All graphics of selected layer will be remove.\nAre you sure?", "Clear layer", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _sketchOverlay.Graphics.Clear();

                    // Disable buttons that require graphics
                    ClearLayer_btn.IsEnabled = false;
                    ArrowMouse_btn.IsEnabled = false;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Please select a layer!", "Error", MessageBoxButton.OK);
            }
        }

        private void SketchLayerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                //if(SketchLayerComboBox.SelectedIndex > GraphicLayers.Count -1)
                _sketchOverlay = SketchLayerComboBox.SelectedValue as GraphicsOverlay;
                if (_sketchOverlay.Id == "Distance layer")
                {
                    RenameLayer_btn.IsEnabled = false;
                    DeleteLayer_btn.IsEnabled = false;
                }
                else if (_sketchOverlay.Id == "Temp")
                {
                    DeleteLayer_btn.IsEnabled = false;
                    RenameLayer_btn.IsEnabled = true;
                }
                else
                {
                    RenameLayer_btn.IsEnabled = true;
                    DeleteLayer_btn.IsEnabled = true;
                }
                if (_sketchOverlay.Graphics.Count > 0)
                {
                    ClearLayer_btn.IsEnabled = true;
                    ArrowMouse_btn.IsEnabled = true;
                }
                else
                {
                    ClearLayer_btn.IsEnabled = false;
                    ArrowMouse_btn.IsEnabled = false;
                }

            }
            catch (Exception)
            {
                
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
                    {
                        symbol = new SimpleMarkerSymbol()
                        {
                            Color = color,
                            Style = SimpleMarkerSymbolStyle.Circle,
                            Size = 5d
                        };
                        break;
                    }
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

        #region Draw graphic methods

        private void DrawCircle(PopupCircle popup)
        {
            try
            {
                // Create a geodesic buffer graphic using the same location and distance.
                Geometry bufferGeometryGeodesic = GeometryEngine.BufferGeodetic(popup.MapPoint, popup.Radius, LinearUnits.NauticalMiles, double.NaN, GeodeticCurveType.Geodesic);
                Graphic geodesicBufferGraphic = CreateGraphic(bufferGeometryGeodesic, colors[SketchColorComboBox.SelectedIndex]);
                _sketchOverlay.Graphics.Add(CreateGraphic(popup.MapPoint, Color.Red));
                _sketchOverlay.Graphics.Add(geodesicBufferGraphic);
            }
            catch (Exception ex)
            {
                // Report exceptions
                MessageBox.Show("Error drawing graphic shape: " + ex.Message);
            }
        }

        private void DrawPoint(PopupPoint popup)
        {
            try
            {
                Graphic graphic = CreateGraphic(popup.MapPoint, colors[SketchColorComboBox.SelectedIndex]);
                _sketchOverlay.Graphics.Add(graphic);
            }
            catch (Exception ex)
            {
                // Report exceptions
                MessageBox.Show("Error drawing graphic shape: " + ex.Message);
            }
        }
        private void DrawPolyline(PopupPolyline popup)
        {
            try
            {
                // Create a geodesic buffer graphic using the same location and distance.
                MapPoint[] arr = new MapPoint[2];
                arr[0] = popup.mapPoint;
                if (popup.mapPoint2 != null)
                    arr[1] = popup.mapPoint2;
                else
                {
                    arr[1] = measure_second_point(arr[0].Y, arr[0].X, popup.Distance, popup.Bearing);
                }
                Polyline pline = new Polyline(arr);
                Color creationColor = colors[SketchColorComboBox.SelectedIndex];
                // Create and add a graphic from the geometry the user drew
                Graphic graphic = CreateGraphic(pline, creationColor);
                _sketchOverlay.Graphics.Add(graphic);
            }
            catch (Exception ex)
            {
                // Report exceptions
                MessageBox.Show("Error drawing graphic shape: " + ex.Message);
            }
        }
        private void DrawRectangle(PopupRect popup)
        {
            try
            {
                // Create a geodesic buffer graphic using the same location and distance.
                PointCollection pointCollection = new PointCollection(SpatialReferences.Wgs84);
                pointCollection.Add(popup.mapPoint);
                pointCollection.Add(new MapPoint(popup.mapPoint.X, popup.mapPoint2.Y));
                pointCollection.Add(popup.mapPoint2);
                pointCollection.Add(new MapPoint(popup.mapPoint2.X, popup.mapPoint.Y));
                pointCollection.Add(popup.mapPoint);
                var polygon = new Polygon(pointCollection);
                polygon.ToString();

                Graphic geodesicBufferGraphic = CreateGraphic(polygon, colors[SketchColorComboBox.SelectedIndex]);
                //_sketchOverlay.Graphics.Add(CreateGraphic(tappedPoint, Color.Red));
                _sketchOverlay.Graphics.Add(geodesicBufferGraphic);
            }
            catch(Exception ex)
            {
                // Report exceptions
                MessageBox.Show("Error drawing graphic shape: " + ex.Message);
            }
        }

        private async void Draw(SketchCreationMode creationMode)
        {
            try
            {
                // Let the user draw on the map view using the chosen sketch mode
                Mouse.OverrideCursor = Cursors.Pen;
                Color creationColor = colors[SketchColorComboBox.SelectedIndex];
                Geometry geometry = await MyMapView.SketchEditor.StartAsync(creationMode, true);

                // show length of the geometry with nm and km
                lblLineWidth.Text = String.Format("Length : {0:F4} NM ({1:F4} KM)", GeometryEngine.LengthGeodetic(geometry, LinearUnits.NauticalMiles), GeometryEngine.LengthGeodetic(geometry, LinearUnits.Kilometers));

                // Create and add a graphic from the geometry the user drew
                Graphic graphic = CreateGraphic(geometry, creationColor);
                _sketchOverlay.Graphics.Add(graphic);

                // Enable/disable the clear and edit buttons according to whether or not graphics exist in the overlay
                ClearLayer_btn.IsEnabled = _sketchOverlay.Graphics.Count > 0;
                ArrowMouse_btn.IsEnabled = _sketchOverlay.Graphics.Count > 0;
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

        #endregion Draw graphic methods

        #region Drag and drop support

        private void ListBox_DragPreviewMove(object sender, MouseButtonEventArgs e)
        {
            // This method is called when the user clicks and starts dragging a listbox item.
            _startPoint = e.GetPosition(null);

        }

        private void StartDrag(object sender, MouseEventArgs e)
        {
            if (sender is ListBoxItem)
            {
                // Get the listbox item that is being moved.
                ListBoxItem sendingItem = (ListBoxItem)sender;

                // Record that this item was being dragged - used later when drag ends to determine which item to move.
                _originatingListBoxItem = sendingItem;

                // Register the start of the drag & drop operation with the system.
                DragDrop.DoDragDrop(sendingItem, sendingItem.DataContext, DragDropEffects.Move);

                // Mark the dragged item as selected.
                sendingItem.IsSelected = true;
                dragAction = false;
            }
        }

        private void ListBoxItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is ListBoxItem)
            {
                if (Mouse.LeftButton == MouseButtonState.Pressed && !dragAction)
                {
                    Point position = e.GetPosition(null);
                    if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||

                        Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)

                    {
                        dragAction = true;
                        this.StartDrag(sender, e);
                    }
                }
            }
        }

        private void ListBoxItem_OnDrop(object sender, DragEventArgs e)
        {
            // This method is called when the user finishes dragging while over the listbox.

            // Find the source and destination list boxes.
            ListBox sourceBox = FindParentListBox(_originatingListBoxItem);
            if (sourceBox == null)
            {
                // Return if the source isn't valid - happens when duplicate events are raised.
                return;
            }

            // Find the list box that the item was dropped on (i.e. dragged to).
            ListBox destinationBox = FindParentListBox((UIElement)sender);

            // Get the data that is being dropped.
            GraphicsOverlay draggedData = (GraphicsOverlay)e.Data.GetData(typeof(GraphicsOverlay));

            // Find where in the respective lists the items are.
            int indexOfRemoved = sourceBox.Items.IndexOf(draggedData);
            int indexOfInsertion;

            // Sender is the control that the item is being dropped on. Could be a listbox or a listbox item.
            if (sender is ListBoxItem)
            {
                // Find the layer that the item represents.
                GraphicsOverlay targetData = ((ListBoxItem)sender).DataContext as GraphicsOverlay;

                // Find the position of the layer in the listbox.
                indexOfInsertion = destinationBox.Items.IndexOf(targetData);
            }
            else if (destinationBox != sourceBox)
            {
                // Drop the item at the end of the list if the user let go of the item on the empty space in the box rather than the list item.
                // This works because both the listbox and its individual listbox items participate in drag and drop.
                indexOfInsertion = destinationBox.Items.Count - 1;
            }
            else
            {
                return;
            }

            //// Find the appropriate source and destination boxes.
            //LayerCollection sourceList = sourceBox == IncludedListBox ? _viewModel.IncludedLayers : _viewModel.ExcludedLayers;
            //LayerCollection destinationList = destinationBox == IncludedListBox ? _viewModel.IncludedLayers : _viewModel.ExcludedLayers;

            // Return if there is nothing to do.
            if (indexOfRemoved == indexOfInsertion)
            {
                return;
            }

            indexOfInsertion -= 1;

            // Perform the move.
            object obj = SketchLayerComboBox.SelectedItem;
            GraphicLayers.RemoveAt(indexOfRemoved);
            GraphicLayers.Insert(indexOfInsertion + 1, draggedData);
            SketchLayerComboBox.SelectedItem = obj;

        }

        private static ListBox FindParentListBox(UIElement source)
        {
            // This is needed because it is hard to tell which listbox an item belongs to.

            // Walk up the visual element tree until a ListBox is found.
            UIElement parentElement = source;
            // While the parent element is not a listbox and the parent element is not null,
            while (!(parentElement is ListBox) && parentElement != null)
            {
                // find the next parent.
                parentElement = System.Windows.Media.VisualTreeHelper.GetParent(parentElement) as UIElement;
            }

            return parentElement as ListBox;
        }
        private void AddNewGraphicLayer_btn_Click(object sender, RoutedEventArgs e)
        {
            GraphicsOverlay g = new GraphicsOverlay()
            {
                Id = "layer " + (MyMapView.GraphicsOverlays.Count)
            };
            GraphicLayers.Add(g);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
        }
        #endregion Drag and drop support

        #region Save

        public void Save_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Geometry.FromJson();
                // Create a string array with the lines of text
                List<string> graphicsOverlays = new List<string>();
                graphicsOverlays.Add(MyMapView.GraphicsOverlays.Count.ToString());
                foreach (var item in MyMapView.GraphicsOverlays)
                {
                    graphicsOverlays.Add(item.Id);
                    graphicsOverlays.Add(item.Graphics.Count.ToString());
                    foreach (var graphic in item.Graphics)
                    {
                        graphicsOverlays.Add(graphic.Geometry.ToJson());
                        graphicsOverlays.Add(graphic.Symbol.ToJson());
                    }
                }

                // Set a variable to the Documents path.
                //string docPath =
                //  Environment.GetFolderPath(Environment.SpecialFolder.LocalizedResources);

                // Write the string array to a new file named "datafile.txt".
                using (StreamWriter outputFile = new StreamWriter("datafile.txt"))
                {
                    foreach (string line in graphicsOverlays)
                        outputFile.WriteLine(line);
                }

                MessageBox.Show("All data Saved.", "Save", MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error");
            }
        }

        private async void Screen_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Wait for rendering to finish before taking the screenshot.
                await WaitForRenderCompleteAsync(MyMapView);

                // Export the image from MapView.
                RuntimeImage image = await MyMapView.ExportImageAsync();

                // Display the image in the UI.
                var img = await image.GetEncodedBufferAsync();

                SaveFileDialog dlg = new SaveFileDialog();
                dlg.FileName = "Document"; // Default file name
                                           // dlg.DefaultExt = ".jpg"; // Default file extension
                dlg.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|PNG Image|*.png";
                if (dlg.ShowDialog() == true)
                {
                    string fName = dlg.FileName;
                    if (dlg.FileName != "")
                    {
                        var encoder = new PngBitmapEncoder(); // Or PngBitmapEncoder, or whichever encoder you want
                        encoder.Frames.Add(BitmapFrame.Create(img));
                        using (var stream = dlg.OpenFile())
                        {
                            encoder.Save(stream);
                        }
                    }
                }
                //var _kmlDocument = new KmlDocument() { Name = "KML Sample Document" };

                //// Create a KML dataset using the KML document.
                //var _kmlDataset = new KmlDataset(_kmlDocument);

                //// Create the KML layer using the KML dataset.
                //var _kmlLayer = new KmlLayer(_kmlDataset);
                //await MyMapView.Map.SaveAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error");
            }
        }

        private static Task WaitForRenderCompleteAsync(MapView mapview)
        {
            // The task completion source manages the task, including marking it as finished when the time comes.
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            // If the map is currently finished drawing, set the result immediately.
            if (mapview.DrawStatus == DrawStatus.Completed)
            {
                tcs.SetResult(null);
            }
            // Otherwise, configure a callback and a timeout to either set the result when
            // the map is finished drawing or set the result after 2000 ms.
            else
            {
                // Define a cancellation token source for 2000 ms.
                const int timeoutMs = 2000;
                var ct = new CancellationTokenSource(timeoutMs);

                // Register the callback that sets the task result after 2000 ms.
                ct.Token.Register(() =>
                    tcs.TrySetResult(null), false);


                // Define a local function that will set the task result and unregister itself when the map finishes drawing.
                void DrawCompleteHandler(object s, DrawStatusChangedEventArgs e)
                {
                    if (e.Status == DrawStatus.Completed)
                    {
                        mapview.DrawStatusChanged -= DrawCompleteHandler;
                        tcs.TrySetResult(null);
                    }
                }

                // Register the draw complete event handler.
                mapview.DrawStatusChanged += DrawCompleteHandler;
            }

            // Return the task.
            return tcs.Task;
        }

        #endregion Save

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

        #endregion

        #region Ruler parts
        private void MapView_Tapped(object sender, GeoViewInputEventArgs e)
        {
            // Get the tapped point - this is in the map's spatial reference,
            // which in this case is WebMercator because that is the SR used by the included basemaps.
            MapPoint tappedPoint = MyMapView.ScreenToLocation(e.Position);
            // Update the graphics.
            if (start_distance)
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
                    start_polyline = false;
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
                string projectedCoords = CoordinateFormatter.ToLatitudeLongitude(projectedPoint, LatitudeLongitudeFormat.DegreesMinutesSeconds, 2); //string.Format("lat: {1:F4}, lng: {0:F4}", projectedPoint.X, projectedPoint.Y);
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
            string first_projectedCoord = string.Format("{0:F4}, {1:F4}", first.Y, first.X);
            string last_projectedCoord = string.Format("{0:F4}, {1:F4}", projectedPoint.Y, projectedPoint.X);

            // show the results in the text blocks
            coordinate_text.Text = string.Format("{0} : {1} && {2}", "Coordiantes",
                first_projectedCoord, last_projectedCoord);
            //_distance = distance(first.Y, first.X, projectedPoint.Y, projectedPoint.X, distance_type[0]);
            //distance_text.Text = string.Format("{0} : {1:F1} {2}", "Distance",
            //    _distance, distance_type);
            //_angle = DegreeBearing(first.Y, first.X, projectedPoint.Y, projectedPoint.X);
            //angle_text.Text = string.Format("{0} : {1:F0}", "Radial/Bearing", _angle);
            measure_distance(first, projectedPoint, distance_type);

            MapPoint[] arr = new MapPoint[2];
            arr[0] = first;
            arr[1] = projectedPoint;
            Polyline pline = new Polyline(arr);
            Color creationColor = colors[SketchColorComboBox.SelectedIndex];
            // Create and add a graphic from the geometry the user drew
            Graphic graphic = CreateGraphic(pline, creationColor);
            if (start_polyline && last != null)
            {
                _distanceOverlay.Graphics.Clear();
                _sketchOverlay.Graphics.Add(graphic);
                ClearLayer_btn.IsEnabled = _sketchOverlay.Graphics.Count > 0;
                ArrowMouse_btn.IsEnabled = _sketchOverlay.Graphics.Count > 0;
                _selectedTag = "";
            }
            else
            {
                _distanceOverlay.Graphics.Add(new Graphic(first));
                _distanceOverlay.Graphics.Add(new Graphic(projectedPoint));
                _distanceOverlay.Graphics.Add(graphic);
            }
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
                measure_distance(first, last, distance_type);
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
        private double measure_distance(MapPoint mapPoint1, MapPoint mapPoint2, string un)
        {
            LinearUnit unit = LinearUnits.Kilometers;
            switch (un)
            {
                case "Km":
                    unit = LinearUnits.Kilometers;
                    break;
                case "M":
                    unit = LinearUnits.Miles;
                    break;
                case "NM":
                    unit = LinearUnits.NauticalMiles;
                    break;
            }
            try
            {
                GeodeticDistanceResult result = GeometryEngine.DistanceGeodetic(mapPoint1, mapPoint2, unit, AngularUnits.Degrees, GeodeticCurveType.Geodesic);
                double azimuth = (result.Azimuth1 + 360) % 360;
                angle_text.Text = string.Format("{0} : {1:F1}", "Radial/Bearing", azimuth);
                _distance = result.Distance;
                distance_text.Text = string.Format("{0} : {1:F1} {2}", "Distance", _distance, distance_type);
                return _distance;
            }
            catch (Exception) { return 0; }
        }

        private MapPoint measure_second_point(double lat1, double lon1, double distance, double bearing)
        {
            double angular_dis = distance / 6371;
            lat1 = ToRad(lat1);
            lon1 = ToRad(lon1);
            bearing = ToRad(bearing);
            double lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(angular_dis) + Math.Cos(lat1) * Math.Sin(angular_dis) * Math.Cos(bearing));
            double lon2 = lon1 + Math.Atan2(Math.Sin(bearing) * Math.Sin(angular_dis) * Math.Cos(lat1), Math.Cos(angular_dis) - Math.Sin(lat1) * Math.Sin(lat2));
            lat2 = ToDegrees(lat2);
            lon2 = ToDegrees(lon2);
            return new MapPoint(lon2, lat2);
        }

        #endregion

        #region Goto
        private void Goto_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PopupGoto popupGoto = new PopupGoto(MyMapView);
                bool? check = popupGoto.ShowDialog();
                if (check == false && popupGoto.LatLng != null)
                    _sketchOverlay.Graphics.Add(CreateGraphic(popupGoto.LatLng, Color.Red));
            }
            catch (Exception)
            {
                MessageBox.Show("Error in the format of Coords!", "Error", MessageBoxButton.OK);
            }
        }
        #endregion GOTO

        #region Key down

        private void MyMapView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Mouse.OverrideCursor = null;
                start_distance = start_polyline = false;
                first = last = null;
                try
                {
                    MyMapView.SketchEditor.CancelCommand.Execute(null);
                }
                catch (Exception)
                {
                    // Ignore ... let the user cancel editing
                }
            }
        }
        #endregion Key down
    }

}
