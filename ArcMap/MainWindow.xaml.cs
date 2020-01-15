using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.UI;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ArcMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            string licenseKey = "runtimelite,1000,rud5233433699,none,NKMFA0PL4S4JLMZ59219";
            ArcGISRuntimeEnvironment.SetLicense(licenseKey);
            this.Hide();
            SplashScreen splash = new SplashScreen(this);
            splash.Show();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure?", "Exit", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            { }
            else
                e.Cancel = true;
        }

        // Map initialization logic is contained in MapViewModel.cs

    }
}
