namespace CavRn.HideMinimap
{
    using Eco.Core.Plugins.Interfaces;
    using Eco.Gameplay.Minimap;
    using Eco.Gameplay.Players;
    using Eco.Gameplay.Systems.Messaging.Chat.Commands;
    using Eco.Shared.Math;
    using Eco.Shared.Networking;
    using Eco.Shared.Time;
    using Eco.Shared.Voxel;
    using Eco.World;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Reflection;
    using System;

    [ChatCommandHandler]
    public static class HideMinimapCommands
    {
        [ChatCommand("Shows commands for HideMinimap mod.")]
        public static void HideMinimap(User user) { }

        [ChatSubCommand("HideMinimap", "Restore", ChatAuthorizationLevel.Admin)]
        public static void Restore(User user)
        {
            if (HideMinimapPlugin.OnTopOrWaterBlockCacheChangedSavedCallbacks.Count == 0)
            {
                user.Player.MsgLocStr("No need to restore minimap.");
                return;
            }

            HideMinimapPlugin.Obj.Config.HideMinimap = false;
            HideMinimapPlugin.Obj.SaveConfig();

            foreach (var savedUpdate in HideMinimapPlugin.OnTopOrWaterBlockCacheChangedSavedCallbacks)
            {
                Eco.World.World.OnTopOrWaterBlockCacheChanged.AddUnique(savedUpdate.Delegate as Action<Vector2i>);
            }

            MarkAllChunksDirty();

            HideMinimapPlugin.OnTopOrWaterBlockCacheChangedSavedCallbacks.Clear();
        }

        [ChatSubCommand("HideMinimap", "Hide", ChatAuthorizationLevel.Admin)]
        public static void Hide(User user, int height = 160, string block = "ShaleBlock")
        {
            var result = Hide(height, block.Trim());

            if (!result.StartsWith("ERROR:"))
            {
                HideMinimapPlugin.Obj.Config.HideMinimap = true;
                HideMinimapPlugin.Obj.SaveConfig();
            }

            user.Player.MsgLocStr(result);
        }

        public static string Hide(int height = 160, string block = "ShaleBlock")
        {
            if (HideMinimapPlugin.OnTopOrWaterBlockCacheChangedSavedCallbacks.Count == 0)
            {
                HideMinimapPlugin.OnTopOrWaterBlockCacheChangedSavedCallbacks = Eco.World.World.OnTopOrWaterBlockCacheChanged.Callbacks.ToList();
                Eco.World.World.OnTopOrWaterBlockCacheChanged.Clear();
            }

            var field = typeof(MinimapManager).GetField(
                "data",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            var landData = field?.GetValue(MinimapManager.Obj);
            if (landData == null)
            {
                return "ERROR: Technical issue 1, contact mod creator.";
            }

            var dataType = landData.GetType();
            var landHeightsProp  = dataType.GetProperty("LandHeights");
            var landTypesProp    = dataType.GetProperty("LandTypes");
            var waterHeightsProp = dataType.GetProperty("WaterHeights");

            var landHeights  = landHeightsProp?.GetValue(landData);
            var landTypes    = landTypesProp?.GetValue(landData);
            var waterHeights = waterHeightsProp?.GetValue(landData);

            if (landHeights == null || landTypes == null || waterHeights == null)
            {
                return "ERROR: Technical issue 2, contact mod creator.";
            }

            var chunkMapType = landHeights.GetType();
            var indexer = chunkMapType.GetProperty("Item", new[] { typeof(Vector2i) });
            if (indexer == null)
            {
                return "ERROR: Technical issue 3, contact mod creator.";
            }

            if (!block.EndsWith("Block")) block += "Block";
            var blockType = BlockManager.FromTypeName(block);

            if (blockType == null)
            {
                return $"ERROR: Can't find block {block}.";
            }

            var blockId = BlockManager.GetBlockID(blockType);

            var size = Eco.Shared.Voxel.World.ChunkSize.XZ;

            foreach (var chunkPos in size.XYIter())
            {
                var hChunk = indexer?.GetValue(landHeights, new object[] { chunkPos });
                var tChunk = indexer?.GetValue(landTypes, new object[] { chunkPos });

                ForceUniformChunk(hChunk, (ushort)height);
                ForceUniformChunk(tChunk, blockId);
            }

            EnqueueAllChunksToAllViewers();

            return $"Minimap has been set to height {height} with block {block}.";
        }

        private static void ForceUniformChunk(object? chunkData, ushort value)
        {
            if (chunkData == null) return;

            var chunkType = chunkData.GetType();
            int size = Chunk.Size;

            // indexeur this[int,int]
            var indexer = chunkType.GetProperty("Item", new[] { typeof(int), typeof(int) });
            if (indexer == null) return;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    indexer.SetValue(chunkData, value, new object[] { x, y });
                }
            }

            // On laisse la classe faire son boulot
            var compressMethod = chunkType.GetMethod("Compress", BindingFlags.Instance | BindingFlags.Public);
            compressMethod?.Invoke(chunkData, null);

            var lastUpdatedProp = chunkType.GetProperty(
                "LastUpdated",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            lastUpdatedProp?.SetValue(chunkData, TimeUtil.Seconds);
        }

        private static void EnqueueAllChunksToAllViewers()
        {
            var chunkSyncField = typeof(MinimapManager).GetField(
                "chunkSync",
                BindingFlags.Static | BindingFlags.NonPublic
            );

            var dictObj = chunkSyncField?.GetValue(null);
            if (dictObj == null) return;

            var chunkSync = (ConcurrentDictionary<INetObjectViewer, ConcurrentQueue<Vector2i>>)dictObj;

            var size = Eco.Shared.Voxel.World.ChunkSize.XZ;

            foreach (var kvp in chunkSync)
            {
                var queue = kvp.Value;
                foreach (var chunkPos in size.XYIter())
                    queue.Enqueue(chunkPos);
            }
        }

        private static void MarkAllChunksDirty()
        {
            var changedChunksField = typeof(MinimapManager).GetField(
                "changedChunks",
                BindingFlags.Static | BindingFlags.NonPublic
            );

            var queueObj = changedChunksField?.GetValue(null);
            if (queueObj is not ConcurrentQueue<Vector2i> queue)
                return;

            var size = Eco.Shared.Voxel.World.ChunkSize.XZ;

            foreach (var chunkPos in size.XYIter())
                queue.Enqueue(chunkPos);
        }
    }
}
