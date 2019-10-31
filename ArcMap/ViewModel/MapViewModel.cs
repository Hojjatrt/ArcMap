using Esri.ArcGISRuntime.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArcMap.ViewModel
{
    class MapViewModel
    {
        public Map Map { get; set; }

        public LayerCollection IncludedLayers
        {
            get { return Map.OperationalLayers; }
        }

        public LayerCollection ExcludedLayers { get; set; }

        public MapViewModel(Map map)
        {
            Map = map;
            ExcludedLayers = new LayerCollection();
        }

        public void AddLayerFromUrl(string layerUrl)
        {
            ArcGISMapImageLayer layer = new ArcGISMapImageLayer(new Uri(layerUrl));
            Map.OperationalLayers.Add(layer);
        }
    }
}
