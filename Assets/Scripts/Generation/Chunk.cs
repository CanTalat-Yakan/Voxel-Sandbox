using Engine.Components;

namespace VoxelSandbox;

public class Chunk
{
    public Mesh Mesh;

    public Dictionary<Vector3Byte, VoxelType> VoxelData = new();

    public Vector3Int WorldPosition { get; private set; } // Position of the chunk in world space (32, 0, 0)

    public int LevelOfDetail { get; private set; } // Size of the chunk (32, 64, 128, etc.)
    public int VoxelSize => Generator.LODSizesXZ[LevelOfDetail] / Generator.ChunkSizeXZ;

    public Chunk(Vector3Int worldPosition, int levelOfDetail)
    {
        WorldPosition = worldPosition;
        LevelOfDetail = levelOfDetail;
    }

    public bool IsWithinBounds(Vector3Byte localPosition) =>
        localPosition.X >= 1 && localPosition.X <= Generator.ChunkSizeXZ
     && localPosition.Y >= 1 && localPosition.Y <= Generator.ChunkSizeY
     && localPosition.Z >= 1 && localPosition.Z <= Generator.ChunkSizeXZ;

    public bool HasVoxel(Vector3Byte localPosition) =>
        VoxelData.ContainsKey(localPosition);
    
    public bool GetVoxel(Vector3Byte localPosition, out VoxelType voxelType) =>
        VoxelData.TryGetValue(localPosition, out voxelType);

    public void SetVoxel(Vector3Byte localPosition, VoxelType voxelType) =>
        VoxelData.Add(localPosition, voxelType);
}