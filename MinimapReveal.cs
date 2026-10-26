namespace CavRn.HideMinimap
{
    using Eco.Core.Controller;
    using Eco.Core.Plugins.Interfaces;
    using Eco.Core.Systems;
    using Eco.Core.Utils;
    using Eco.Gameplay.Civics.Districts;
    using Eco.Gameplay.InfluenceObjects;
    using Eco.Gameplay.LegislationSystem;
    using Eco.Gameplay.Minimap;
    using Eco.Gameplay.Property;
    using Eco.Shared.Items;
    using Eco.Shared.Math;
    using Eco.Shared.Voxel;
    using Eco.Simulation.WorldLayers.History;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System;
    using VoxelWorld = Eco.Shared.Voxel.World;

    /// <summary>Keeps the minimap hidden everywhere except in the configured districts (plus settlement influence and claims when enabled), via the display-only reveal mask
    /// (MinimapManager.SetRevealMask): the real chunk data keeps flowing so far terrain is unaffected. Icons are only shown inside revealed chunks.</summary>
    public static class MinimapReveal
    {
        /// <summary>Fired after the set of revealed districts changes (config edit, command or map control object).</summary>
        public static ThreadSafeAction RevealedDistrictsChangedEvent = new();

        static readonly object   sync = new();
        static HashSet<Vector2i> revealedChunks = new();

        static HideMinimapConfig Config => HideMinimapPlugin.Obj.Config;

        //Controller subscriptions are weak and need an instance target: keep one alive for the influence maps.
        static readonly InfluenceWatcher influenceWatcher = new();
        sealed class InfluenceWatcher { public void OnInfluenceChanged() => Refresh(); }

        public static bool IsActive           { get; private set; }
        public static int  RevealedChunkCount => revealedChunks.Count;

        public static string Key(string mapName, string districtName) => $"{mapName.Trim()}/{districtName.Trim()}";
        public static string Key(DistrictMap map, District district)  => Key(map.Name, district.Name);

        public static IEnumerable<DistrictMap> ActiveMaps => Registrars.Get<DistrictMap>().Where(map => map.State == ProposableState.Active);

        /// <summary>Finds the active district map with this name (case-insensitive).</summary>
        public static DistrictMap FindMap(string mapName) => ActiveMaps.FirstOrDefault(map => string.Equals(map.Name?.Trim(), mapName?.Trim(), StringComparison.OrdinalIgnoreCase));

        /// <summary>Finds a district by name inside a map (case-insensitive).</summary>
        public static District FindDistrict(DistrictMap map, string districtName) => map?.Districts.Values.FirstOrDefault(d => string.Equals(d.Name?.Trim(), districtName?.Trim(), StringComparison.OrdinalIgnoreCase));

        public static bool IsDistrictRevealed(string mapName, string districtName)
        {
            var key = Key(mapName, districtName);
            return Config.RevealedDistricts.Any(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Adds or removes a district from the revealed set, persists it and applies the change immediately.</summary>
        public static void SetDistrictRevealed(string mapName, string districtName, bool revealed)
        {
            var key = Key(mapName, districtName);
            lock (sync)
            {
                Config.RevealedDistricts.RemoveAll(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                if (revealed) Config.RevealedDistricts.Add(key);
                HideMinimapPlugin.Obj.SaveConfig();
            }
            Refresh();
            RevealedDistrictsChangedEvent.Invoke();
        }

        public static Vector2i ChunkOf(Vector3 position)
        {
            var wrapped = VoxelWorld.GetWrappedWorldPosition(new Vector2i((int)position.X, (int)position.Z));
            return new Vector2i(wrapped.x / Chunk.Size, wrapped.y / Chunk.Size);
        }

        public static bool IsChunkRevealed(Vector2i chunkPos) => !IsActive || revealedChunks.Contains(chunkPos);

        /// <summary>Settlement influence maps whose plots are revealed, by tier (0 = town, 1 = nation, 2 = federation).</summary>
        static IEnumerable<InfluenceMap> RevealedInfluenceMaps()
        {
            var flags = new[] { Config.TownInfluenceIsUnhidden, Config.NationInfluenceIsUnhidden, Config.FederationInfluenceIsUnhidden };
            return SettlementType.All.Where(type => (int)type < flags.Length && flags[(int)type]).Select(type => InfluenceManager.Obj.GetMap(type)).Where(map => map?.Map != null);
        }

        /// <summary>Follows every settlement influence map so influence growth/shrink updates the revealed area.</summary>
        public static void WatchInfluenceMaps()
        {
            foreach (var map in SettlementType.All.Select(type => InfluenceManager.Obj.GetMap(type)).Where(map => map != null))
                map.Subscribe(nameof(InfluenceMap.Map), influenceWatcher.OnInfluenceChanged);
            Refresh();
        }

        public static void RefreshIfClaimsRevealed() { if (Config.ClaimInfluenceIsUnhidden) Refresh(); }

        /// <summary>A chunk is revealed when any of its 2x2 plots belongs to a revealed district of an active map, a revealed settlement influence or a claim (when enabled).</summary>
        static HashSet<Vector2i> ComputeRevealedChunks()
        {
            var result     = new HashSet<Vector2i>();
            var keys       = new HashSet<string>(Config.RevealedDistricts.Select(k => k.Trim()), StringComparer.OrdinalIgnoreCase);
            var influences = RevealedInfluenceMaps().ToList();
            var claims     = Config.ClaimInfluenceIsUnhidden;
            if (keys.Count == 0 && influences.Count == 0 && !claims) return result;

            var maps = keys.Count > 0 ? ActiveMaps.ToList() : new List<DistrictMap>();
            foreach (var chunkPos in VoxelWorld.ChunkSize.XZ.XYIter())
            {
                var origin = chunkPos * Chunk.Size;
                foreach (var plotOffset in Vector2i.XYIter(2))
                {
                    var plotPos = (origin + (plotOffset * PlotUtil.PlotSize)).ToPlotPos();
                    if (maps.Any(map => IsPlotRevealed(map, plotPos, keys))
                     || influences.Any(map => map.GetInfluencePlotPos(plotPos) > 0)
                     || (claims && PropertyManager.GetDeed(plotPos) != null)) { result.Add(chunkPos); break; }
                }
            }
            return result;
        }

        static bool IsPlotRevealed(DistrictMap map, PlotPos plotPos, HashSet<string> keys)
        {
            var district = map.GetDistrictAtPlotPos(plotPos);
            return district != null && (keys.Contains(Key(map, district)) || keys.Contains(Key(map.Name, string.Empty)));
        }

        /// <summary>Packs the revealed chunk set into the bitmask format of MinimapManager.SetRevealMask (bit index = x + y * chunkDims.x, LSB first).</summary>
        static byte[] BuildMask(HashSet<Vector2i> chunks)
        {
            var dims = VoxelWorld.ChunkSize.XZ;
            var mask = new byte[((dims.x * dims.y) + 7) / 8];
            foreach (var chunkPos in chunks)
            {
                var index = chunkPos.x + (chunkPos.y * dims.x);
                mask[index / 8] |= (byte)(1 << (index % 8));
            }
            return mask;
        }

        /// <summary>Hides the minimap except in the revealed districts. Safe to call again to re-apply.</summary>
        public static void Activate()
        {
            lock (sync)
            {
                IsActive       = true;
                revealedChunks = ComputeRevealedChunks();
                PushMask();
            }
        }

        /// <summary>Restores the vanilla minimap everywhere.</summary>
        public static void Deactivate()
        {
            lock (sync)
            {
                if (!IsActive) return;
                IsActive       = false;
                revealedChunks = new HashSet<Vector2i>();
                PushMask();
            }
        }

        /// <summary>Recomputes the revealed chunks and pushes the new mask, called when districts or the revealed set change.</summary>
        public static void Refresh()
        {
            if (!IsActive) return;
            lock (sync)
            {
                revealedChunks = ComputeRevealedChunks();
                PushMask();
            }
        }

        /// <summary>Pushes the current state to both display channels: the clients' minimap mask (with the configured fog style) and the web-map images.</summary>
        static void PushMask()
        {
            var color = int.TryParse(Config.HiddenMapColor?.Trim().TrimStart('#'), System.Globalization.NumberStyles.HexNumber, null, out var rgb) ? rgb : -1;
            MinimapManager.SetRevealMask(IsActive ? BuildMask(revealedChunks) : null, Config.HiddenMapHeight, color);
            WorldLayerHistory.MapRevealedTest = IsActive ? worldPos => IsChunkRevealed(new Vector2i(worldPos.x / Chunk.Size, worldPos.y / Chunk.Size)) : null;
            WorldLayerHistory.RefreshMapImages();
        }
    }
}
