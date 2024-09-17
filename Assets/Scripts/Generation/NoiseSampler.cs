using AVXPerlinNoise;

namespace VoxelSandbox;

public class NoiseSampler
{
    public Perlin Noise = new();

    public void GenerateChunkContent(Chunk chunk)
    {
        for (int x = 0; x <= Generator.BaseChunkSizeXZ + 1; x++)
            for (int z = 0; z <= Generator.BaseChunkSizeXZ + 1; z++)
            {
                int surfaceHeight = GetSurfaceHeight(x + chunk.WorldPosition.X, z + chunk.WorldPosition.Z);

                for (int y = 0; y < Generator.BaseChunkSizeY; y++)
                    if (surfaceHeight > chunk.WorldPosition.Y + y)
                        chunk.SetVoxel(new(x, y, z), VoxelType.Solid);
            }

        Stack<Vector3Byte> unexposedVoxelsToRemove = new();
        // Check if the voxel is not exposed (has no neighbor that is empty)
        foreach (var voxel in chunk.VoxelData)
            if (!IsVoxelExposed(chunk, voxel.Key))
                unexposedVoxelsToRemove.Push(voxel.Key);

        foreach (var voxel in unexposedVoxelsToRemove)
            chunk.VoxelData.Remove(voxel);

        GameManager.Instance.Generator.ChunksToBuild.Enqueue(chunk);
    }

    private int GetSurfaceHeight(int x, int z) =>
        (int)(Noise.OctavePerlin(x, 0, z, scale: 100) * 75);

    private bool IsVoxelExposed(Chunk chunk, Vector3Byte localPosition)
    {
        foreach (var direction in Vector3Int.Directions)
        {
            Vector3Int neighbor = direction + localPosition;

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
            if (!chunk.HasVoxel(neighbor.ToVector3Byte()))
                return true;
        }

        // All neighbors are solid and the voxel is not exposed
        return false;
    }
}