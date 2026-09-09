namespace CavRn.HideMinimap
{
    using Eco.Core.Plugins.Interfaces;
    using Eco.Gameplay.Players;
    using Eco.Gameplay.Systems.Messaging.Chat.Commands;
    using Eco.Simulation.WorldLayers;
    using Eco.Shared.Localization;
    using System.Linq;

    [ChatCommandHandler]
    public static class HideMinimapCommands
    {
        [ChatCommand("Shows commands for HideMinimap mod.")]
        public static void HideMinimap(User user) { }

        [ChatSubCommand("HideMinimap", "Hides the minimap everywhere except in revealed districts. Optional fog height (0-255, -1 = default) and RRGGBB fog color.", ChatAuthorizationLevel.Admin)]
        public static void Hide(User user, int fogHeight = -1, string fogColor = "")
        {
            var config = HideMinimapPlugin.Obj.Config;
            config.HideMinimap     = true;
            config.HiddenMapHeight = fogHeight;
            config.HiddenMapColor  = fogColor.Trim();
            HideMinimapPlugin.Obj.SaveConfig();
            MinimapReveal.Activate();
            user.Player.MsgLocStr($"Minimap hidden, {MinimapReveal.RevealedChunkCount} chunks revealed.");
        }

        [ChatSubCommand("HideMinimap", "Restores the vanilla minimap everywhere.", ChatAuthorizationLevel.Admin)]
        public static void Restore(User user)
        {
            var config    = HideMinimapPlugin.Obj.Config;
            var wasKilled = config.KillMinimap;
            config.HideMinimap = false;
            config.KillMinimap = false;
            HideMinimapPlugin.Obj.SaveConfig();

            if (wasKilled) MinimapAccess.NetObject.Active = true;
            if (!MinimapReveal.IsActive && !wasKilled) { user.Player.MsgLocStr("No need to restore minimap."); return; }

            MinimapReveal.Deactivate();
            user.Player.MsgLocStr("Minimap restored.");
        }

        [ChatSubCommand("HideMinimap", "Reveals the minimap inside a district: map name, then district name (empty = whole map).", ChatAuthorizationLevel.Admin)]
        public static void RevealDistrict(User user, string mapName, string districtName = "")
        {
            if (!TryValidate(user, mapName, districtName)) return;
            MinimapReveal.SetDistrictRevealed(mapName, districtName, true);
            user.Player.MsgLocStr($"Revealed {MinimapReveal.Key(mapName, districtName)} ({MinimapReveal.RevealedChunkCount} chunks visible).");
        }

        [ChatSubCommand("HideMinimap", "Hides the minimap again inside a district: map name, then district name (empty = whole map).", ChatAuthorizationLevel.Admin)]
        public static void ConcealDistrict(User user, string mapName, string districtName = "")
        {
            MinimapReveal.SetDistrictRevealed(mapName, districtName, false);
            user.Player.MsgLocStr($"Concealed {MinimapReveal.Key(mapName, districtName)} ({MinimapReveal.RevealedChunkCount} chunks visible).");
        }

        [ChatSubCommand("HideMinimap", "Lists revealed districts and active district maps.", ChatAuthorizationLevel.Admin)]
        public static void ListDistricts(User user)
        {
            var revealed = HideMinimapPlugin.Obj.Config.RevealedDistricts;
            var maps     = MinimapReveal.ActiveMaps.Select(map => $"{map.Name}: {string.Join(", ", map.Districts.Values.Select(d => d.Name))}");
            user.Player.MsgLocStr($"Hidden: {MinimapReveal.IsActive}\nRevealed: {(revealed.Any() ? string.Join(", ", revealed) : "none")}\nActive maps:\n{string.Join("\n", maps)}");
        }

        static bool TryValidate(User user, string mapName, string districtName)
        {
            var map = MinimapReveal.FindMap(mapName);
            if (map == null) { user.Player.MsgLocStr($"No active district map named '{mapName}'. Use /HideMinimap ListDistricts."); return false; }
            if (!string.IsNullOrWhiteSpace(districtName) && MinimapReveal.FindDistrict(map, districtName) == null) { user.Player.MsgLocStr($"No district named '{districtName}' in map '{map.Name}'."); return false; }
            return true;
        }

        [ChatSubCommand("HideMinimap", "Disables the minimap network object entirely.", ChatAuthorizationLevel.Admin)]
        public static void KillMap(User user)
        {
            KillMap();
            HideMinimapPlugin.Obj.Config.KillMinimap = true;
            HideMinimapPlugin.Obj.SaveConfig();
        }

        public static void KillMap() => MinimapAccess.NetObject.Active = false;

        [ChatSubCommand("HideMinimap", "Shows all world layers.", ChatAuthorizationLevel.Admin)]
        public static void ShowLayers(User user) => ShowLayers();

        [ChatSubCommand("HideMinimap", "Hides all world layers.", ChatAuthorizationLevel.Admin)]
        public static void HideLayers(User user) => HideLayers();

        public static void ShowLayers() => SetLayersVisible(true);
        public static void HideLayers() => SetLayersVisible(false);

        static void SetLayersVisible(bool visible)
        {
            foreach (var layer in WorldLayerManager.Obj.Layers)
                WorldLayerManager.Obj.UpdateLayerVisibility(layer, visible);
        }
    }
}
