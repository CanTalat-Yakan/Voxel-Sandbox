using AVXPerlinNoise;
using System.Numerics;

namespace VoxelSandbox;

public class NoiseSampler
{
    public Perlin Noise = new();
    private Random random = new();

    // Biome definitions
    private enum BiomeType { Plains, Mountains, Desert, Forest }
    private const int BiomeSize = 500; // Adjust to control biome size

    public void GenerateChunkContent(Chunk chunk)
    {
        for (int x = 0; x <= Generator.BaseChunkSizeXZ + 1; x++)
        {
            for (int z = 0; z <= Generator.BaseChunkSizeXZ + 1; z++)
            {
                int worldX = x + chunk.WorldPosition.X;
                int worldZ = z + chunk.WorldPosition.Z;

                // Determine the biome at this location
                BiomeType biome = GetBiome(worldX, worldZ);

                // Get surface height based on biome
                int surfaceHeight = GetSurfaceHeight(worldX, worldZ, biome);

                // Generate terrain
                for (int y = 0; y < Generator.BaseChunkSizeY; y++)
                {
                    Vector3Byte voxelPosition = new(x, y, z);

                    if (y > surfaceHeight)
                    {
                        // Air above the surface
                        continue;
                    }
                    else if (y == surfaceHeight)
                    {
                        // Surface block
                        VoxelType surfaceBlock = GetSurfaceBlockType(biome);
                        chunk.SetVoxel(voxelPosition, surfaceBlock);

                        // Add trees in forest biome
                        if (biome == BiomeType.Forest && random.NextDouble() < 0.05)
                        {
                            GenerateTree(chunk, x, y + 1, z);
                        }
                    }
                    else if (y > surfaceHeight - 5)
                    {
                        // Sub-surface blocks
                        VoxelType subSurfaceBlock = GetSubSurfaceBlockType(biome);
                        chunk.SetVoxel(voxelPosition, subSurfaceBlock);
                    }
                    else
                    {
                        // Underground blocks
                        // Check for caves
                        double caveValue = GetCaveNoise(worldX, y, worldZ);
                        if (caveValue > 0.5)
                        {
                            // Air in caves
                            continue;
                        }
                        else
                        {
                            // Chance to generate ores
                            VoxelType undergroundBlock = GetUndergroundBlockType(y);
                            chunk.SetVoxel(voxelPosition, undergroundBlock);
                        }
                    }
                }
            }
        }

        RemoveUnexposedVoxels(chunk);

        GameManager.Instance.Generator.ChunksToBuild.Enqueue(chunk);
    }

    private BiomeType GetBiome(int x, int z)
    {
        double biomeNoise = Noise.OctavePerlin(x, 0, z, scale: BiomeSize);
        if (biomeNoise < 0.25)
            return BiomeType.Desert;
        else if (biomeNoise < 0.5)
            return BiomeType.Plains;
        else if (biomeNoise < 0.75)
            return BiomeType.Forest;
        else
            return BiomeType.Mountains;
    }

    private int GetSurfaceHeight(int x, int z, BiomeType biome)
    {
        double height = 0;
        switch (biome)
        {
            case BiomeType.Plains:
                height = Noise.OctavePerlin(x, 0, z, scale: 100) * 10 + 64;
                break;
            case BiomeType.Mountains:
                double mountainNoise = Noise.OctavePerlin(x, 0, z, scale: 50) * 30;
                double hillNoise = Noise.OctavePerlin(x, 0, z, scale: 100) * 10;
                height = mountainNoise + hillNoise + 80;
                break;
            case BiomeType.Desert:
                height = Noise.OctavePerlin(x, 0, z, scale: 100) * 5 + 62;
                break;
            case BiomeType.Forest:
                height = Noise.OctavePerlin(x, 0, z, scale: 80) * 12 + 68;
                break;
        }
        return (int)height;
    }

    private VoxelType GetSurfaceBlockType(BiomeType biome)
    {
        switch (biome)
        {
            case BiomeType.Plains:
            case BiomeType.Forest:
                return VoxelType.Grass;
            case BiomeType.Mountains:
                return VoxelType.Stone;
            case BiomeType.Desert:
                return VoxelType.Sand;
            default:
                return VoxelType.Grass;
        }
    }

    private VoxelType GetSubSurfaceBlockType(BiomeType biome)
    {
        switch (biome)
        {
            case BiomeType.Plains:
            case BiomeType.Forest:
                return VoxelType.Dirt;
            case BiomeType.Mountains:
                return VoxelType.Stone;
            case BiomeType.Desert:
                return VoxelType.Sandstone;
            default:
                return VoxelType.Dirt;
        }
    }

    private VoxelType GetUndergroundBlockType(int y)
    {
        // Chance to generate ores
        if (y < 20 && random.NextDouble() < 0.02)
            return VoxelType.DiamondOre;
        else if (y < 40 && random.NextDouble() < 0.04)
            return VoxelType.IronOre;
        else
            return VoxelType.Stone;
    }

    private double GetCaveNoise(int x, int y, int z)
    {
        return Noise.OctavePerlin(x, y, z, nOctaves: 3, scale: 60);
    }

    private void GenerateTree(Chunk chunk, int x, int y, int z)
    {
        // Simple tree structure
        int height = random.Next(4, 7);
        for (int i = 0; i < height; i++)
        {
            Vector3Byte trunkPosition = new(x, y + i, z);
            chunk.SetVoxel(trunkPosition, VoxelType.Log);
        }

        //// Add leaves at the top
        //for (int dx = -2; dx <= 2; dx++)
        //    for (int dy = 0; dy <= 2; dy++)
        //        for (int dz = -2; dz <= 2; dz++)
        //        {
        //            if (dx * dx + dy * dy + dz * dz <= 3)
        //            {
        //                Vector3Byte leafPosition = new(x + dx, y + height + dy, z + dz);
        //                chunk.SetVoxel(leafPosition, VoxelType.Leaves);
        //            }
        //        }
    }

    private void RemoveUnexposedVoxels(Chunk chunk)
    {
        Dictionary<Vector3Byte, VoxelType> exposedVoxels = new();

        // Check if the voxel is exposed (has at least one neighbor that is empty)
        foreach (var voxel in chunk.VoxelData)
        {
            // Set border voxels as exposed
            if (!chunk.IsWithinBounds(voxel.Key))
            {
                exposedVoxels.Add(voxel.Key, voxel.Value);

                continue;
            }

            bool funcIsExposed = false;
            bool isExposed = IterateAdjacentVoxels(voxel.Key, (Vector3Byte adjacentLocalPosition) =>
            {
                // If the neighbor voxel is empty, the current voxel is exposed
                if (!chunk.HasVoxel(adjacentLocalPosition))
                    if (!funcIsExposed)
                        funcIsExposed = true;
            }, continueOnEdgeCases: false);

            if (isExposed)
                exposedVoxels.Add(voxel.Key, voxel.Value);
            else if (funcIsExposed)
                exposedVoxels.Add(voxel.Key, voxel.Value);
        }

        foreach (var voxel in exposedVoxels.Keys.ToArray())
            IterateAdjacentVoxels(voxel, (Vector3Byte adjacentLocalPosition) =>
            {
                if (!exposedVoxels.ContainsKey(adjacentLocalPosition))
                    if (chunk.VoxelData.ContainsKey(adjacentLocalPosition))
                        exposedVoxels.Add(adjacentLocalPosition, VoxelType.Air);
                    else
                        exposedVoxels.Add(adjacentLocalPosition, VoxelType.None);
            });

        chunk.VoxelData.Clear();
        chunk.VoxelData = exposedVoxels;
    }

    private bool IterateAdjacentVoxels(Vector3Byte localPosition, Action<Vector3Byte> action, bool continueOnEdgeCases = true)
    {
        Vector3Int adjacentVoxel = new();

        foreach (var direction in Vector3Int.Directions)
        {
            adjacentVoxel.X = direction.X + localPosition.X;
            adjacentVoxel.Y = direction.Y + localPosition.Y;
            adjacentVoxel.Z = direction.Z + localPosition.Z;

            if (adjacentVoxel.X <= 0 || adjacentVoxel.X >= Generator.BaseChunkSizeXZ + 1
             || adjacentVoxel.Z <= 0 || adjacentVoxel.Z >= Generator.BaseChunkSizeXZ + 1)
                if (continueOnEdgeCases) continue;

            // If the neighbor is outside the world bounds, consider it as empty
            if (adjacentVoxel.Y > Generator.BaseChunkSizeY)
                if (continueOnEdgeCases) continue;
                else return true;

            // If the neighbor is below ground, consider it as solid
            if (adjacentVoxel.Y < 0)
                if (continueOnEdgeCases) continue;
                else return false;

            // If the neighbor voxel is empty, the current voxel is exposed
            action.Invoke(new(adjacentVoxel.X, adjacentVoxel.Y, adjacentVoxel.Z));
        }

        // All neighbors are solid and the voxel is not exposed
        return false;
    }
}