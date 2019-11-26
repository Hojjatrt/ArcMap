using Esri.ArcGISRuntime.Ogc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ArcMap.ViewModel
{
    /// <summary>
    /// This is a ViewModel class for maintaining the state of a layer selection.
    /// Typically, this would go in a separate file, but it is included here for clarity.
    /// </summary>
    public class LayerDisplayVM : INotifyPropertyChanged
    {
        private bool _isEnabled;

        public WmsLayerInfo Info { get; set; }

        // True if layer is selected for display.
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { Select(value); }
        }

        public List<LayerDisplayVM> Children { get; set; }

        private LayerDisplayVM Parent { get; set; }

        public LayerDisplayVM(WmsLayerInfo info, LayerDisplayVM parent)
        {
            Info = info;
            Parent = parent;
        }

        // Select this layer and all child layers.
        private void Select(bool isSelected = true)
        {
            _isEnabled = isSelected;
            if (Children == null)
            {
                return;
            }

            foreach (var child in Children)
            {
                child.Select(isSelected);
            }
            OnPropertyChanged("IsEnabled");
        }

        // Override ToString to enhance display formatting.
        public override string ToString()
        {
            return Info.Title;
        }

        public static void BuildLayerInfoList(LayerDisplayVM root, IList<LayerDisplayVM> result)
        {
            // Add the root node to the result list.
            result.Add(root);

            // Initialize the child collection for the root.
            root.Children = new List<LayerDisplayVM>();

            // Recursively add sublayers.
            foreach (WmsLayerInfo layer in root.Info.LayerInfos)
            {
                // Create the view model for the sublayer.
                LayerDisplayVM layerVM = new LayerDisplayVM(layer, root);

                // Add the sublayer to the root's sublayer collection.
                root.Children.Add(layerVM);

                // Recursively add children.
                BuildLayerInfoList(layerVM, result);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
