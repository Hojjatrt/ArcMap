using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ArcMap
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Window
    {
        DispatcherTimer dis;
        private MainWindow main;
        public SplashScreen(MainWindow main)
        {
            InitializeComponent();
            dis = new DispatcherTimer();
            dis.Interval = new TimeSpan(0, 0, 1);
            dis.Start();
            this.main = main;
            dis.Tick += Dis_Tick;
        }

        int i = 0;
        private void Dis_Tick(object sender, EventArgs e)
        {
            i += 1;
            if (i == 4)
            {
                dis.Stop();
                
                main.Show();
                this.Close();
                GC.Collect();
            }
        }
    }
}
