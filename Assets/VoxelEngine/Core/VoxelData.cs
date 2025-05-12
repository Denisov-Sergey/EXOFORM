using UnityEngine;

namespace VoxelEngine.Core
{
    public struct VoxelData
    {
        public byte Type;
        public byte Biome;
        public float Light;
        
        public static VoxelData Air => new VoxelData { Type = 0 };
    }
}
