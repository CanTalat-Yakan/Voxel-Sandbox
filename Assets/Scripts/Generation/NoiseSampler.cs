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
    public Billow _bedrockNoise = new()
    {
        Primitive2D = s_perlinPrimitive,
        OctaveCount = 1,
        Frequency = 0.5f,
        Scale = 1
    };

    public void GenerateChunkContent(Chunk chunk)
    {
        int voxelSize = chunk.VoxelSize;

        chunk.SolidVoxelData = new bool[chunk.MaxVoxelCapacity];
        chunk.VoxelTypeData = new VoxelType[chunk.MaxVoxelCapacity];

        for (int x = 1; x <= chunk.ChunkSizeXZ; x++)
            for (int z = 1; z <= chunk.ChunkSizeXZ; z++)
                for (int y = 1; y < chunk.ChunkSizeY; y ++)
                    AddVoxelIfExposed(new(x, y, z), chunk);

        GameManager.Instance.Generator.ChunksToBuild.Enqueue(chunk);
    }
}

public sealed partial class NoiseSampler
{
    private void AddVoxelIfExposed(Vector3Byte voxelPosition, Chunk chunk)
    {
        if (!SampleVoxel(out var voxel, ref voxelPosition, chunk))
            return;

        chunk.SetSolidVoxel(voxelPosition);

        foreach (var direction in Vector3Int.Directions)
        {
            Vector3Byte adjacentVoxelPosition = (direction + voxelPosition).ToVector3Byte();
            VoxelType adjacentVoxelType = VoxelType.None;

            // If the adjacent voxel was not found, the current iterated voxel is exposed
            if (chunk.IsVoxelEmpty(adjacentVoxelPosition) && !SampleVoxel(out adjacentVoxelType, ref adjacentVoxelPosition, chunk))
            {
                chunk.SetVoxelType(voxelPosition, voxel);

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

        var noiseData = SampleNoise(chunk, ref voxelPosition);
        int surfaceHeight = noiseData.SurfaceHeight;
        int mountainHeight = noiseData.MountainHeight;
        int undergroundDetail = noiseData.UndergroundDetail;
        int bedrockHeight = noiseData.BedrockHeight;

        int x = voxelPosition.X;
        int y = voxelPosition.Y * chunk.VoxelSize;
        int z = voxelPosition.Z;

        if (chunk.LevelOfDetail > 0)
        {
            if (y <= surfaceHeight + chunk.VoxelSize && y >= surfaceHeight)
                sample = VoxelType.Grass;
        }
        else if (y < surfaceHeight)
        {
            voxelPosition = new(x, y / chunk.VoxelSize, z);

            // Check cave noise to determine if this voxel should be empty (cave)
            if (y < undergroundDetail)
                sample = y - 1 < bedrockHeight ? VoxelType.DiamondOre : VoxelType.Stone;
            else if (y + undergroundDetail < surfaceHeight)
            {
                int nx = chunk.WorldPosition.X + x * chunk.VoxelSize;
                int nz = chunk.WorldPosition.Z + z * chunk.VoxelSize;

                double caveValue = GetCaveNoise(nx, y * 2, nz);

                if (!(caveValue < 0.25 || caveValue > 0.6))
                    sample = VoxelType.Stone;
            }
            else if (y + undergroundDetail - 5 < surfaceHeight)
                sample = VoxelType.Stone;
            else
                sample = VoxelType.Dirt;
        }
        else if (y == surfaceHeight)
            sample = VoxelType.Grass;

        return sample is not VoxelType.None;
    }

    private NoiseData SampleNoise(Chunk chunk, ref Vector3Byte noisePosition)
    {
        if (chunk.TryGetNoiseData(noisePosition.X, noisePosition.Z, out var noiseData))
            return noiseData;

        int nx = chunk.WorldPosition.X + noisePosition.X * chunk.VoxelSize;
        int nz = chunk.WorldPosition.Z + noisePosition.Z * chunk.VoxelSize;

        int surfaceHeight = GetSurfaceHeight(nx, nz);
        int mountainHeight = GetMountainHeight(nx, nz);
        int undergroundDetail = GetUndergroundDetail(nx, nz);
        int bedrockHeight = GetBedrockNoise(nx, nz);

        surfaceHeight += (mountainHeight + 1) / 2;

        noiseData = new(surfaceHeight, mountainHeight, undergroundDetail, bedrockHeight);

        chunk.SetNoiseData(noisePosition.X, noisePosition.Z, noiseData);

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