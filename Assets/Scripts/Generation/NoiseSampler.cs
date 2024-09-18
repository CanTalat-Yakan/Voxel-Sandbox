using AVXPerlinNoise;

namespace VoxelSandbox;

public class NoiseSampler
{
    public Perlin Noise = new();

    public void GenerateChunkContent(Chunk chunk)
    {
        Random random = new();

        for (int x = 0; x <= Generator.BaseChunkSizeXZ + 1; x++)
            for (int z = 0; z <= Generator.BaseChunkSizeXZ + 1; z++)
            {
                int surfaceHeight = GetSurfaceHeight(x + chunk.WorldPosition.X, z + chunk.WorldPosition.Z);
                int undergroundDetail = GetSurfaceDetail(x + chunk.WorldPosition.X, z + chunk.WorldPosition.Z);
                undergroundDetail = 20;
                int bedrockHeight = random.Next(5);

                for (int y = 0; y < Generator.BaseChunkSizeY; y++)
                    // Only generate solid voxels below the surface
                    if (surfaceHeight > y)
                    {
                        // Check cave noise to determine if this voxel should be empty (cave)
                        if (surfaceHeight > y + undergroundDetail && y > undergroundDetail)
                        {
                            double caveValue = GetCaveNoise(x + chunk.WorldPosition.X, y * 2, z + chunk.WorldPosition.Z);

                            if ((caveValue < 0.25 || caveValue > 0.6))
                                continue;

                            chunk.SetVoxel(new(x, y, z), VoxelType.Stone);
                        }
                        else
                            chunk.SetVoxel(new(x, y, z), VoxelType.Grass);
                    }
            }

        RemoveUnexposedVoxels(chunk);

        GameManager.Instance.Generator.ChunksToBuild.Enqueue(chunk);
    }

    private int GetSurfaceHeight(int x, int z) =>
        (int)(Noise.OctavePerlin(x, 0, z, scale: 100) * 75) + 100;

    private int GetSurfaceDetail(int x, int z) =>
        (int)(Noise.OctavePerlin(x, 0, z, scale: 10) * 75) + 100;

    private double GetCaveNoise(int x, int y, int z) =>
        Noise.OctavePerlin(x, y, z, nOctaves: 4, scale: 50);

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
            bool isExposed = IterateAdjacentVoxels(chunk, voxel.Key, (Vector3Byte adjacentLocalPosition) =>
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
            IterateAdjacentVoxels(chunk, voxel, (Vector3Byte adjacentLocalPosition) =>
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

    private bool IterateAdjacentVoxels(Chunk chunk, Vector3Byte localPosition, Action<Vector3Byte> func, bool continueOnEdgeCases = true)
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
            func.Invoke(new(adjacentVoxel.X, adjacentVoxel.Y, adjacentVoxel.Z));
        }

        // All neighbors are solid and the voxel is not exposed
        return false;
    }
}