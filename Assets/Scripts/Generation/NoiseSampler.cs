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
                int bedrockHeight = random.Next(5);

                for (int y = 0; y < Generator.BaseChunkSizeY; y++)
                    // Only generate solid voxels below the surface
                    if (surfaceHeight > y)
                    {
                        // Check cave noise to determine if this voxel should be empty (cave)
                        if (surfaceHeight > y + 20 && y > 20)
                        {
                            double caveValue = GetCaveNoise(x + chunk.WorldPosition.X, y * 2, z + chunk.WorldPosition.Z);

                            if ((caveValue < 0.45 || caveValue > 0.6))
                                continue;
                        }

                        // Set the voxel as solid
                        chunk.SetVoxel(new(x, y, z), VoxelType.Solid);
                    }
            }

        GameManager.Instance.Generator.ChunksToBuild.Enqueue(chunk);
    }

    private int GetSurfaceHeight(int x, int z) =>
        (int)(Noise.OctavePerlin(x, 0, z, scale: 100) * 75) + 100;

    private double GetCaveNoise(int x, int y, int z) =>
        Noise.OctavePerlin(x, y, z, nOctaves: 4, scale: 50);

    private bool IsVoxelExposed(Chunk chunk, Vector3Byte localPosition)
    {
        Vector3Int neighbor = new();

        foreach (var direction in Vector3Int.Directions)
        {
            neighbor.X = direction.X + localPosition.X;
            neighbor.Y = direction.Y + localPosition.Y;
            neighbor.Z = direction.Z + localPosition.Z;

            // If the neighbor is not withing the bounds of the chunk
            if (neighbor.X <= 0 || neighbor.X >= Generator.BaseChunkSizeXZ + 1
             || neighbor.Z <= 0 || neighbor.Z >= Generator.BaseChunkSizeXZ + 1)
                continue;

            // If the neighbor is below ground, consider it as solid
            if (neighbor.Y < 0)
                return false;

            // If the neighbor is outside the world bounds, consider it as empty
            if (neighbor.Y > Generator.BaseChunkSizeY)
                return true;

            // If the neighbor voxel is empty, the current voxel is exposed
            if (!chunk.HasVoxel(new(neighbor.X, neighbor.Y, neighbor.Z)))
                return true;
        }

        // All neighbors are solid and the voxel is not exposed
        return false;
    }

    //private void RemoveUnexposedVoxels()
    //{
    //    Stack<Vector3Byte> unexposedVoxelsToRemove = new();
    //    // Check if the voxel is not exposed (has no neighbor that is empty)
    //    foreach (var voxel in chunk.VoxelData)
    //        if (!IsVoxelExposed(chunk, voxel.Key))
    //            unexposedVoxelsToRemove.Push(voxel.Key);

    //    foreach (var voxel in unexposedVoxelsToRemove)
    //        chunk.VoxelData.Remove(voxel);
    //}
}