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
        public PopupRect()
        {
            InitializeComponent();
            check = false;
        }
        private string mappoint;
        private string mappoint2;
        private bool check;

        public string mapPoint
        {
            get { return mappoint; }
        }

        public string mapPoint2 { get => mappoint2; set => mappoint2 = value; }

        private void DrawRectanglebtn_Click(object sender, RoutedEventArgs e)
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
            if (DDRadiobtn2.IsChecked == true)
            {
                if (DDlatTextField2.Text.Trim() == "" || DDlngTextField2.Text.Trim() == "")
                {
                    check = false;
                    MessageBox.Show("lat lng field cannot be empty!", "error", MessageBoxButton.OK);
                }
                else
                {
                    check = true;
                    mappoint2 = DDlatTextField2.Text + "N" + " " + DDlngTextField2.Text + "E";
                }
            }
            else
            {
                if (DmsLTextField2.Text.Trim() == "" || DmsRTextField2.Text.Trim() == "")
                {
                    check = false;
                    MessageBox.Show("lat lng field cannot be empty!", "error", MessageBoxButton.OK);
                }
                else
                {
                    check = true;
                    mappoint2 = DmsLTextField2.Text + "N" + " " + DmsRTextField2.Text + "E";
                }
            }
            if (check)
                this.Close();
        }
    }
}
