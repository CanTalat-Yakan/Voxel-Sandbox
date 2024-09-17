using AVXPerlinNoise;

namespace VoxelSandbox;

public class NoiseSampler
{
    private Perlin _perlin = new();

    public void GenerateChunkContent(Chunk chunk)
    {
        Random rnd = new();

        for (int x = 0; x <= Generator.BaseChunkSizeXZ + 1; x++)
            for (int z = 0; z <= Generator.BaseChunkSizeXZ + 1; z++)
            {
                int surfaceHeight = GetSurfaceHeight(x + chunk.WorldPosition.X, z + chunk.WorldPosition.Z);

                for (int y = 0; y < Generator.BaseChunkSizeY; y++)
                    if (surfaceHeight > chunk.WorldPosition.Y + y)
                        chunk.SetVoxel(new(x, y, z), VoxelType.Solid);
            }
    }

    public bool GetVoxel(Vector3Byte localPosition, Vector3Int worldPosition, out VoxelType voxelType)
    {
        voxelType = VoxelType.None;

        int surfaceHeight = GetSurfaceHeight(worldPosition.X, worldPosition.Z);
        if (surfaceHeight > worldPosition.Y + localPosition.Y)
        {
            voxelType = VoxelType.Solid;

            return true;
        }

        return false;
    }

    private int GetSurfaceHeight(int x, int z) =>
        (int)(_perlin.OctavePerlin(x, 0, z) * 8) + 4;
}