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

namespace RCCMap
{
    /// <summary>
    /// Interaction logic for PopupPolyline.xaml
    /// </summary>
    public partial class PopupPolyline : Window
    {
        public PopupPolyline(MapView mapView)
        {
            InitializeComponent();
            this.mapView = mapView;
            bearing = distance = -1;
        }
        private MapPoint mappoint;
        private MapPoint mappoint2;
        private double bearing;
        private double distance;
        private MapView mapView;

        public MapPoint mapPoint
        {
            get { return mappoint; }
        }

        public MapPoint mapPoint2 { get => mappoint2; }
        public double Bearing { get => bearing; }
        public double Distance { get => distance; }

        private async void DrawRectanglebtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (RadioPoint.IsChecked == true)
                {
                    if (RectLatLngTextField.Text.Trim() == "" || RectLatLng2TextField.Text.Trim() == "")
                        MessageBox.Show("fields cannot be empty!", "error", MessageBoxButton.OK);
                    else
                    {
                        mappoint =
                            CoordinateFormatter.FromLatitudeLongitude(RectLatLngTextField.Text, mapView.SpatialReference);
                        mappoint2 =
                            CoordinateFormatter.FromLatitudeLongitude(RectLatLng2TextField.Text, mapView.SpatialReference);

                        await mapView.SetViewpointCenterAsync(mapPoint);
                        // Set the Viewpoint scale to match the specified scale 
                        await mapView.SetViewpointScaleAsync(876200);
                        this.Close();
                    }
                }
                else
                {
                    if (RectLatLngTextField.Text.Trim() == "" || BearingTextField.Text.Trim() == "" || DistanceTextField.Text.Trim() == "")
                        MessageBox.Show("fields cannot be empty!", "error", MessageBoxButton.OK);
                    else
                    {
                        mappoint =
                            CoordinateFormatter.FromLatitudeLongitude(RectLatLngTextField.Text, mapView.SpatialReference);
                        bearing = Convert.ToDouble(BearingTextField.Text.Trim().Replace('.', '/'));
                        if (bearing < 0 || bearing >= 360)
                        {
                            MessageBox.Show("bearing cannot be more than 360 or less than 0!", "error", MessageBoxButton.OK);
                            return;
                        }
                        distance = Convert.ToDouble(DistanceTextField.Text.Trim().Replace('.', '/'));

                        await mapView.SetViewpointCenterAsync(mapPoint);
                        // Set the Viewpoint scale to match the specified scale 
                        await mapView.SetViewpointScaleAsync(876200);
                        this.Close();
                    }
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
