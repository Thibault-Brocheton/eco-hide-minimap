namespace CavRn.HideMinimap
{
    using Eco.Gameplay.Minimap;
    using Eco.Shared.Networking;
    using System.Reflection;

    /// <summary>Reflection bridge to the minimap's private net object, only used by the KillMap command.</summary>
    public static class MinimapAccess
    {
        static readonly FieldInfo netObjectField = typeof(MinimapManager).GetField("netObject", BindingFlags.Instance | BindingFlags.NonPublic);

        public static NetObject NetObject => (NetObject)netObjectField.GetValue(MinimapManager.Obj);
    }
}
