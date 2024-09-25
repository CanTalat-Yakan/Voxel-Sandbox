using LibNoise.Filter;
using LibNoise.Primitive;

namespace VoxelSandbox;

public record NoiseData(int SurfaceHeight, int MountainHeight, int UndergroundDetail, int BedrockHeight);

public sealed partial class NoiseSampler
{
    private static SimplexPerlin s_perlinPrimitive = new() { Seed = 1234 };

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
        OctaveCount = 3,        // Fewer octaves for a smoother look
        Frequency = 0.0002f,      // Lower frequency for broader features
        Scale = 200,           // Adjust scale to manage the level of detail
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
    public Billow _bedrockNoise = new()
    {
        Primitive2D = s_perlinPrimitive,
        OctaveCount = 1,
        Frequency = 0.5f,
        Scale = 1
    };

    private Dictionary<Vector3Byte, VoxelType> _cachedVoxelDictionary = new();

    public void GenerateChunkContent(Chunk chunk)
    {
        int voxelSize = chunk.VoxelSize;
        int chunkSizeXZMultiplyer = chunk.LevelOfDetail == 0 ? 1 : 2;

        for (int x = 1; x <= Generator.ChunkSizeXZ * chunkSizeXZMultiplyer; x++)
            for (int z = 1; z <= Generator.ChunkSizeXZ * chunkSizeXZMultiplyer; z++)
                for (int y = voxelSize; y < Generator.ChunkSizeY; y += voxelSize)
                    AddVoxelIfExposed(chunk, new(x, y, z));

        GameManager.Instance.Generator.ChunksToBuild.Enqueue(chunk);
    }
}

public sealed partial class NoiseSampler
{
    private void AddVoxelIfExposed(Chunk chunk, Vector3Byte voxelPosition)
    {
        CheckVoxel(out var voxel, ref voxelPosition, chunk, SampleNoise(chunk, voxelPosition.ToVector2Byte()));

        if (voxel is null)
            return;

        foreach (var direction in Vector3Int.Directions)
        {
            Vector3Byte adjacentVoxelPosition = (direction + voxelPosition).ToVector3Byte();

                if (!Chunk.IsWithinBounds(adjacentVoxelPosition))
                    continue;

            CheckVoxel(out var adjacentVoxel, ref adjacentVoxelPosition, chunk, SampleNoise(chunk, adjacentVoxelPosition.ToVector2Byte()));

            // If the adjacent voxel was not found, the current iterated voxel is exposed
            if (adjacentVoxel is null)
            {
                chunk.SetVoxel(adjacentVoxelPosition, VoxelType.None); // air voxel outside the mesh
                chunk.SetVoxel(voxelPosition, voxel.Value.Value); // the mesh
            }
        }
    }

    private void CheckVoxel(out KeyValuePair<Vector3Byte, VoxelType>? voxel, ref Vector3Byte voxelPosition, Chunk chunk, NoiseData noiseData)
    {
        voxel = null;

        if (_cachedVoxelDictionary.ContainsKey(voxelPosition))
            voxel = new(voxelPosition, _cachedVoxelDictionary[voxelPosition]);
        else if (SampleVoxel(out var sample, ref voxelPosition, chunk, noiseData))
        {
            _cachedVoxelDictionary.Add(voxelPosition, sample.Value.Value);

            voxel = sample.Value;
        }
    }

    private bool SampleVoxel(out KeyValuePair<Vector3Byte, VoxelType>? sample, ref Vector3Byte voxelPosition, Chunk chunk, NoiseData noiseData)
    {
        sample = null;

        int surfaceHeight = noiseData.SurfaceHeight;
        int mountainHeight = noiseData.MountainHeight;
        int undergroundDetail = noiseData.UndergroundDetail;
        int bedrockHeight = noiseData.BedrockHeight;

        int x = voxelPosition.X;
        int y = voxelPosition.Y;
        int z = voxelPosition.Z;

        if (chunk.LevelOfDetail > 0)
        {
            if (y <= surfaceHeight + chunk.VoxelSize && y >= surfaceHeight)
                sample = new(voxelPosition, VoxelType.Grass);
        }
        else if (y < surfaceHeight)
        {
            voxelPosition = new(x, y / chunk.VoxelSize, z);

            // Check cave noise to determine if this voxel should be empty (cave)
            if (y < undergroundDetail)
                sample = new(voxelPosition, y - 1 < bedrockHeight ? VoxelType.DiamondOre : VoxelType.Stone);
            else if (y + undergroundDetail < surfaceHeight)
            {
                int nx = x + chunk.WorldPosition.X * chunk.VoxelSize;
                int nz = z + chunk.WorldPosition.Z * chunk.VoxelSize;

                double caveValue = GetCaveNoise(nx, y * 2, nz);

                if (caveValue > 0.25 && caveValue < 0.6)
                    sample = new(voxelPosition, VoxelType.Stone);
            }
            else if (y + undergroundDetail - 5 < surfaceHeight)
                sample = new(voxelPosition, VoxelType.Stone);
            else
                sample = new(voxelPosition, VoxelType.Dirt);
        }
        else if (y == surfaceHeight)
            sample = new(voxelPosition, VoxelType.Grass);

        return sample is not null;
    }

    private NoiseData SampleNoise(Chunk chunk, Vector2Byte noisePosition)
    {
        if (chunk.NoiseData.ContainsKey(noisePosition))
            return chunk.NoiseData[noisePosition];

        int nx = noisePosition.X + chunk.WorldPosition.X * chunk.VoxelSize;
        int nz = noisePosition.Z + chunk.WorldPosition.Z * chunk.VoxelSize;

        int surfaceHeight = GetSurfaceHeight(nx, nz);
        int mountainHeight = GetMountainHeight(nx, nz);
        int undergroundDetail = GetUndergroundDetail(nx, nz);
        int bedrockHeight = GetBedrockNoise(nx, nz);

        surfaceHeight += (mountainHeight + 1) / 2;

        NoiseData noiseData = new(surfaceHeight, mountainHeight, undergroundDetail, bedrockHeight);

        chunk.SetNoiseData(noisePosition, noiseData);

        return noiseData;
    }
}

public sealed partial class NoiseSampler
{
    private int GetSurfaceHeight(int x, int z) =>
        (int)_surfaceNoise.GetValue(x, z) + 100;

    private int GetMountainHeight(int x, int z) =>
        (int)_mountainNoise.GetValue(x, z);

    private int GetUndergroundDetail(int x, int z) =>
        (int)_undergroundNoise.GetValue(x, z) + 10;

    private double GetCaveNoise(int x, int y, int z) =>
        _caveNoise.GetValue(x, y, z);

    private int GetBedrockNoise(int x, int z) =>
        (int)_bedrockNoise.GetValue(x + z, x - z) % 5;
}