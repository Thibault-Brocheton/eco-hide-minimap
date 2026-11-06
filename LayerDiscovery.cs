namespace CavRn.HideMinimap
{
    using Eco.Core.Plugins;
    using Eco.Core.Serialization;
    using Eco.Shared.Math;
    using Eco.Shared.Serialization;
    using Eco.Shared.Voxel;
    using Eco.Simulation.WorldLayers;
    using Eco.Simulation.WorldLayers.Layers;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>Saved plots discovered through the reveal layer, as a plot bitmask.</summary>
    [Serialized]
    public class LayerDiscoveryData : IStorage
    {
        public IPersistent StorageHandle { get; set; }
        [Serialized] public byte[] Mask;
    }

    /// <summary>Reveals plots where a world layer (e.g. PlayerTrampled or PlayerActivity) reaches a minimum value, sampled at each plot center after every layer tick.
    /// Permanent mode keeps a plot once discovered (fog of war); live mode only shows plots currently above the threshold.</summary>
    public static class LayerDiscovery
    {
        static readonly object     sync = new();
        static HashSet<Vector2i>   discoveredPlots = new();
        static LayerDiscoveryData  data;

        static HideMinimapConfig Config => HideMinimapPlugin.Obj.Config;

        public static int DiscoveredPlotCount => discoveredPlots.Count;

        public static bool IsPlotDiscovered(Vector2i plotXZ) => discoveredPlots.Contains(plotXZ);

        /// <summary>The configured reveal layer, or null when the feature is off or the name matches no layer.</summary>
        public static WorldLayer Layer => string.IsNullOrWhiteSpace(Config.RevealLayerName) ? null : WorldLayerManager.Obj?.GetLayer(Config.RevealLayerName.Trim());

        public static void Initialize()
        {
            data            = StorageManager.LoadOrCreate<LayerDiscoveryData>("HideMinimapLayerDiscovery");
            discoveredPlots = PlotMask.Unpack(data.Mask);
            WorldLayerManager.Obj.OnTicked.Add(() => Update());
        }

        /// <summary>Samples the layer and refreshes the minimap when the discovered set changed. Only runs while the minimap is hidden, so nothing is discovered while the map is visible.</summary>
        public static void Update()
        {
            if (!MinimapReveal.IsActive) return;
            var layer = Layer;
            if (layer == null) return;

            bool changed;
            lock (sync)
            {
                var current = PlotsAbove(layer, Config.RevealLayerMinValue);
                if (Config.RevealLayerIsPermanent)
                {
                    var before = discoveredPlots.Count;
                    discoveredPlots.UnionWith(current);
                    changed = discoveredPlots.Count != before;
                }
                else
                {
                    changed         = !current.SetEquals(discoveredPlots);
                    discoveredPlots = current;
                }
                if (changed) Save();
            }
            if (changed) MinimapReveal.Refresh();
        }

        /// <summary>Forgets every discovered plot; plots still above the threshold come back on the next layer tick.</summary>
        public static void Reset()
        {
            lock (sync)
            {
                discoveredPlots = new HashSet<Vector2i>();
                Save();
            }
            MinimapReveal.Refresh();
        }

        static HashSet<Vector2i> PlotsAbove(WorldLayer layer, float minValue)
        {
            var center = new Vector2i(PlotUtil.PlotSize / 2, PlotUtil.PlotSize / 2);
            return PlotMask.PlotDims.XYIter().Where(plotXZ => layer.EntryWorldPos((plotXZ * PlotUtil.PlotSize) + center) >= minValue).ToHashSet();
        }

        static void Save()
        {
            data.Mask = PlotMask.Pack(discoveredPlots);
            StorageManager.Obj.MarkDirty(data);
        }
    }
}
