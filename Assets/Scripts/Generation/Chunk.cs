using System.Collections;

using Engine.Components;

namespace VoxelSandbox;

public sealed class Chunk
{
    public Mesh Mesh;

    public BitArray SolidVoxelData;
    public VoxelType[] VoxelTypeData;
    public NoiseData[] NoiseData;

    public List<Vector3Byte> ExposedVoxelData = new();

    public Vector3Int WorldPosition { get; set; }
    public int LevelOfDetail { get; private set; }

    public int VoxelSize => _voxelSize ??= (int)Math.Pow(2, Math.Max(0, LevelOfDetail - 1));
    private int? _voxelSize = null;

    public int MaxVoxelCapacity => PaddedChunkSizeXZSquared * ChunkSizeY + PaddedChunkSizeXZSquared;

    public int PaddedChunkSizeXZSquared => _paddedChunkSizeXZsquared ??= PaddedChunkSizeXZ * PaddedChunkSizeXZ;
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

        SolidVoxelData = new BitArray(MaxVoxelCapacity);
        VoxelTypeData = new VoxelType[MaxVoxelCapacity];
        NoiseData = new NoiseData[PaddedChunkSizeXZSquared * 2];
    }

    public Vector3Int GetChunkSize() =>
        new Vector3Int(
            ChunkSizeXZ,
            ChunkSizeY,
            ChunkSizeXZ) * VoxelSize;

    public bool IsWithinBounds(Vector3Byte position) =>
        position.X >= 1 && position.X <= ChunkSizeXZ
     && position.Y >= 1 && position.Y <= ChunkSizeY
     && position.Z >= 1 && position.Z <= ChunkSizeXZ;

    public void SetExposedVoxel(Vector3Byte position) =>
        ExposedVoxelData.Add(position);

    public VoxelType GetVoxelType(Vector3Byte position) =>
        VoxelTypeData[ToIndex(position)];

    public void SetVoxelType(Vector3Byte position, VoxelType voxelType) =>
        VoxelTypeData[ToIndex(position)] = voxelType;

    public bool IsVoxelEmpty(Vector3Byte position) =>
        !IsVoxelSolid(position);

    public bool IsVoxelSolid(Vector3Byte position) =>
        SolidVoxelData[ToIndex(position)] == true;

    public void SetSolidVoxel(Vector3Byte position) =>
        SolidVoxelData[ToIndex(position)] = true;

    public NoiseData GetNoiseData(int x, int z) =>
        NoiseData[ToIndex(x, z)];

    public void SetNoiseData(int x, int z, NoiseData noiseData) =>
        NoiseData[ToIndex(x, z)] = noiseData;

    public int ToIndex(Vector3Byte position) =>
        position.X + (position.Z * PaddedChunkSizeXZ) + (position.Y * PaddedChunkSizeXZSquared);

    public int ToIndex(int x, int z) =>
        x * PaddedChunkSizeXZ + z;
}