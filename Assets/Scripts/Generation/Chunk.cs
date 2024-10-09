using System.Collections;

using Engine.Components;

namespace VoxelSandbox;

public sealed partial class Chunk
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

    public Vector3Int WorldPosition => UnscaledPosition * ScaledChunkSize;
    public Vector3Int UnscaledPosition;

    public int LevelOfDetail { get; private set; }

    public int PaddedChunkSizeSquared => _paddedChunkSizeSquared ??= PaddedChunkSize * PaddedChunkSize;
    private int? _paddedChunkSizeSquared = null;

    public int PaddedChunkSize => _paddedChunkSize ??= ChunkSize + 2;
    public int? _paddedChunkSize = null;

    public int ChunkSizeSquared => _chunkSizeSquared ??= ChunkSize * ChunkSize;
    private int? _chunkSizeSquared = null;

    public int ChunkSize => _chunkSize ??= Generator.ChunkSize;
    public int? _chunkSize = null;

    public int ScaledChunkSize => _scaledChunkSize ??= ChunkSize * VoxelSize;
    public int? _scaledChunkSize = null;

    public int VoxelSize => _voxelSize ??= (int)Math.Pow(2, LevelOfDetail);
    private int? _voxelSize = null;

    public Chunk()
    {
        SolidVoxelData = new BitArray(PaddedChunkSizeSquared * PaddedChunkSize + PaddedChunkSizeSquared);
        VoxelTypeData = new VoxelType[ChunkSizeSquared * ChunkSize + ChunkSizeSquared];
        NoiseData = new NoiseData[PaddedChunkSizeSquared];

        for (int i = 0; i < NoiseData.Length; i++)
            NoiseData[i] = new();
    }

    public Chunk Initialize(GameManager gameManager, int levelOfDetail, int x, int z, int? y = null)
    {
        Mesh ??= gameManager.Entity.Manager.CreateEntity().AddComponent<Mesh>();

        LevelOfDetail = levelOfDetail;
        UnscaledPosition = y is null ? new(x, 0, z) : new(x, y.Value, z);

        _voxelSize = null;
        _scaledChunkSize = null;

        return this;
    }

    public Chunk Reset()
    {
        SolidVoxelData.SetAll(false);

        for (int i = 0; i < NoiseData.Length; i++)
            NoiseData[i].initialized = false;

        return this;
    }
}

public sealed partial class Chunk
{
    public bool IsVoxelEmpty(ref Vector3Short position) =>
        !IsVoxelSolid(ref position);

    public bool IsVoxelSolid(ref Vector3Short position) =>
        SolidVoxelData[ToSolidVoxelDataIndex(ref position)] == true;

    public void SetEmptyVoxel(ref Vector3Short position) =>
        SolidVoxelData[ToSolidVoxelDataIndex(ref position)] = false;

    public void SetSolidVoxel(ref Vector3Short position) =>
        SolidVoxelData[ToSolidVoxelDataIndex(ref position)] = true;

    public VoxelType GetVoxelType(Vector3Short position) =>
        VoxelTypeData[ToVoxelTypeDataIndex(ref position)];

    public VoxelType GetVoxelType(ref Vector3Short position) =>
        VoxelTypeData[ToVoxelTypeDataIndex(ref position)];

    public void SetVoxelType(ref Vector3Short position, ref VoxelType voxelType) =>
        VoxelTypeData[ToVoxelTypeDataIndex(ref position)] = voxelType;

    public NoiseData GetNoiseData(int x, int z) =>
        NoiseData[ToNoiseDataIndex(x, z)];

    public void SetNoiseData(int x, int z, NoiseData noiseData) =>
        NoiseData[ToNoiseDataIndex(x, z)] = noiseData;

    public void SetExposedVoxel(ref Vector3Short position) =>
        ExposedVoxelPosition.Add(position);
}

public sealed partial class Chunk
{
    public int GetGridY(int y) =>
        FloorDivision(y, ScaledChunkSize);

    private static int FloorDivision(int a, int b) =>
        a >= 0 ? a / b : (a - (b - 1)) / b;

    public Vector3Int GetChunkSize() =>
        Vector3Int.One * ChunkSize * VoxelSize;

    public bool IsWithinBounds(ref Vector3Short position) =>
        position.X >= 1 && position.X <= ChunkSize
     && position.Y >= 1 && position.Y <= ChunkSize
     && position.Z >= 1 && position.Z <= ChunkSize;
}

public sealed partial class Chunk
{
    public int ToSolidVoxelDataIndex(ref Vector3Short position) =>
        position.X + position.Z * PaddedChunkSize + position.Y * PaddedChunkSizeSquared;

    public int ToVoxelTypeDataIndex(ref Vector3Short position) =>
        position.X - 1 + (position.Z - 1) * ChunkSize + (position.Y - 1) * ChunkSizeSquared;

    public int ToNoiseDataIndex(int x, int z) =>
        x + z * PaddedChunkSize;
}