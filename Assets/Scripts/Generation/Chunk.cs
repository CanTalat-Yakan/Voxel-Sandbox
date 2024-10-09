using System.Collections;

using Engine.Components;

namespace VoxelSandbox;

public sealed class Chunk
{
    public Mesh Mesh;

    public BitArray SolidVoxelData;
    public VoxelType[] VoxelTypeData;
    public NoiseData[] NoiseData;

    public List<Vector3Short> ExposedVoxelPosition = new();

    public bool IsChunkDirty = true;
    public bool IsChunkFromChunk = false;

    public Chunk TopChunk;
    public Chunk BottomChunk;

    public Vector3Int WorldPosition { get; set; }
    public int LevelOfDetail { get; private set; }

    public int VoxelSize => _voxelSize ??= (int)Math.Pow(2, LevelOfDetail);
    private int? _voxelSize = null;

    public int MaxVoxelCapacity => _maxVoxelCapacity ??= PaddedChunkSizeSquared * PaddedChunkSize + PaddedChunkSizeSquared;
    private int? _maxVoxelCapacity = null;

    public int PaddedChunkSizeSquared => _paddedChunkSizeSquared ??= PaddedChunkSize * PaddedChunkSize;
    private int? _paddedChunkSizeSquared = null;

    public int PaddedChunkSize => _paddedChunkSize ??= ChunkSize + 2;
    public int? _paddedChunkSize = null;

    public int ChunkSize => _chunkSize ??= Generator.ChunkSize;
    public int? _chunkSize = null;

    public Chunk()
    {
        SolidVoxelData = new BitArray(MaxVoxelCapacity);
        VoxelTypeData = new VoxelType[MaxVoxelCapacity];
        NoiseData = new NoiseData[PaddedChunkSizeSquared];

        for (int i = 0; i < NoiseData.Length; i++)
            NoiseData[i] = new();
    }

    public Chunk Initialize(GameManager gameManager, int levelOfDetail, int x, int z, int? y = null)
    {
        Mesh ??= gameManager.Entity.Manager.CreateEntity().AddComponent<Mesh>();

        LevelOfDetail = levelOfDetail;
        WorldPosition = y is not null ? new(x, y.Value, z) : new(x, 0, z);
        WorldPosition *= ChunkSize;

        _voxelSize = null;

        return this;
    }

    public Chunk Reset()
    {
        SolidVoxelData.SetAll(false);

        for (int i = 0; i < NoiseData.Length; i++)
            NoiseData[i].initialized = false;

        return this;
    }

    public int GetGridY(int y) =>
        FloorDivision(y, ChunkSize * VoxelSize) * ChunkSize * VoxelSize;

    private static int FloorDivision(int a, int b) =>
        a >= 0 ? a / b : (a - (b - 1)) / b;

    public Vector3Int GetChunkSize() =>
        Vector3Int.One * ChunkSize * VoxelSize;

    public bool IsWithinBounds(ref Vector3Short position) =>
        position.X >= 1 && position.X <= ChunkSize
     && position.Y >= 1 && position.Y <= ChunkSize
     && position.Z >= 1 && position.Z <= ChunkSize;

    public void SetExposedVoxel(ref Vector3Short position) =>
        ExposedVoxelPosition.Add(position);

    public VoxelType GetVoxelType(Vector3Short position) =>
        VoxelTypeData[ToIndex(ref position)];

    public VoxelType GetVoxelType(ref Vector3Short position) =>
        VoxelTypeData[ToIndex(ref position)];

    public void SetVoxelType(ref Vector3Short position, ref VoxelType voxelType) =>
        VoxelTypeData[ToIndex(ref position)] = voxelType;

    public bool IsVoxelEmpty(ref Vector3Short position) =>
        !IsVoxelSolid(ref position);

    public bool IsVoxelSolid(ref Vector3Short position) =>
        SolidVoxelData[ToIndex(ref position)] == true;

    public void SetEmptyVoxel(ref Vector3Short position) =>
        SolidVoxelData[ToIndex(ref position)] = false;

    public void SetSolidVoxel(ref Vector3Short position) =>
        SolidVoxelData[ToIndex(ref position)] = true;

    public NoiseData GetNoiseData(int x, int z) =>
        NoiseData[ToIndex(x, z)];

    public void SetNoiseData(int x, int z, NoiseData noiseData) =>
        NoiseData[ToIndex(x, z)] = noiseData;

    public int ToIndex() =>
        WorldPosition.X + WorldPosition.Z * PaddedChunkSize + WorldPosition.Y * PaddedChunkSizeSquared;

    public int ToIndex(ref Vector3Short position) =>
        position.X + position.Z * PaddedChunkSize + position.Y * PaddedChunkSizeSquared;

    public int ToIndex(int x, int z) =>
        x + z * PaddedChunkSize;
}