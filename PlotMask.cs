namespace CavRn.HideMinimap
{
    using Eco.Shared.Math;
    using Eco.Shared.Voxel;
    using System.Collections.Generic;
    using System.Linq;
    using VoxelWorld = Eco.Shared.Voxel.World;

    /// <summary>Packs plot sets into the bitmask format of MinimapManager.SetRevealMask (bit index = x + y * plotDims.x, LSB first).</summary>
    public static class PlotMask
    {
        public static Vector2i PlotDims => VoxelWorld.VoxelSize.XZ / PlotUtil.PlotSize;

        public static byte[] Pack(HashSet<Vector2i> plots)
        {
            var dims = PlotDims;
            var mask = new byte[((dims.x * dims.y) + 7) / 8];
            foreach (var plotXZ in plots)
            {
                var index = plotXZ.x + (plotXZ.y * dims.x);
                mask[index / 8] |= (byte)(1 << (index % 8));
            }
            return mask;
        }

        /// <summary>Reverse of <see cref="Pack"/>; an empty set for a null mask or one saved with another world size.</summary>
        public static HashSet<Vector2i> Unpack(byte[] mask)
        {
            var dims = PlotDims;
            if (mask == null || mask.Length != ((dims.x * dims.y) + 7) / 8) return new HashSet<Vector2i>();
            return dims.XYIter().Where(p => (mask[(p.x + (p.y * dims.x)) / 8] & (1 << ((p.x + (p.y * dims.x)) % 8))) != 0).ToHashSet();
        }
    }
}
