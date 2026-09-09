namespace CavRn.HideMinimap
{
    using Eco.Core.Plugins.Interfaces;
    using Eco.Core.Plugins;
    using Eco.Core.Utils;
    using Eco.Core.Utils.Threading;
    using Eco.Gameplay.Civics.Districts;
    using Eco.Shared.Localization;
    using Eco.Shared.Utils;
    using System.Collections.Generic;
    using System;

    public class HideMinimapMod : IModInit
    {
        public static ModRegistration Register() => new()
        {
            ModName        = "HideMinimap",
            ModDescription = "Hides the minimap except inside chosen districts, to create some Roleplay.",
            ModDisplayName = "Hide Minimap"
        };
    }

    public class HideMinimapConfig : Singleton<HideMinimapConfig>
    {
        public bool HideMinimap { get; set; } = false;
        public bool KillMinimap { get; set; } = false;

        /// <summary>World height (0-255) of the flat fog terrain on the minimap; -1 = client default.</summary>
        public int HiddenMapHeight { get; set; } = -1;

        /// <summary>Fog color as RRGGBB hex (e.g. "1a3350"); empty = client default.</summary>
        public string HiddenMapColor { get; set; } = "";

        /// <summary>Districts whose minimap is visible, as "MapName/DistrictName". "MapName/" reveals the whole map.</summary>
        public List<string> RevealedDistricts { get; set; } = new();
    }

    [Priority(PriorityAttribute.Low)]
    public class HideMinimapPlugin : Singleton<HideMinimapPlugin>, IModKitPlugin, IInitializablePlugin, IConfigurablePlugin
    {
        public IPluginConfig PluginConfig => this.config;
        readonly PluginConfig<HideMinimapConfig> config;
        public HideMinimapConfig Config => this.config.Config;
        public ThreadSafeAction<object, string> ParamChanged { get; set; } = new();

        public HideMinimapPlugin()
        {
            this.config = new PluginConfig<HideMinimapConfig>("HideMinimap");
            this.SaveConfig();
        }

        public string GetStatus()   => MinimapReveal.IsActive ? $"Hidden, {MinimapReveal.RevealedChunkCount} chunks revealed" : "Visible";
        public string GetCategory() => Localizer.DoStr("Mods");

        public void Initialize(TimedTask timer)
        {
            if (this.Config.HideMinimap) MinimapReveal.Activate();
            if (this.Config.KillMinimap) HideMinimapCommands.KillMap();

            //Districts grow/shrink through civics: follow them so revealed zones stay in sync without any admin action.
            DistrictMap.DistrictsUpdatedEvent.Add(_ => MinimapReveal.Refresh());
        }

        public object GetEditObject() => this.config.Config;
        public void OnEditObjectChanged(object o, string param) { this.SaveConfig(); }
    }
}
