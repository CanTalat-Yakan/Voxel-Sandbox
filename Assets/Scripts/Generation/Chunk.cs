using System.Collections;

using Engine.Components;

namespace VoxelSandbox;

public sealed class Chunk
{
    public Mesh Mesh;

    public BitArray SolidVoxelData;
    public VoxelType[] VoxelTypeData;
    public NoiseData[] NoiseData;

    public bool GeneratedChunkBottom = false;
    public bool GeneratedChunkTop = false;
    public bool GeneratedChunkFromChunk = false;

    public List<Vector3Short> ExposedVoxelData = new();

    public Vector3Int WorldPosition { get; set; }
    public int LevelOfDetail { get; private set; }

    public int VoxelSize => _voxelSize ??= (int)Math.Pow(2, LevelOfDetail);
    private int? _voxelSize = null;

    public int MaxVoxelCapacity => PaddedChunkSizeSquared * PaddedChunkSize + PaddedChunkSizeSquared;

    public int PaddedChunkSizeSquared => _paddedChunkSizeSquared ??= PaddedChunkSize * PaddedChunkSize;
    private int? _paddedChunkSizeSquared = null;

    public int PaddedChunkSize => _paddedChunkSize ??= ChunkSize + 2;
    public int? _paddedChunkSize = null;

    public int ChunkSize => _chunkSize ??= Generator.ChunkSize / VoxelSize;
    public int? _chunkSize = null;

    public Chunk()
    {
        SolidVoxelData = new BitArray(MaxVoxelCapacity);
        VoxelTypeData = new VoxelType[MaxVoxelCapacity];
        NoiseData = new NoiseData[PaddedChunkSizeSquared];
    }

    public Chunk Initialize(Vector3Int worldPosition, int levelOfDetail)
    {
        WorldPosition = worldPosition;
        LevelOfDetail = levelOfDetail;

        return this;
    }

    public Chunk Reset()
    {
        SolidVoxelData.SetAll(false);

        for (int i = 0; i < NoiseData.Length; i++)
            NoiseData[i] = null;

        return this;
    }

    public Vector3Int GetChunkSize() =>
        Vector3Int.One * ChunkSize * VoxelSize;

    public bool IsWithinBounds(Vector3Short position) =>
        position.X >= 1 && position.X <= ChunkSize
     && position.Y >= 1 && position.Y <= ChunkSize
     && position.Z >= 1 && position.Z <= ChunkSize;

    public void SetExposedVoxel(Vector3Short position) =>
        ExposedVoxelData.Add(position);

    public VoxelType GetVoxelType(Vector3Short position) =>
        VoxelTypeData[ToIndex(position)];

    public void SetVoxelType(Vector3Short position, VoxelType voxelType) =>
        VoxelTypeData[ToIndex(position)] = voxelType;

    public bool IsVoxelEmpty(Vector3Short position) =>
        !IsVoxelSolid(position);

    public bool IsVoxelSolid(Vector3Short position) =>
        SolidVoxelData[ToIndex(position)] == true;

    public void SetSolidVoxel(Vector3Short position) =>
        SolidVoxelData[ToIndex(position)] = true;

    public NoiseData GetNoiseData(int x, int z) =>
        NoiseData[ToIndex(x, z)];

    public void SetNoiseData(int x, int z, NoiseData noiseData) =>
        NoiseData[ToIndex(x, z)] = noiseData;

    public int ToIndex(Vector3Short position) =>
        position.X + position.Z * PaddedChunkSize + position.Y * PaddedChunkSizeSquared;

    public int ToIndex(int x, int z) =>
        x + z * PaddedChunkSize;
}