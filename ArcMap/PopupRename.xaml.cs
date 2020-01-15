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
using System.Windows.Shapes;

namespace ArcMap
{
    /// <summary>
    /// Interaction logic for popupRename.xaml
    /// </summary>
    public partial class PopupRename : Window
    {
        public PopupRename(GraphicsOverlay overlay, GraphicsOverlayCollection graphics)
        {
            InitializeComponent();
            this.graphics = graphics;
            this.overlay = overlay;
        }
        private GraphicsOverlay overlay;
        private GraphicsOverlayCollection graphics;
        private string name;
        public GraphicsOverlayCollection Graphics { get => graphics; }

        private void Rename_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NameTextField.Text.Trim() != "")
                {
                    this.name = NameTextField.Text.Trim();
                    if (Graphics[overlay.Id] != null && Graphics[this.name] == null)
                    {
                        overlay.Id = this.name;
                        MessageBox.Show("Layer renamed successfully!", "Rename");
                        this.Close();
                    }
                    else
                    {
                        this.name = "";
                        MessageBox.Show("Layer with this name already exist!", "Error");
                    }
                }
                else
                {
                    MessageBox.Show("Name field can't be empty!", "Error");
                }
            }
            catch(Exception)
            {
                MessageBox.Show("This layer cannot renamed!", "Error");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.name = overlay.Id;
            NameTextField.Text = overlay.Id;
        }
    }
}
