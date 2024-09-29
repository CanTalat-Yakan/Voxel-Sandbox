using System.Collections.Concurrent;

namespace VoxelSandbox;

public sealed class Generator
{
    public static Dictionary<int, Dictionary<Vector3Int, Chunk>> GeneratedChunks = new();

    public const int ChunkSizeXZ = 32;
    public const int ChunkSizeY = 384;

    public static readonly int LODCount = 3;
    public static readonly int NativeRadius = 8;

    public ConcurrentQueue<Chunk> ChunksToGenerate = new();
    public ConcurrentQueue<Chunk> ChunksToBuild = new();

    public GameManager GameManager;

    public void Initialize(GameManager gameManager)
    {
        GameManager = gameManager;

        // Initialize generatedChunks dictionary for all LOD levels
        for (int i = 0; i < LODCount; i++)
            GeneratedChunks[i] = new();

        UpdateChunks(new Vector3Int(0, 0, 0));
    }

    public static void GetChunkFromPosition(Vector3Int worldPosition, out Chunk chunk, out Vector3Byte localVoxelPosition)
    {
        Vector3Int chunkPosition = new();
        chunkPosition.Y = 0;

        chunkPosition.X = worldPosition.X / ChunkSizeXZ * ChunkSizeXZ;
        if (worldPosition.X < 0 && (worldPosition.X % ChunkSizeXZ) != 0)
            chunkPosition.X -= ChunkSizeXZ;

        chunkPosition.Z = worldPosition.Z / ChunkSizeXZ * ChunkSizeXZ;
        if (worldPosition.Z < 0 && (worldPosition.Z % ChunkSizeXZ) != 0)
            chunkPosition.Z -= ChunkSizeXZ;

        chunk = null;
        if (GeneratedChunks[0].ContainsKey(chunkPosition))
            chunk = GeneratedChunks[0][chunkPosition];

        localVoxelPosition = (worldPosition - chunkPosition).ToVector3Byte();
    }

    public void UpdateChunks(Vector3Int newPlayerPosition)
    {
        ChunksToGenerate.Clear();
        ChunksToBuild.Clear();

        CalculateChunks(newPlayerPosition);

        GameManager.ChunkGenerationThread();
    }

    private void CalculateChunks(Vector3Int worldPosition)
    {
        int combinedLODRadius = NativeRadius * LODCount;

        Func<int, int> currentLOD = i => i / NativeRadius;
        Func<int, int> multiplyerIfLOD0 = i => currentLOD(i) == 0 ? 1 : 0;
        Func<int, int> multiplyerIfLOD1Plus = i => 1 - multiplyerIfLOD0(i);

        Func<int, int> chunkSize = i => (int)Math.Pow(2, currentLOD(i)) * ChunkSizeXZ;
        Func<int, int> previousChunkSize = i => chunkSize(Math.Max(0, currentLOD(i) - 1));

        Func<int, int> chunkOffset = i => (multiplyerIfLOD1Plus(i) + 1) * Math.Min(1, currentLOD(i)) * (currentLOD(i) * chunkSize(i) + chunkSize(i));
        Func<int, int> originOffset = i => currentLOD(i) * NativeRadius * chunkSize(i) - chunkOffset(i);
        Func<int, int> lodOffset = i => previousChunkSize(i) * (int)Math.Pow(2, currentLOD(i));

        Func<int, int> currentLODStartLengthXZ = i => currentLOD(i) == 1 ? NativeRadius + 1 : NativeRadius + 1 + (int)Math.Pow(2, Math.Max(1, currentLOD(i) - 1)) + NativeRadius / 2 / currentLOD(i);
        Func<int, int> chunkCountXZ = i => currentLOD(i) == 0 ? i : i % NativeRadius + (currentLODStartLengthXZ(i) - 1) / 2;

        // Calculate the center chunk position for the player
        Vector3Int centerChunkPosition = new(
            worldPosition.X / (ChunkSizeXZ * 2) * ChunkSizeXZ * 2, 0,
            worldPosition.Z / (ChunkSizeXZ * 2) * ChunkSizeXZ * 2);

        foreach (var LODChunks in GeneratedChunks.Values)
            foreach (var chunk in LODChunks.Values)
                chunk.Mesh.IsEnabled = false;

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
            GeneratedChunks[levelOfDetail][chunkWorldPosition].Mesh.IsEnabled = true;
        else
        {
            Chunk newChunk = new(chunkWorldPosition, levelOfDetail);
            GeneratedChunks[levelOfDetail].Add(chunkWorldPosition, newChunk);
            ChunksToGenerate.Enqueue(newChunk);
        }
    }
}