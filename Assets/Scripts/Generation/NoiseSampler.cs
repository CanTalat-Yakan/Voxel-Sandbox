using System.Diagnostics;

using LibNoise.Filter;
using LibNoise.Primitive;

using Engine.Essentials;
using Engine.Utilities;

namespace VoxelSandbox;

public record NoiseData(ushort SurfaceHeight, ushort MountainHeight, byte UndergroundDetail, byte BedrockHeight);

public sealed partial class NoiseSampler
{
    GameManager GameManager;

    private Stopwatch _stopwatch = new();

    public void GenerateChunkContent(Chunk chunk, GameManager gameManager)
    {
        _stopwatch.Start();

        GameManager = gameManager;

        SetGridY(chunk);

        for (int x = 1; x <= chunk.ChunkSize; x++)
            for (int z = 1; z <= chunk.ChunkSize; z++)
                for (int y = 1; y <= chunk.ChunkSize; y++)
                    AddExposedVoxel(new(x, y, z), chunk);

        gameManager.Generator.ChunksToBuild.Enqueue(chunk);

        _stopwatch.Stop();

        Output.Log($"CG: {_stopwatch.Elapsed.TotalMilliseconds * 1000:F0} µs");

        _stopwatch.Reset();
    }

    private int GetGridY(int y, int chunkSizeY) =>
        y / chunkSizeY * chunkSizeY;

    private void SetGridY(Chunk chunk)
    {
        if (chunk.IsChunkFromChunk)
            return;

        bool result = false;
        do result = Generator.GeneratedChunks[chunk.LevelOfDetail].TryRemove(chunk.WorldPosition, out _);
        while (!result);

        int gridY = GetGridY(SampleNoise(chunk, Vector3Short.UnitXZ).SurfaceHeight, chunk.ChunkSize * chunk.VoxelSize);
        chunk.WorldPosition = chunk.WorldPosition.Set(y: gridY);
        
        result = false;
        do result = Generator.GeneratedChunks[chunk.LevelOfDetail].TryAdd(chunk.WorldPosition, chunk);
        while (!result);
    }

    private void CheckChunkVertically(Chunk chunk, NoiseData noiseData)
    {
        if (!chunk.IsBottomChunkGenerated && noiseData.SurfaceHeight == chunk.WorldPosition.Y - 1)
            chunk.IsBottomChunkGenerated = true;
        else if (!chunk.IsTopChunkGenerated && noiseData.SurfaceHeight == chunk.WorldPosition.Y + chunk.ChunkSize * chunk.VoxelSize + 1)
            chunk.IsTopChunkGenerated = true;
        else return;

        Vector3Int chunkPosition = new(chunk.WorldPosition.X, GetGridY(noiseData.SurfaceHeight, chunk.ChunkSize * chunk.VoxelSize), chunk.WorldPosition.Z);
        if (Generator.GeneratedChunks[chunk.LevelOfDetail].ContainsKey(chunkPosition))
            return;

        Chunk newChunk = PoolManager.GetPool<Chunk>().Get();
        newChunk.Initialize(GameManager, chunkPosition, chunk.LevelOfDetail);
        newChunk.IsChunkFromChunk = true;

        bool result = false;
        do result = Generator.GeneratedChunks[chunk.LevelOfDetail].TryAdd(chunkPosition, newChunk);
        while (!result);

        GameManager.ChunkGenerationTask(newChunk);
    }
}

public sealed partial class NoiseSampler
{
    private void AddExposedVoxel(Vector3Short voxelPosition, Chunk chunk)
    {
        if (!SampleVoxel(out var voxelType, ref voxelPosition, chunk))
            return;

        chunk.SetVoxelType(ref voxelPosition, ref voxelType);
        chunk.SetSolidVoxel(ref voxelPosition);

        Vector3Short adjacentVoxelPosition = new();

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
            if (chunk.IsVoxelEmpty(ref adjacentVoxelPosition) && !SampleVoxel(out adjacentVoxelType, ref adjacentVoxelPosition, chunk))
            {
                chunk.SetExposedVoxel(ref voxelPosition);

                return;
            }

            if (adjacentVoxelType is not VoxelType.None)
            {
                chunk.SetVoxelType(ref adjacentVoxelPosition, ref adjacentVoxelType);
                chunk.SetSolidVoxel(ref adjacentVoxelPosition);
            }
        }
    }

    private bool SampleVoxel(out VoxelType sample, ref Vector3Short voxelPosition, Chunk chunk)
    {
        sample = chunk.GetVoxelType(ref voxelPosition);

        if (sample is not VoxelType.None)
            return true;

        var noiseData = SampleNoise(chunk, voxelPosition.X, voxelPosition.Z);
        int surfaceHeight = noiseData.SurfaceHeight;
        int mountainHeight = noiseData.MountainHeight;
        int undergroundDetail = noiseData.UndergroundDetail;
        int bedrockHeight = noiseData.BedrockHeight;

        CheckChunkVertically(chunk, noiseData);

        int x = voxelPosition.X;
        int y = voxelPosition.Y * chunk.VoxelSize + chunk.WorldPosition.Y;
        int z = voxelPosition.Z;

        if (y < surfaceHeight)
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
        else if (y <= surfaceHeight)
            sample = VoxelType.Grass;

        return sample is not VoxelType.None;
    }

    private NoiseData SampleNoise(Chunk chunk, Vector3Short position) =>
        SampleNoise(chunk, position.X, position.Z);

    private NoiseData SampleNoise(Chunk chunk, int x, int z)
    {
        NoiseData noiseData = chunk.GetNoiseData(x, z);
        if (noiseData is not null)
            return noiseData;

        int nx = x * chunk.VoxelSize + chunk.WorldPosition.X;
        int nz = z * chunk.VoxelSize + chunk.WorldPosition.Z;

        ushort surfaceHeight = GetSurfaceHeight(nx, nz);
        ushort mountainHeight = GetMountainHeight(nx, nz);
        byte undergroundDetail = GetUndergroundDetail(nx, nz);
        byte bedrockHeight = GetBedrockNoise();

        mountainHeight = (ushort)Math.Max(0, mountainHeight - 80);
        surfaceHeight += mountainHeight;

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

    private Billow _surfaceNoiseMicro = new()
    {
        Primitive2D = s_perlinPrimitive,
        OctaveCount = 5,        // Fewer octaves for a smoother look
        Frequency = 0.005f,     // Lower frequency for broader features
        Scale = 5,              // Adjust scale to manage the level of detail
    };
    
    private Billow _surfaceNoiseMacro = new()
    {
        Primitive2D = s_perlinPrimitive,
        OctaveCount = 5,    
        Frequency = 0.0002f,      
        Scale = 50,
    };

    private Billow _mountainNoise = new()
    {
        Primitive2D = s_perlinPrimitive,
        OctaveCount = 7,
        Frequency = 0.001f,
        Scale = 100,
    };

    private Billow _undergroundNoise = new()
    {
        Primitive2D = s_perlinPrimitive,
        OctaveCount = 6,
        Frequency = 0.05f,
        Scale = 15,
    };

    public Billow _caveNoise = new()
    {
        Primitive3D = s_perlinPrimitive,
        OctaveCount = 5,
        Frequency = 0.005f,
        Scale = 0.5f
    };

    private ushort GetSurfaceHeight(int x, int z) =>
        (ushort)(_surfaceNoiseMicro.GetValue(x, z) + _surfaceNoiseMacro.GetValue(x, z) + 1000);

    private ushort GetMountainHeight(int x, int z) =>
        (ushort)_mountainNoise.GetValue(x, z);

    private byte GetUndergroundDetail(int x, int z) =>
        (byte)(_undergroundNoise.GetValue(x, z) + 10);

    private byte GetBedrockNoise() =>
        (byte)Random.Next(0, 5);

    private double GetCaveNoise(int x, int y, int z) =>
        _caveNoise.GetValue(x, y, z);
}