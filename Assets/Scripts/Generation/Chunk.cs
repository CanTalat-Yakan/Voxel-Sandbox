using Engine.Components;

namespace VoxelSandbox;

public sealed class Chunk
{
    public Mesh Mesh;

    public bool[] SolidVoxelData;
    public VoxelType[] VoxelTypeData;
    public List<Vector3Byte> ExposedVoxelData = new();
    public Dictionary<int, NoiseData> NoiseData = new();

    public Vector3Int WorldPosition { get; private set; }
    public int LevelOfDetail { get; private set; }

    public int VoxelSize => _voxelSize ??= (int)Math.Pow(2, Math.Max(0, LevelOfDetail - 1));
    private int? _voxelSize = null;

    public int MaxVoxelCapacity => PaddedChunkSizeXZSquared * ChunkSizeY + PaddedChunkSizeXZSquared;

    public int PaddedChunkSizeXZSquared => _paddedChunkSizeXZsquared ??= (int)Math.Pow(PaddedChunkSizeXZ, 2);
    private int? _paddedChunkSizeXZsquared = null;

    public int PaddedChunkSizeXZ => _paddedChunkSizeXZ ??= ChunkSizeXZ + 1;
    public int? _paddedChunkSizeXZ = null;

    public int ChunkSizeXZ => _chunkSizeXZ ??= Generator.ChunkSizeXZ * ChunkSizeXZMultiplier;
    private int? _chunkSizeXZ = null;

    public int ChunkSizeY => _chunkSizeY ??= Generator.ChunkSizeY / VoxelSize;
    public int? _chunkSizeY = null;

    public int ChunkSizeXZMultiplier => _chunkSizeXZMultiplier ??= LevelOfDetail == 0 ? 1 : 2;
    private int? _chunkSizeXZMultiplier = null;

    public Chunk(Vector3Int worldPosition, int levelOfDetail)
    {
        WorldPosition = worldPosition;
        LevelOfDetail = levelOfDetail;

        SolidVoxelData = new bool[MaxVoxelCapacity];
        VoxelTypeData = new VoxelType[MaxVoxelCapacity];
    }

    public Vector3Int GetChunkSize() =>
        new Vector3Int(
            ChunkSizeXZ,
            ChunkSizeY,
            ChunkSizeXZ) * VoxelSize;

    public bool IsWithinBounds(Vector3Byte localPosition) =>
        localPosition.X >= 1 && localPosition.X <= ChunkSizeXZ
     && localPosition.Y >= 1 && localPosition.Y <= ChunkSizeY
     && localPosition.Z >= 1 && localPosition.Z <= ChunkSizeXZ;

    public void SetExposedVoxel(Vector3Byte localPosition) =>
        ExposedVoxelData.Add(localPosition);

    public VoxelType GetVoxelType(Vector3Byte localPosition) =>
        VoxelTypeData[ToIndex(localPosition)];

    public void SetVoxelType(Vector3Byte localPosition, VoxelType voxelType) =>
        VoxelTypeData[ToIndex(localPosition)] = voxelType;

    public bool IsVoxelEmpty(Vector3Byte localPosition) =>
        !IsVoxelSolid(localPosition);

    public bool IsVoxelSolid(Vector3Byte localPosition) =>
        SolidVoxelData[ToIndex(localPosition)] == true;

    public void SetSolidVoxel(Vector3Byte localPosition) =>
        SolidVoxelData[ToIndex(localPosition)] = true;

    public bool TryGetNoiseData(int x, int z, out NoiseData noiseData) =>
        NoiseData.TryGetValue(ToIndex(x, z), out noiseData);

    public bool SetNoiseData(int x, int z, NoiseData noiseData) =>
        NoiseData.TryAdd(x * ChunkSizeXZ + z, noiseData);

    public int ToIndex(Vector3Byte localPosition) =>
        localPosition.X + (localPosition.Z * PaddedChunkSizeXZ) + (localPosition.Y * PaddedChunkSizeXZSquared);

    public int ToIndex(int x, int z) =>
        x * ChunkSizeXZ + z;
}