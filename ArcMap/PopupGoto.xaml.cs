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
    /// Interaction logic for PopupGoto.xaml
    /// </summary>
    public partial class PopupGoto : Window
    {
        public PopupGoto(MapView mapView)
        {
            this.mapView = mapView;
            InitializeComponent();
        }
        private MapView mapView;
        private MapPoint mapPoint;
        public MapPoint LatLng { get => mapPoint; }

        private async void Goto_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                if (GotoTextField.Text.Trim() == "")
                    MessageBox.Show("fields cannot be empty!", "error", MessageBoxButton.OK);
                else
                {
                    mapPoint =
                        CoordinateFormatter.FromLatitudeLongitude(GotoTextField.Text, mapView.SpatialReference);
                    await mapView.SetViewpointCenterAsync(mapPoint);

                    // Set the Viewpoint scale to match the specified scale 
                    await mapView.SetViewpointScaleAsync(876200);
                    //mapView.GraphicsOverlays["Temp"].Graphics.Add(new Esri.ArcGISRuntime.UI.Graphic(mapPoint));
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
