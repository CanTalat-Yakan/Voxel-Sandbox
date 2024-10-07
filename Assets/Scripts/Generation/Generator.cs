using System.Collections.Concurrent;

using Engine.Essentials;

namespace VoxelSandbox;

public sealed class Generator
{
    public static Dictionary<int, ConcurrentDictionary<Vector3Int, Chunk>> GeneratedChunks = new();

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

        UpdateChunks(new Vector3Int(0, 0, 0));
    }

    public static void SetVoxel(Vector3Int worldPosition)
    {
        GetChunkFromPosition(worldPosition, out var chunk, out var localVoxelPosition);

        chunk.SetVoxelType(ref localVoxelPosition, VoxelType.None);
        chunk.SetEmptyVoxel(ref localVoxelPosition);

        ChunksToBuild.Enqueue(chunk);
    }

    public static void GetChunkFromPosition(Vector3Int worldPosition, out Chunk chunk, out Vector3Short localVoxelPosition)
    {
        Vector3Int chunkPosition = new();

        // Use FloorDiv for correct chunk positioning
        chunkPosition.X = FloorDivision(worldPosition.X, ChunkSize) * ChunkSize;
        chunkPosition.Y = FloorDivision(worldPosition.Y, ChunkSize) * ChunkSize;
        chunkPosition.Z = FloorDivision(worldPosition.Z, ChunkSize) * ChunkSize;

        chunk = null;
        if (GeneratedChunks[0].ContainsKey(chunkPosition))
            chunk = GeneratedChunks[0][chunkPosition];

        localVoxelPosition = (worldPosition - chunkPosition).ToVector3Byte();
    }

    private static int FloorDivision(int a, int b) =>
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

        Func<int, int> currentLOD = i => i / NativeRadius;
        Func<int, int> multiplyerIfLOD0 = i => currentLOD(i) == 0 ? 1 : 0;
        Func<int, int> multiplyerIfLOD1Plus = i => 1 - multiplyerIfLOD0(i);

        Func<int, int> chunkSize = i => (int)Math.Pow(2, currentLOD(i)) * ChunkSize;
        Func<int, int> previousChunkSize = i => chunkSize(Math.Max(0, currentLOD(i) - 1));

        Func<int, int> chunkOffset = i => (multiplyerIfLOD1Plus(i) + 1) * Math.Min(1, currentLOD(i)) * (currentLOD(i) * chunkSize(i) + chunkSize(i));
        Func<int, int> originOffset = i => currentLOD(i) * NativeRadius * chunkSize(i) - chunkOffset(i);
        Func<int, int> lodOffset = i => previousChunkSize(i) * (int)Math.Pow(2, currentLOD(i));

        Func<int, int> currentLODStartLengthXZ = i => currentLOD(i) == 1 ? NativeRadius + 1 : NativeRadius + 1 + (int)Math.Pow(2, Math.Max(1, currentLOD(i) - 1)) + NativeRadius / 2 / currentLOD(i);
        Func<int, int> chunkCountXZ = i => currentLOD(i) == 0 ? i : i % NativeRadius + (currentLODStartLengthXZ(i) - 1) / 2;

        Vector3Int centerChunkPosition = new(
            worldPosition.X / (ChunkSize * 2) * ChunkSize * 2, 0,
            worldPosition.Z / (ChunkSize * 2) * ChunkSize * 2);

        for (int i = 0; i < combinedLODRadius; i++)
            for (int j = -chunkCountXZ(i); j <= chunkCountXZ(i); j++)
            {
                // Front
                CheckChunk(currentLOD(i), new(
                    centerChunkPosition.X + i * chunkSize(i) - originOffset(i), 0,
                    centerChunkPosition.Z + j * chunkSize(i) - lodOffset(i)));

                // Back
                CheckChunk(currentLOD(i), new(
                    centerChunkPosition.X - (i + 1) * chunkSize(i) + originOffset(i), 0,
                    centerChunkPosition.Z - (j - 1) * chunkSize(i) - lodOffset(i)));

                // Right
                CheckChunk(currentLOD(i), new(
                    centerChunkPosition.X + j * chunkSize(i), 0,
                    centerChunkPosition.Z + (i + 1) * chunkSize(i) - originOffset(i) - lodOffset(i)));

                // Left
                CheckChunk(currentLOD(i), new(
                    centerChunkPosition.X - (j + 1) * chunkSize(i), 0,
                    centerChunkPosition.Z - i * chunkSize(i) + originOffset(i) - lodOffset(i)));
            }
    }

    private void CheckChunk(int levelOfDetail, Vector3Int chunkWorldPosition)
    {
        if (GeneratedChunks[levelOfDetail].Keys.Contains(chunkWorldPosition))
        {
            //Chunk oldChunk = GeneratedChunks[levelOfDetail][chunkWorldPosition];
            //PoolManager.GetPool<Chunk>().Return(oldChunk.Reset());
            //GeneratedChunks[levelOfDetail].TryRemove(chunkWorldPosition, out _);
        }
        else
        {
            Chunk newChunk = PoolManager.GetPool<Chunk>().Get();
            newChunk.Initialize(GameManager, chunkWorldPosition, levelOfDetail);
            GeneratedChunks[levelOfDetail].TryAdd(chunkWorldPosition, newChunk);
            ChunksToGenerate.Enqueue(newChunk);
        }
    }
}