using System.Numerics;

public class NoiseSampler
{
    private const int ChunkSize = 32; // Size of the chunk

    private AVXPerlinNoise.Perlin _perlin = new();

    public void GenerateChunkContent(Chunk chunk)
    {
        Random rnd = new();


        for (int x = 0; x < chunk.Size; x++)
            for (int z = 0; z < chunk.Size; z++)
            {
                int nx = (int)(x + chunk.WorldPosition.X);
                int nz = (int)(z + chunk.WorldPosition.Z);

                float noiseValue = _perlin.OctavePerlin(nx, 0, nz);

                for (int y = 0; y < chunk.Size; y++)
                {
                    if (noiseValue * 8 + 4 > chunk.WorldPosition.Y + y)
                        chunk.SetVoxel(x, y, z, new Voxel { Type = (int)VoxelType.Solid });
                    else
                        chunk.SetVoxel(x, y, z, new Voxel { Type = (int)VoxelType.Air });
                }
            }
    }

    public Voxel GetVoxel(int x, int y, int z, Vector3 worldPosition)
    {
        int nx = (int)(x + worldPosition.X);
        int nz = (int)(z + worldPosition.Z);

        float noiseValue = _perlin.OctavePerlin(nx, 0, nz);

        if (noiseValue * 8 + 4 > worldPosition.Y + y)
            return new Voxel { Type = (int)VoxelType.Solid };

        return new Voxel { Type = (int)VoxelType.Air };
    }
}