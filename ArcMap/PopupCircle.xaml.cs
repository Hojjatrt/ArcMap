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
    /// Interaction logic for Popup.xaml
    /// </summary>
    public partial class PopupCircle : Window
    {
        public PopupCircle(MapView mapView)
        {
            InitializeComponent();
            check = false;
            this.mapView = mapView;
        }

        private MapView mapView;
        private bool check;
        private double radius;
        private MapPoint mapPoint;

        public MapPoint MapPoint
        {
            get { return mapPoint; }
        }
        public double Radius
        {
            get { return radius; }
        }


        private async void DrawCirclebtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (CircleLatLngTextField.Text.Trim() == "")
                    MessageBox.Show("Lat & long field cannot be empty!", "error", MessageBoxButton.OK);
                else
                {
                    mapPoint =
                        CoordinateFormatter.FromLatitudeLongitude(CircleLatLngTextField.Text, mapView.SpatialReference);
                    check = true;
                }
                if (check)
                {
                    try
                    {
                        if (RadiusTextField.Text.Trim() != "")
                        {
                            radius = Convert.ToDouble(RadiusTextField.Text);

                            await mapView.SetViewpointCenterAsync(mapPoint);

                            // Set the Viewpoint scale to match the specified scale 
                            await mapView.SetViewpointScaleAsync(876200);
                            //mapView.GraphicsOverlays["Temp"].Graphics.Add(new Esri.ArcGISRuntime.UI.Graphic(mapPoint));
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Radius field cannot be empty!", "error", MessageBoxButton.OK);
                            check = false;
                        }
                    }
                    catch(Exception)
                    {
                        MessageBox.Show("Radius field must be a number!", "error", MessageBoxButton.OK);
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
