using System.Collections.Concurrent;

using Engine.Essentials;

namespace VoxelSandbox;

public sealed class Generator
{
    public static Dictionary<int, ConcurrentDictionary<(int, int), Chunk>> GeneratedChunks = new();

    public const int ChunkSize = 30;

    public static readonly int LODCount = 1;
    public static readonly int NativeRadius = 15;

    public static ConcurrentQueue<Chunk> ChunksToGenerate = new();
    public static ConcurrentQueue<Chunk> ChunksToBuild = new();

    public GameManager GameManager;

    public void Initialize(GameManager gameManager)
    {
        GameManager = gameManager;

        for (int i = 0; i < LODCount; i++)
            GeneratedChunks[i] = new();

        UpdateChunks(Vector3Int.Zero);
    }

    public static void SetVoxel(Vector3Int worldPosition, VoxelType voxelType)
    {
        GetChunkFromPosition(worldPosition, out var chunk, out var localVoxelPosition);

        chunk.SetVoxelType(ref localVoxelPosition, ref voxelType);
        chunk.SetEmptyVoxel(ref localVoxelPosition);

        ChunksToBuild.Enqueue(chunk);
    }

    public static void GetChunkFromPosition(Vector3Int worldPosition, out Chunk chunk, out Vector3Short localVoxelPosition)
    {
        localVoxelPosition = Vector3Short.Zero;

        int x = FloorDivision(worldPosition.X);
        int y = FloorDivision(worldPosition.Y);
        int z = FloorDivision(worldPosition.Z);

        chunk = null;
        if (GeneratedChunks[0].ContainsKey((x, z)))
            chunk = GeneratedChunks[0][(x, z)];
        else return;

        if (chunk.WorldPosition.Y == 0)
            return;

        int chunkYCount = y - FloorDivision(chunk.WorldPosition.Y);

        if (chunkYCount > 0)
            for (int i = 0; i < Math.Abs(chunkYCount); i++)
                if (chunk.TopChunk is not null)
                    chunk = chunk.TopChunk;
                else
                {
                    chunk = null;

                    return;
                }

        if (chunkYCount < 0)
            for (int i = 0; i < Math.Abs(chunkYCount); i++)
                if (chunk.BottomChunk is not null)
                    chunk = chunk.BottomChunk;
                else
                {
                    chunk = null;

                    return;
                }

        localVoxelPosition = new(
            worldPosition.X - x * ChunkSize, 
            worldPosition.Y - y * ChunkSize,
            worldPosition.Z - z * ChunkSize);
    }

    private static int FloorDivision(int a, int b = ChunkSize) =>
        a >= 0 ? a / b : (a - (b - 1)) / b;

    public void UpdateChunks(Vector3Int newPlayerPosition)
    {
        ChunksToGenerate.Clear();
        ChunksToBuild.Clear();

        CalculateChunks(newPlayerPosition);
    }

    private void CalculateChunks(Vector3Int worldPosition)
    {
        int combinedLODRadius = NativeRadius * LODCount;

        Vector3Int centerChunkPosition = new(
            worldPosition.X / (ChunkSize * 2) * ChunkSize * 2, 0,
            worldPosition.Z / (ChunkSize * 2) * ChunkSize * 2);

        for (int i = 0; i < combinedLODRadius; i++)
            for (int j = -i; j <= i; j++)
            {
                // Front
                CheckChunk(0,
                    centerChunkPosition.X + i,
                    centerChunkPosition.Z + j);

                // Back
                CheckChunk(0,
                    centerChunkPosition.X - i + 1,
                    centerChunkPosition.Z - j - 1);

                // Right
                CheckChunk(0,
                    centerChunkPosition.X + j,
                    centerChunkPosition.Z + i + 1);

                // Left
                CheckChunk(0,
                    centerChunkPosition.X - j + 1,
                    centerChunkPosition.Z - i);
            }
    }

    private void CheckChunk(int levelOfDetail, int x, int z)
    {
        if (GeneratedChunks[levelOfDetail].Keys.Contains((x, z)))
        {
            //Chunk oldChunk = GeneratedChunks[levelOfDetail][chunkWorldPosition];
            //PoolManager.GetPool<Chunk>().Return(oldChunk.Reset());
            //GeneratedChunks[levelOfDetail].TryRemove(chunkWorldPosition, out _);
        }
        else
        {
            Chunk newChunk = PoolManager.GetPool<Chunk>().Get();
            newChunk.Initialize(GameManager, levelOfDetail, x, z);
            GeneratedChunks[levelOfDetail].TryAdd((x, z), newChunk);
            ChunksToGenerate.Enqueue(newChunk);
        }
    }
}