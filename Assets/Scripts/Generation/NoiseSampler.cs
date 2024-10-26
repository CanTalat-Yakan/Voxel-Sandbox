using System.Diagnostics;

using LibNoise.Filter;
using LibNoise.Primitive;

using Engine.Components;
using Engine.Essentials;
using Engine.Helper;
using Engine.Utilities;
using Engine.Buffer;

namespace VoxelSandbox;

public class NoiseData()
{
    public bool initialized = false;
    public ushort SurfaceHeight, UndergroundDetail, BedrockHeight;
}

public sealed partial class NoiseSampler
{
    GameManager GameManager;

    Compute ComputeShader = new();

    private Stopwatch _stopwatch = new();

    public void GenerateChunkContent(Chunk chunk, GameManager gameManager)
    {
        _stopwatch.Restart();

        GameManager = gameManager;

        SetGridY(chunk);

        ComputeShader.Initialize("ChunkNoiseGenerator", new RootSignatureHelper().AddUnorderedAccessViewTable().AddShaderResourceViewTable());
        ComputeShader.Setup();
        ComputeData data = new();
        //data.SetData();
        ComputeShader.Dispatch();

        for (int x = 1; x <= chunk.ChunkSize; x++)
            for (int z = 1; z <= chunk.ChunkSize; z++)
                for (int y = 1; y <= chunk.ChunkSize; y++)
                    AddExposedVoxel(chunk, new(x, y, z));

        Generator.ChunksToBuild.Enqueue(chunk);

        Output.Log($"CG: {_stopwatch.Elapsed.TotalMilliseconds * 1000:F0} µs");
    }
}

public sealed partial class NoiseSampler
{
    public void AddExposedVoxel(Chunk chunk, Vector3Short voxelPosition, VoxelType voxelType = VoxelType.None)
    {
        if (voxelType is VoxelType.None)
            if (!SampleVoxel(chunk, ref voxelPosition, out voxelType))
                return;

        if (chunk.IsWithinBounds(ref voxelPosition))
            chunk.SetVoxelType(ref voxelPosition, ref voxelType);
        chunk.SetSolidVoxel(ref voxelPosition);

        int x = voxelPosition.X;
        int y = voxelPosition.Y;
        int z = voxelPosition.Z;

        VoxelType adjacentVoxelType = VoxelType.None;
        Vector3Short adjacentVoxelPosition = new();

        foreach (var direction in Vector3Int.Directions)
        {
            adjacentVoxelType = VoxelType.None;
            adjacentVoxelPosition.Set(
                (byte)(x + direction.X),
                (byte)(y + direction.Y),
                (byte)(z + direction.Z));

            // If the adjacent voxel was not found, the current iterated voxel is exposed
            if (chunk.IsVoxelEmpty(ref adjacentVoxelPosition) && !SampleVoxel(chunk, ref adjacentVoxelPosition, out adjacentVoxelType))
            {
                chunk.AddExposedVoxel(ref voxelPosition);

                if (chunk.IsAtBoundsBorder(ref adjacentVoxelPosition))
                    return;
            }
            else if (adjacentVoxelPosition.Y > chunk.ChunkSize)
                AddChunkOnTop(chunk);
            else if (adjacentVoxelPosition.Y == 0)
                AddChunkBelow(chunk);

            if (adjacentVoxelType is not VoxelType.None)
            {
                if (chunk.IsWithinBounds(ref adjacentVoxelPosition))
                    chunk.SetVoxelType(ref adjacentVoxelPosition, ref adjacentVoxelType);
                chunk.SetSolidVoxel(ref adjacentVoxelPosition);
            }
        }
    }

    public void RemoveExposedVoxel(Chunk chunk, Vector3Short voxelPosition)
    {
        VoxelType voxelType = VoxelType.None;
        chunk.SetVoxelType(ref voxelPosition, ref voxelType);
        chunk.SetEmptyVoxel(ref voxelPosition);
        chunk.RemoveExposedVoxel(ref voxelPosition);

        int x = voxelPosition.X;
        int y = voxelPosition.Y;
        int z = voxelPosition.Z;

        if (y > chunk.ChunkSize)
            AddChunkOnTop(chunk);
        else if (y == 0)
            AddChunkBelow(chunk);

        Vector3Short adjacentVoxelPosition = new();

        foreach (var direction in Vector3Int.Directions)
        {
            adjacentVoxelPosition.Set(
                (byte)(x + direction.X),
                (byte)(y + direction.Y),
                (byte)(z + direction.Z));

            // If the adjacent voxel was not found, the current iterated voxel is exposed
            if (!chunk.IsVoxelEmpty(ref adjacentVoxelPosition))
                chunk.AddExposedVoxel(ref adjacentVoxelPosition);
        }
    }

    private bool SampleVoxel(Chunk chunk, ref Vector3Short voxelPosition, out VoxelType sample)
    {
        sample = VoxelType.None;

        if (chunk.IsWithinBounds(ref voxelPosition))
        {
            sample = chunk.GetVoxelType(ref voxelPosition);

            if (sample is not VoxelType.None)
                return true;
        }

        var noiseData = SampleNoise(chunk, voxelPosition.X, voxelPosition.Z);
        int surfaceHeight = noiseData.SurfaceHeight;
        int undergroundDetail = noiseData.UndergroundDetail;
        int bedrockHeight = noiseData.BedrockHeight;

        CheckChunkVertically(chunk, noiseData);

        int x = voxelPosition.X * chunk.VoxelSize + chunk.WorldPosition.X;
        int y = voxelPosition.Y * chunk.VoxelSize + chunk.WorldPosition.Y;
        int z = voxelPosition.Z * chunk.VoxelSize + chunk.WorldPosition.Z;

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
                    double caveValue = GetCaveNoise(x, y * 2, z);

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

    private NoiseData SampleNoise(Chunk chunk, int x, int z)
    {
        NoiseData noiseData = chunk.GetNoiseData(x, z);
        if (noiseData.initialized)
            return noiseData;

        int noiseX = x * chunk.VoxelSize + chunk.WorldPosition.X;
        int noiseZ = z * chunk.VoxelSize + chunk.WorldPosition.Z;

        noiseData.SurfaceHeight = (ushort)(GetSurfaceHeight(noiseX, noiseZ) + GetMountainHeight(noiseX, noiseZ));
        noiseData.UndergroundDetail = GetUndergroundDetail(noiseX, noiseZ);
        noiseData.BedrockHeight = GetBedrockNoise();

        noiseData.initialized = true;

        return noiseData;
    }
}

public sealed partial class NoiseSampler
{
    private void SetGridY(Chunk chunk)
    {
        if (chunk.IsChunkFromChunk)
            return;

        int gridY = chunk.GetGridY(SampleNoise(chunk, 1, 1).SurfaceHeight);
        chunk.UnscaledPosition.Set(y: gridY);
    }

    private void CheckChunkVertically(Chunk chunk, NoiseData noiseData)
    {
        if (chunk.TopChunk is null && noiseData.SurfaceHeight > chunk.WorldPosition.Y + chunk.ChunkSize * chunk.VoxelSize)
            AddChunkOnTop(chunk);
        else if (chunk.BottomChunk is null && noiseData.SurfaceHeight < chunk.WorldPosition.Y)
            AddChunkBelow(chunk);
    }

    private void AddChunkOnTop(Chunk chunk, bool checkChunkFromChunk = true)
    {
        if (chunk.TopChunk is not null)
            return;

        if (checkChunkFromChunk && chunk.IsChunkFromChunk)
            return;

        chunk.TopChunk = PoolManager.GetPool<Chunk>().Get().Initialize(GameManager,
            chunk.LevelOfDetail,
            chunk.UnscaledPosition.X,
            chunk.UnscaledPosition.Z,
            chunk.UnscaledPosition.Y + 1);

        chunk.TopChunk.IsChunkFromChunk = true;

        GameManager.ChunkGenerationTask(chunk.TopChunk);
    }

    private void AddChunkBelow(Chunk chunk, bool checkChunkFromChunk = true)
    {
        if (chunk.BottomChunk is not null)
            return;

        if (checkChunkFromChunk && chunk.IsChunkFromChunk)
            return;

        chunk.BottomChunk = PoolManager.GetPool<Chunk>().Get().Initialize(GameManager,
            chunk.LevelOfDetail,
            chunk.UnscaledPosition.X,
            chunk.UnscaledPosition.Z,
            chunk.UnscaledPosition.Y - 1);

        chunk.BottomChunk.IsChunkFromChunk = true;

        GameManager.ChunkGenerationTask(chunk.BottomChunk);
    }
}

public sealed partial class NoiseSampler
{
    private static Random Random = new(GameManager.Seed);
    private static SimplexPerlin s_perlinPrimitive = new(GameManager.Seed, LibNoise.NoiseQuality.Fast);

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
        (ushort)Math.Max(0, _mountainNoise.GetValue(x, z) - 80);

    private byte GetUndergroundDetail(int x, int z) =>
        (byte)(_undergroundNoise.GetValue(x, z) + 10);

    private byte GetBedrockNoise() =>
        (byte)Random.Next(0, 5);

    private double GetCaveNoise(int x, int y, int z) =>
        _caveNoise.GetValue(x, y, z);
}