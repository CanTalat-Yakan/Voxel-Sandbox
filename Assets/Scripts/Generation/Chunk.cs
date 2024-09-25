using Engine.Components;

namespace VoxelSandbox;

public class Chunk
{
    public Mesh Mesh;

    public Dictionary<Vector3Byte, VoxelType> VoxelData = new();
    public Dictionary<int, NoiseData> NoiseData = new();

    public Vector3Int WorldPosition { get; private set; }
    public int LevelOfDetail { get; private set; }

    public int VoxelSize => (int)Math.Pow(2, Math.Max(0, LevelOfDetail - 1));

    public Chunk(Vector3Int worldPosition, int levelOfDetail)
    {
        WorldPosition = worldPosition;
        LevelOfDetail = levelOfDetail;
    }

    public static bool IsWithinBounds(Vector3Byte localPosition) =>
        localPosition.X >= 1 && localPosition.X <= Generator.ChunkSizeXZ
     && localPosition.Y >= 1 && localPosition.Y <= Generator.ChunkSizeY
     && localPosition.Z >= 1 && localPosition.Z <= Generator.ChunkSizeXZ;

    public bool HasVoxel(Vector3Byte localPosition) =>
        VoxelData.ContainsKey(localPosition);

    public bool GetVoxel(Vector3Byte localPosition, out VoxelType voxelType) =>
        VoxelData.TryGetValue(localPosition, out voxelType);

    public bool SetVoxel(Vector3Byte localPosition, VoxelType voxelType) =>
        VoxelData.TryAdd(localPosition, voxelType);
    
    public bool TryGetNoiseData(int x, int z, out NoiseData noiseData) =>
        NoiseData.TryGetValue(x * Generator.ChunkSizeXZ + z, out noiseData);
    
    public NoiseData GetNoiseData(int x, int z) =>
        NoiseData[x * Generator.ChunkSizeXZ + z];
    
    public bool SetNoiseData(int x, int z, NoiseData noiseData) =>
        NoiseData.TryAdd(x * Generator.ChunkSizeXZ + z, noiseData);

    public Vector3Int GetChunkSize() =>
        new Vector3Int(
            Generator.ChunkSizeXZ * VoxelSize,
            Generator.ChunkSizeY,
            Generator.ChunkSizeXZ * VoxelSize);
}