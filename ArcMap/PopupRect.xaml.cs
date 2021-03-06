﻿using Esri.ArcGISRuntime.Geometry;
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
    /// Interaction logic for PopupRect.xaml
    /// </summary>
    public partial class PopupRect : Window
    {
        public PopupRect(MapView mapView)
        {
            InitializeComponent();
            this.mapView = mapView;
        }
        private MapPoint mappoint;
        private MapPoint mappoint2;
        private MapView mapView;

        public MapPoint mapPoint
        {
            get { return mappoint; }
        }

        public MapPoint mapPoint2 { get => mappoint2; }

        private async void DrawRectanglebtn_Click(object sender, RoutedEventArgs e)
        {
            try
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
