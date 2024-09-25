using LibNoise.Filter;
using LibNoise.Primitive;

namespace VoxelSandbox;

public class NoiseSampler
{
    private Random _random = new();
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

    private Dictionary<Vector3Byte, VoxelType> _cachedVoxelDictionary = new();

    public void GenerateChunkContent(Chunk chunk)
    {
        int voxelSize = chunk.VoxelSize;
        int chunkSizeXZMultiplyer = chunk.LevelOfDetail == 0 ? 1 : 2;

        for (int x = 1; x <= Generator.ChunkSizeXZ * chunkSizeXZMultiplyer; x++)
            for (int z = 1; z <= Generator.ChunkSizeXZ * chunkSizeXZMultiplyer; z++)
            {
                int surfaceHeight = GetSurfaceHeight(chunk.WorldPosition.X + x * chunk.VoxelSize, chunk.WorldPosition.Z + z * chunk.VoxelSize);
                int mountainHeight = GetMountainHeight(chunk.WorldPosition.X + x * chunk.VoxelSize, chunk.WorldPosition.Z + z * chunk.VoxelSize);
                int undergroundDetail = GetUndergroundDetail(chunk.WorldPosition.X + x * chunk.VoxelSize, chunk.WorldPosition.Z + z * chunk.VoxelSize);
                int bedrockHeight = _random.Next(5);

                surfaceHeight += (mountainHeight + 1) / 2;

                for (int y = voxelSize; y < Generator.ChunkSizeY; y += voxelSize)
                    if (!_cachedVoxelDictionary.ContainsKey(new(x, y, z)))
                        AddVoxelIfExposed(x, y, z, surfaceHeight, mountainHeight, undergroundDetail, bedrockHeight, voxelSize, chunk);
            }

        RemoveUnexposedVoxels(chunk);

        GameManager.Instance.Generator.ChunksToBuild.Enqueue(chunk);
    }

    private void AddVoxelIfExposed(int x, int y, int z, int surfaceHeight, int mountainHeight, int undergroundDetail, int bedrockHeight, int voxelSize, Chunk chunk)
    {
        if (!SampleVoxel(out var sample, x, y, z, surfaceHeight, mountainHeight, undergroundDetail, bedrockHeight, voxelSize, chunk))
            return;

        var voxel = sample.Value;
        _cachedVoxelDictionary.Add(voxel.Key, voxel.Value);

        IterateAdjacentVoxels(continueOnOutsideBounds: true, voxel.Key,
            (Vector3Byte adjacentLocalPosition) =>
            {
                if (!_cachedVoxelDictionary.ContainsKey(adjacentLocalPosition))
                    if (!SampleVoxel(out var sample, adjacentLocalPosition, surfaceHeight, mountainHeight, undergroundDetail, bedrockHeight, voxelSize, chunk))
                    {
                        // Air Voxel        | adjacentLocalPosition
                        // Exposed Voxel    | voxel.Key

                        if (!_cachedVoxelDictionary.ContainsKey(voxel.Key))
                            _cachedVoxelDictionary.Add(voxel.Key, voxel.Value);
                    }
                    else if (!_cachedVoxelDictionary.ContainsKey(voxel.Key))
                        _cachedVoxelDictionary.Add(voxel.Key, voxel.Value);
            });
    }

    private bool SampleVoxel(out KeyValuePair<Vector3Byte, VoxelType>? sample, int x, int y, int z, int surfaceHeight, int mountainHeight, int undergroundDetail, int bedrockHeight, int voxelSize, Chunk chunk) =>
        SampleVoxel(out sample, new(x, y, z), surfaceHeight, mountainHeight, undergroundDetail, bedrockHeight, voxelSize, chunk);

    private bool SampleVoxel(out KeyValuePair<Vector3Byte, VoxelType>? sample, Vector3Byte voxelPosition, int surfaceHeight, int mountainHeight, int undergroundDetail, int bedrockHeight, int voxelSize, Chunk chunk)
    {
        sample = null;

        int x = voxelPosition.X;
        int y = voxelPosition.Y;
        int z = voxelPosition.Z;

        if (chunk.LevelOfDetail > 0)
        {
            if (y <= surfaceHeight + voxelSize && y >= surfaceHeight)
                sample = new(voxelPosition, VoxelType.Grass);
        }
        else if (y < surfaceHeight)
        {
            voxelPosition = new(x, y / voxelSize, z);

            // Check cave noise to determine if this voxel should be empty (cave)
            if (y < undergroundDetail)
                sample = new(voxelPosition, y - 1 < bedrockHeight ? VoxelType.DiamondOre : VoxelType.Stone);
            else if (y + undergroundDetail < surfaceHeight)
            {
                double caveValue = GetCaveNoise(x + chunk.WorldPosition.X * chunk.VoxelSize, y * 2, z + chunk.WorldPosition.Z * chunk.VoxelSize);

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

    private int GetSurfaceHeight(int x, int z) =>
        (int)_surfaceNoise.GetValue(x, z) + 100;

    private int GetMountainHeight(int x, int z) =>
        (int)_mountainNoise.GetValue(x, z);

    private int GetUndergroundDetail(int x, int z) =>
        (int)_undergroundNoise.GetValue(x, z) + 10;

    private double GetCaveNoise(int x, int y, int z) =>
        _caveNoise.GetValue(x, y, z);

    private void RemoveUnexposedVoxels(Chunk chunk)
    {
        Dictionary<Vector3Byte, VoxelType> exposedVoxels = new();
        bool renderBorders = chunk.LevelOfDetail == 0;

        // Check if the voxel is exposed (has at least one neighbor that is empty)
        foreach (var voxel in chunk.VoxelData)
        {
            bool IsVoxelExposed = false;
            bool IsEdgeCaseExposed = IterateAdjacentVoxels(continueOnOutsideBounds: renderBorders, voxel.Key,
                (Vector3Byte adjacentLocalPosition) =>
                {
                    // If the adjacent voxel is empty, the current voxel is exposed
                    if (!chunk.HasVoxel(adjacentLocalPosition))
                        if (!IsVoxelExposed)
                            IsVoxelExposed = true;
                });

            if (IsEdgeCaseExposed || IsVoxelExposed)
                exposedVoxels.Add(voxel.Key, voxel.Value);
        }

        // Add further voxel information for mesh building
        foreach (var voxel in exposedVoxels.Keys.ToArray())
            IterateAdjacentVoxels(continueOnOutsideBounds: true, voxel,
                (Vector3Byte adjacentLocalPosition) =>
                {
                    if (!exposedVoxels.ContainsKey(adjacentLocalPosition))
                        if (!chunk.VoxelData.ContainsKey(adjacentLocalPosition))
                            exposedVoxels.Add(adjacentLocalPosition, VoxelType.None); // None for outside the mesh
                });

        chunk.VoxelData.Clear();
        chunk.VoxelData = exposedVoxels;
    }

    private bool IterateAdjacentVoxels(bool continueOnOutsideBounds, Vector3Byte localPosition, Action<Vector3Byte> action)
    {
        foreach (var direction in Vector3Int.Directions)
        {
            Vector3Byte adjacentVoxel = (direction + localPosition).ToVector3Byte();

            if (continueOnOutsideBounds)
                if (!Chunk.IsWithinBounds(adjacentVoxel))
                    continue;

            action.Invoke(new(adjacentVoxel.X, adjacentVoxel.Y, adjacentVoxel.Z));
        }

        return false;
    }
}