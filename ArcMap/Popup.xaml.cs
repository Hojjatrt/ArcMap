﻿using Esri.ArcGISRuntime.Geometry;
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
    public partial class Popup : Window
    {
        public Popup()
        {
            InitializeComponent();
            check = false;
        }

        private bool check;
        private string mappoint;
        private double radius;

        public string mapPoint
        {
            get { return mappoint; }
        }
        public double Radius
        {
            get { return radius; }
        }


        private void DrawCirclebtn_Click(object sender, RoutedEventArgs e)
        {
            if (DDRadiobtn.IsChecked == true)
            {
                if (DDlatTextField.Text.Trim() == "" || DDlngTextField.Text.Trim() == "")
                    MessageBox.Show("lat lng field cannot be empty!", "error", MessageBoxButton.OK);
                else
                {
                    check = true;
                    mappoint = DDlatTextField.Text + "N" + " " + DDlngTextField.Text + "E";
                }
            }
            else
            {
                if (DmsLTextField.Text.Trim() == "" || DmsRTextField.Text.Trim() == "")
                    MessageBox.Show("lat lng field cannot be empty!", "error", MessageBoxButton.OK);
                else
                {
                    check = true;
                    mappoint = DmsLTextField.Text + "N" + " " + DmsRTextField.Text + "E";
                }
            }

            if (check)
            {
                if (RadiusTextField.Text.Trim() != "")
                    radius = Convert.ToDouble(RadiusTextField.Text);
                else
                {
                    MessageBox.Show("Radius field cannot be empty!", "error", MessageBoxButton.OK);
                    check = false;
                }
            }
            if (check)
                this.Close();
        }
    }
}
