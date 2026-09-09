namespace CavRn.HideMinimap
{
    using Eco.Core.Controller;
    using Eco.Core.Utils;
    using Eco.Gameplay.Auth;
    using Eco.Gameplay.Objects;
    using Eco.Gameplay.Players;
    using Eco.Shared.IoC;
    using Eco.Shared.Items;
    using Eco.Shared.Localization;
    using Eco.Shared.Networking;
    using Eco.Shared.Serialization;
    using System.ComponentModel;

    /// <summary>Lets authorized users reveal or hide the minimap inside one district, picked by map and district name so the object survives district edits.</summary>
    [Serialized, CreateComponentTabLoc("Map Control", true), HasIcon("MinimapComponent"), Priority(1000)]
    public class MapControlComponent : WorldObjectComponent
    {
        public override WorldObjectComponentClientAvailability Availability => WorldObjectComponentClientAvailability.Always;
        [SyncToView] public override string IconName => "MinimapComponent";

        [SyncToView, Autogen, Sort(0), UITypeName("GeneralHeader")]
        public string Title => "Map Control";

        [Serialized, Eco(AccessType.FullAccess), Sort(1), Description("Name of the district map (as shown in Civics).")]
        public string DistrictMapName { get; set; } = "";

        [Serialized, Eco(AccessType.FullAccess), Sort(2), Description("Name of the district inside that map. Leave empty to control the whole map.")]
        public string DistrictName { get; set; } = "";

        [SyncToView, Autogen, Sort(3), UITypeName("GeneralHeader")]
        public string Status => this.ComputeStatus();

        [SyncToView] public string ButtonTitle => this.IsRevealed ? Localizer.DoStr("Hide map here") : Localizer.DoStr("Reveal map here");
        [SyncToView] public bool   CanToggle   => MinimapReveal.IsActive && this.ResolveTarget(out _);

        bool IsRevealed => MinimapReveal.IsDistrictRevealed(this.DistrictMapName, this.DistrictName);

        [Autogen, RPC, Sort(4), UITypeName("BigButton"), DynamicTitle(nameof(ButtonTitle)), EnabledParam(nameof(CanToggle))]
        public void ToggleMap(Player player)
        {
            if (!ServiceHolder<IAuthManager>.Obj.IsAuthorized(this.Parent, player.User, AccessType.ConsumerAccess, null, out _))
            {
                player.ErrorLocStr("You are not authorized to use this object.");
                return;
            }
            if (!MinimapReveal.IsActive) { player.ErrorLocStr("The minimap is not hidden on this server."); return; }
            if (!this.ResolveTarget(out var error)) { player.ErrorLocStr(error); return; }

            var reveal = !this.IsRevealed;
            MinimapReveal.SetDistrictRevealed(this.DistrictMapName, this.DistrictName, reveal);
            player.MsgLocStr(reveal ? $"Map revealed in {this.TargetName}." : $"Map hidden in {this.TargetName}.");
        }

        public override void Initialize()
        {
            base.Initialize();
            MinimapReveal.RevealedDistrictsChangedEvent.Add(this.RefreshView);
            this.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(this.DistrictMapName) || e.PropertyName == nameof(this.DistrictName)) this.RefreshView(); };
        }

        public override void Destroy()
        {
            MinimapReveal.RevealedDistrictsChangedEvent.Remove(this.RefreshView);
            base.Destroy();
        }

        void RefreshView()
        {
            this.Changed(nameof(this.Status));
            this.Changed(nameof(this.ButtonTitle));
            this.Changed(nameof(this.CanToggle));
        }

        string TargetName => string.IsNullOrWhiteSpace(this.DistrictName) ? this.DistrictMapName.Trim() : $"{this.DistrictMapName.Trim()} / {this.DistrictName.Trim()}";

        bool ResolveTarget(out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(this.DistrictMapName)) { error = "No district map configured."; return false; }
            var map = MinimapReveal.FindMap(this.DistrictMapName);
            if (map == null) { error = $"No active district map named '{this.DistrictMapName.Trim()}'."; return false; }
            if (!string.IsNullOrWhiteSpace(this.DistrictName) && MinimapReveal.FindDistrict(map, this.DistrictName) == null) { error = $"No district named '{this.DistrictName.Trim()}' in map '{map.Name}'."; return false; }
            return true;
        }

        string ComputeStatus()
        {
            if (!MinimapReveal.IsActive) return "Minimap is not hidden on this server";
            if (!this.ResolveTarget(out var error)) return error;
            return this.IsRevealed ? $"{this.TargetName}: map revealed" : $"{this.TargetName}: map hidden";
        }
    }
}
