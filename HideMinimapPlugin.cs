namespace CavRn.HideMinimap
{
    using Eco.Core.Plugins.Interfaces;
    using Eco.Core.Plugins;
    using Eco.Core.Utils;
    using Eco.Shared.Localization;
    using Eco.Shared.Math;
    using Eco.Shared.Utils;
    using System.Collections.Generic;
    using System;

    public class HideMinimapMod: IModInit
    {
        public static ModRegistration Register() => new()
        {
            ModName = "HideMinimap",
            ModDescription = "Allows to hide the minimap to create some Roleplay.",
            ModDisplayName = "Hide Minimap"
        };
    }

    public class HideMinimapConfig: Singleton<HideMinimapConfig>
    {
        public bool HideMinimap { get; set; } = false;
    }

    [Priority(PriorityAttribute.Low)]
    public class HideMinimapPlugin: Singleton<HideMinimapPlugin>, IModKitPlugin, IInitializablePlugin, IConfigurablePlugin
    {
        public static List<ThreadSafeActionBase<Action<Vector2i>>.DelegateDebug> OnTopOrWaterBlockCacheChangedSavedCallbacks = new List<ThreadSafeActionBase<Action<Vector2i>>.DelegateDebug>();

        public static ThreadSafeAction OnSettingsChanged = new();
        public IPluginConfig PluginConfig => this.config;
        private readonly PluginConfig<HideMinimapConfig> config;
        public HideMinimapConfig Config => this.config.Config;
        public ThreadSafeAction<object, string> ParamChanged { get; set; } = new();

        public HideMinimapPlugin()
        {
            this.config = new PluginConfig<HideMinimapConfig>("HideMinimap");
            this.SaveConfig();
        }

        public string GetStatus()
        {
            return "OK";
        }

        public string GetCategory()
        {
            return Localizer.DoStr("Mods");
        }

        public void Initialize(TimedTask timer)
        {
            if (this.Config.HideMinimap)
            {
                HideMinimapCommands.Hide();
            }
        }

        public object GetEditObject() => this.config.Config;
        public void OnEditObjectChanged(object o, string param) { this.SaveConfig(); }
    }
}


