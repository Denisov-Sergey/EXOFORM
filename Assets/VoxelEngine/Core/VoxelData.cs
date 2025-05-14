using UnityEngine;

namespace VoxelEngine.Core
{
    public struct VoxelData
    {
        public VoxelType Type;
        public byte CorruptionLevel;
        
        public byte LightLevel;
        public byte Moisture;
        
        
        public static implicit operator byte(VoxelData data) => (byte)data.Type;
        public static implicit operator VoxelType(VoxelData data) => data.Type;
    }
}
