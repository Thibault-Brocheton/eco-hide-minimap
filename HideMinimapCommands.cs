namespace CavRn.HideMinimap
{
    using Eco.Core.Plugins.Interfaces;
    using Eco.Gameplay.Players;
    using Eco.Gameplay.Systems.Messaging.Chat.Commands;
    using Eco.Simulation.WorldLayers;
    using Eco.Shared.Localization;
    using System.Linq;
    using System.Text;
    using System;

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
            HideMinimapPlugin.Obj.Config.HideMinimap = false;
            HideMinimapPlugin.Obj.SaveConfig();
            if (!MinimapReveal.IsActive) { user.Player.MsgLocStr("No need to restore minimap."); return; }

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

        [ChatSubCommand("HideMinimap", "Shows whether the minimap is hidden and every reveal setting.", ChatAuthorizationLevel.Admin)]
        public static void Status(User user)
        {
            var config = HideMinimapPlugin.Obj.Config;
            var sb     = new StringBuilder();
            sb.AppendLine($"Minimap: {(MinimapReveal.IsActive ? $"hidden, {MinimapReveal.RevealedChunkCount} chunks revealed" : "visible")}");
            sb.AppendLine($"Fog: height {(config.HiddenMapHeight < 0 ? "default" : config.HiddenMapHeight)}, color {(string.IsNullOrWhiteSpace(config.HiddenMapColor) ? "default" : config.HiddenMapColor)}");
            sb.AppendLine($"Town influence: {RevealLabel(config.TownInfluenceIsUnhidden)}");
            sb.AppendLine($"Nation influence: {RevealLabel(config.NationInfluenceIsUnhidden)}");
            sb.AppendLine($"Federation influence: {RevealLabel(config.FederationInfluenceIsUnhidden)}");
            sb.AppendLine($"Claims: {RevealLabel(config.ClaimInfluenceIsUnhidden)}");
            sb.Append($"Revealed districts: {(config.RevealedDistricts.Any() ? string.Join(", ", config.RevealedDistricts) : "none")}");
            user.Player.MsgLocStr(sb.ToString());
        }

        static string RevealLabel(bool revealed) => revealed ? "revealed" : "hidden";

        static bool TryValidate(User user, string mapName, string districtName)
        {
            var map = MinimapReveal.FindMap(mapName);
            if (map == null) { user.Player.MsgLocStr($"No active district map named '{mapName}'. Use /HideMinimap ListDistricts."); return false; }
            if (!string.IsNullOrWhiteSpace(districtName) && MinimapReveal.FindDistrict(map, districtName) == null) { user.Player.MsgLocStr($"No district named '{districtName}' in map '{map.Name}'."); return false; }
            return true;
        }

        [ChatSubCommand("HideMinimap", "Toggles revealing the map under town influence.", ChatAuthorizationLevel.Admin)]
        public static void ToggleTownInfluence(User user)       => ToggleReveal(user, "Town influence",       c => c.TownInfluenceIsUnhidden       = !c.TownInfluenceIsUnhidden);

        [ChatSubCommand("HideMinimap", "Toggles revealing the map under nation influence.", ChatAuthorizationLevel.Admin)]
        public static void ToggleNationInfluence(User user)     => ToggleReveal(user, "Nation influence",     c => c.NationInfluenceIsUnhidden     = !c.NationInfluenceIsUnhidden);

        [ChatSubCommand("HideMinimap", "Toggles revealing the map under federation influence.", ChatAuthorizationLevel.Admin)]
        public static void ToggleFederationInfluence(User user) => ToggleReveal(user, "Federation influence", c => c.FederationInfluenceIsUnhidden = !c.FederationInfluenceIsUnhidden);

        [ChatSubCommand("HideMinimap", "Toggles revealing the map on all claimed plots.", ChatAuthorizationLevel.Admin)]
        public static void ToggleClaims(User user)              => ToggleReveal(user, "Claims",               c => c.ClaimInfluenceIsUnhidden      = !c.ClaimInfluenceIsUnhidden);

        /// <summary>Flips a reveal setting (the toggle returns its new value), persists it and applies it immediately.</summary>
        static void ToggleReveal(User user, string label, Func<HideMinimapConfig, bool> toggle)
        {
            var revealed = toggle(HideMinimapPlugin.Obj.Config);
            HideMinimapPlugin.Obj.SaveConfig();
            MinimapReveal.Refresh();
            user.Player.MsgLocStr($"{label}: map {(revealed ? "revealed" : "hidden")} ({MinimapReveal.RevealedChunkCount} chunks visible).");
        }

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
