using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
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

namespace ArcMap
{
    /// <summary>
    /// Interaction logic for PopupPoint.xaml
    /// </summary>
    public partial class PopupPoint : Window
    {
        public PopupPoint(MapView mapView)
        {
            InitializeComponent();
            this.mapView = mapView;
        }

        private MapView mapView;
        private MapPoint mapPoint;

        public MapPoint MapPoint
        {
            get { return mapPoint; }
        }

        private async void DrawPointbtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (LatLngTextField.Text.Trim() == "")
                    MessageBox.Show("Lat & long field cannot be empty!", "error", MessageBoxButton.OK);
                else
                {
                    mapPoint =
                        CoordinateFormatter.FromLatitudeLongitude(LatLngTextField.Text, mapView.SpatialReference);

                    await mapView.SetViewpointCenterAsync(mapPoint);

                    // Set the Viewpoint scale to match the specified scale 
                    await mapView.SetViewpointScaleAsync(876200);
                    //mapView.GraphicsOverlays["Temp"].Graphics.Add(new Esri.ArcGISRuntime.UI.Graphic(mapPoint));
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                // The coordinate is malformed, warn and return
                MessageBox.Show(ex.Message, "Invalid format");
                return;
            }
        }
    }
}