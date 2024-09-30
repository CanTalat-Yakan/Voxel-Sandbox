using System.Numerics;

using LibNoise.Filter;
using LibNoise.Primitive;

using Engine.Essentials;
using Engine.Utilities;

namespace VoxelSandbox;

public record NoiseData(byte SurfaceHeight, byte MountainHeight, byte UndergroundDetail, byte BedrockHeight);

public sealed partial class NoiseSampler
{
    GameManager GameManager;

    public void GenerateChunkContent(Chunk chunk, GameManager gameManager)
    {
        GameManager = gameManager;

        int gridY = GetGridY(SampleNoise(chunk, Vector3Byte.OneXZ).SurfaceHeight, chunk.ChunkSizeY);
        chunk.WorldPosition = chunk.WorldPosition.Set(y: gridY);

        for (int x = 1; x <= chunk.ChunkSizeXZ; x++)
            for (int z = 1; z <= chunk.ChunkSizeXZ; z++)
                for (int y = 1; y < chunk.ChunkSizeY; y++)
                    AddExposedVoxel(new(x, y, z), chunk);

        gameManager.Generator.ChunksToBuild.Enqueue(chunk);
    }

    private int GetGridY(int y, int chunkSizeY) =>
        (int)(y / chunkSizeY) * chunkSizeY;

    void ProcessChunk(Chunk chunk, NoiseData noiseData)
    {
        if (!chunk.GeneratedChunkBottom && noiseData.SurfaceHeight == chunk.WorldPosition.Y - 1)
            chunk.GeneratedChunkBottom = true;
        else if (!chunk.GeneratedChunkTop && noiseData.SurfaceHeight == chunk.WorldPosition.Y + chunk.ChunkSizeY + 1)
            chunk.GeneratedChunkTop = true;
        else return;

        Vector3Int chunkPosition = new(chunk.WorldPosition.X, GetGridY(noiseData.SurfaceHeight, chunk.ChunkSizeY), chunk.WorldPosition.Z);
        Output.Log(chunkPosition);
        //Chunk newChunk = PoolManager.GetPool<Chunk>().Get().Initialize(
        //    chunkPosition,
        //    chunk.LevelOfDetail);
        //GameManager.Generator.ChunksToGenerate.Enqueue(newChunk);
        //var prim = GameManager.Entity.Manager.CreatePrimitive().Entity;
        //prim.Transform.SetPosition(chunkPosition.X + chunk.ChunkSizeXZ / 2, chunkPosition.Y + chunk.ChunkSizeY / 2, chunkPosition.Z + chunk.ChunkSizeXZ / 2);
        //prim.Transform.LocalPosition += Vector3.One / 2;
        //prim.Transform.SetScale(chunk.ChunkSizeXZ, chunk.ChunkSizeY, chunk.ChunkSizeXZ);

        //GameManager.GenerateChunk(PoolManager.GetPool<Chunk>().Get().Initialize(
        //    new(chunk.WorldPosition.X, GetGridY(noiseData.SurfaceHeight, chunk.ChunkSizeY), chunk.WorldPosition.Z),
        //    chunk.LevelOfDetail));
    }
}

public sealed partial class NoiseSampler
{
    private void AddExposedVoxel(Vector3Byte voxelPosition, Chunk chunk)
    {
        if (!SampleVoxel(out var voxelType, ref voxelPosition, chunk))
            return;

        chunk.SetVoxelType(voxelPosition, voxelType);
        chunk.SetSolidVoxel(voxelPosition);

        Vector3Byte adjacentVoxelPosition = new();

        int x = voxelPosition.X;
        int y = voxelPosition.Y;
        int z = voxelPosition.Z;

        foreach (var direction in Vector3Int.Directions)
        {
            adjacentVoxelPosition.Set(
                (byte)(x + direction.X),
                (byte)(y + direction.Y),
                (byte)(z + direction.Z));

            VoxelType adjacentVoxelType = VoxelType.None;

            // If the adjacent voxel was not found, the current iterated voxel is exposed
            if (chunk.IsVoxelEmpty(adjacentVoxelPosition) && !SampleVoxel(out adjacentVoxelType, ref adjacentVoxelPosition, chunk))
            {
                chunk.SetExposedVoxel(voxelPosition);

                return;
            }

            if (adjacentVoxelType is not VoxelType.None)
            {
                chunk.SetVoxelType(adjacentVoxelPosition, adjacentVoxelType);
                chunk.SetSolidVoxel(adjacentVoxelPosition);
            }
        }
    }

    private bool SampleVoxel(out VoxelType sample, ref Vector3Byte voxelPosition, Chunk chunk)
    {
        sample = chunk.GetVoxelType(voxelPosition);

        if (sample is not VoxelType.None)
            return true;

        var noiseData = SampleNoise(chunk, voxelPosition.X, voxelPosition.Z);
        int surfaceHeight = noiseData.SurfaceHeight;
        int mountainHeight = noiseData.MountainHeight;
        int undergroundDetail = noiseData.UndergroundDetail;
        int bedrockHeight = noiseData.BedrockHeight;

        ProcessChunk(chunk, noiseData);

        int x = voxelPosition.X;
        int y = voxelPosition.Y * chunk.VoxelSize + chunk.WorldPosition.Y;
        int z = voxelPosition.Z;

        if (chunk.LevelOfDetail > 0)
        {
            if (y <= surfaceHeight + chunk.VoxelSize && y >= surfaceHeight)
                sample = VoxelType.Grass;
            else if (y < surfaceHeight)
                sample = VoxelType.Stone;
        }
        else if (y < surfaceHeight)
        {
            if (y > surfaceHeight - undergroundDetail + 5)
                sample = VoxelType.Dirt;
            else
            {
                // Check cave noise to determine if this voxel should be empty (cave)
                if (y < undergroundDetail)
                    sample = y - 1 < bedrockHeight ? VoxelType.DiamondOre : VoxelType.Stone;
                else if (y + undergroundDetail < surfaceHeight)
                {
                    int nx = chunk.WorldPosition.X + x * chunk.VoxelSize;
                    int nz = chunk.WorldPosition.Z + z * chunk.VoxelSize;

                    double caveValue = GetCaveNoise(nx, y * 2, nz);

                    if (caveValue < 0.6)
                        sample = VoxelType.Stone;
                }
                else
                    sample = VoxelType.Stone;
            }
        }
        else if (y == surfaceHeight)
            sample = VoxelType.Grass;

        return sample is not VoxelType.None;
    }

    private NoiseData SampleNoise(Chunk chunk, Vector3Byte position) =>
        SampleNoise(chunk, position.X, position.Z);

    private NoiseData SampleNoise(Chunk chunk, int x, int z)
    {
        NoiseData noiseData = chunk.GetNoiseData(x, z);
        if (noiseData is not null)
            return noiseData;

        int nx = chunk.WorldPosition.X + x * chunk.VoxelSize;
        int nz = chunk.WorldPosition.Z + z * chunk.VoxelSize;

        byte surfaceHeight = GetSurfaceHeight(nx, nz);
        byte mountainHeight = GetMountainHeight(nx, nz);
        byte undergroundDetail = GetUndergroundDetail(nx, nz);
        byte bedrockHeight = GetBedrockNoise();

        surfaceHeight += (byte)((mountainHeight + 1) / 2);

        noiseData = new(surfaceHeight, mountainHeight, undergroundDetail, bedrockHeight);

        chunk.SetNoiseData(x, z, noiseData);

        return noiseData;
    }
}

public sealed partial class NoiseSampler
{
    public static int Seed = 1234;

    private static Random Random = new(Seed);
    private static SimplexPerlin s_perlinPrimitive = new(Seed, LibNoise.NoiseQuality.Fast);

    private Billow _surfaceNoise = new()
    {
        Primitive2D = s_perlinPrimitive,
        OctaveCount = 8,        // Fewer octaves for a smoother look
        Frequency = 0.01f,      // Lower frequency for broader features
        Scale = 7.5f,           // Adjust scale to manage the level of detail
    };
    private Billow _mountainNoise = new()
    {
        Primitive2D = s_perlinPrimitive,
        OctaveCount = 3,
        Frequency = 0.0002f,
        Scale = 200,
    };
    private Billow _undergroundNoise = new()
    {
        Primitive2D = s_perlinPrimitive,
        OctaveCount = 6,
        Frequency = 0.05f,
        Scale = 15f,
    };
    public Billow _caveNoise = new()
    {
        Primitive3D = s_perlinPrimitive,
        OctaveCount = 5,
        Frequency = 0.005f,
        Scale = 0.5f
    };

    private byte GetSurfaceHeight(int x, int z) =>
        (byte)(_surfaceNoise.GetValue(x, z) + 100);

    private byte GetMountainHeight(int x, int z) =>
        (byte)_mountainNoise.GetValue(x, z);

    private byte GetUndergroundDetail(int x, int z) =>
        (byte)(_undergroundNoise.GetValue(x, z) + 10);

    private byte GetBedrockNoise() =>
        (byte)Random.Next(0, 5);

    private double GetCaveNoise(int x, int y, int z) =>
        _caveNoise.GetValue(x, y, z);
}