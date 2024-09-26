using Engine.Components;

namespace VoxelSandbox;

public sealed class Chunk
{
    public Mesh Mesh;

    public Dictionary<Vector3Byte, VoxelType> ExposedVoxelData = new();
    public List<Vector3Byte> AirVoxelData = new();

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

    public bool HasExposedVoxel(Vector3Byte localPosition) =>
        ExposedVoxelData.ContainsKey(localPosition);

    public bool GetExposedVoxel(Vector3Byte localPosition, out VoxelType voxelType) =>
        ExposedVoxelData.TryGetValue(localPosition, out voxelType);

    public bool SetExposedVoxel(Vector3Byte localPosition, VoxelType voxelType) =>
        ExposedVoxelData.TryAdd(localPosition, voxelType);

    public bool HasAirVoxel(Vector3Byte localPosition) =>
        AirVoxelData.Contains(localPosition);

    public void SetAirVoxel(Vector3Byte localPosition) =>
        AirVoxelData.Add(localPosition);
    
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