namespace VoxelSandbox;

public class Generator
{
    // Sparse storage for chunks
    // Dictionary to store generated chunks by LOD level
    public static Dictionary<int, Dictionary<Vector3Int, Chunk>> GeneratedChunks = new();

    public const int ChunkSizeXZ = 32;
    public const int ChunkSizeY = 384;
    public static readonly int[] LODSizesXZ = { ChunkSizeXZ, ChunkSizeXZ * 2, ChunkSizeXZ * 4 };
    public static readonly int[] LODSizesY = { ChunkSizeY, ChunkSizeY / 2, ChunkSizeY / 4 };

    public Queue<Chunk> ChunksToGenerate = new();
    public Queue<Chunk> ChunksToBuild = new();

    public void Initialize(Vector3Int playerPosition)
    {
        // Initialize generatedChunks dictionary for all LOD levels
        for (int i = 0; i < LODSizesXZ.Length; i++)
            GeneratedChunks[i] = new();

        UpdateChunks(playerPosition);
    }

    public static void GetChunkFromPosition(Vector3Int worldPosition, out Chunk chunk, out Vector3Byte localVoxelPosition)
    {
        Vector3Int chunkPosition = new Vector3Int();
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
    }

    private void CalculateChunks(Vector3Int worldPosition)
    {
        int nativeRadius = 4;
        int CombinedLODRadius = nativeRadius * LODSizesXZ.Length;

        Func<int, int> currentLOD = i => i / (nativeRadius);
        Func<int, int> chunkSize = i => LODSizesXZ[currentLOD(i)];
        Func<int, int> currentXZChunkCount = i => currentLOD(i) == 0 ? i : i / (2 * currentLOD(i));

        // Calculate the center chunk position for the player
        Vector3Int centerChunkPosition = new(
            worldPosition.X / ChunkSizeXZ * ChunkSizeXZ, 0,
            worldPosition.Z / ChunkSizeXZ * ChunkSizeXZ);

        //foreach (var LODChunks in GeneratedChunks.Values)
        //    foreach (var chunk in LODChunks.Values)
        //        chunk.Mesh.IsEnabled = false;

        for (int i = 0; i < CombinedLODRadius; i++)
            for (int j = -i; j <= currentXZChunkCount(i); j++)
            {
                // Front
                CheckChunk(currentLOD(i), new(
                    centerChunkPosition.X + (i) * chunkSize(i), 0,
                    centerChunkPosition.Z + (j) * chunkSize(i)));

                // Back
                CheckChunk(currentLOD(i), new(
                    centerChunkPosition.X - (i + 1) * chunkSize(i), 0,
                    centerChunkPosition.Z - (j - 1) * chunkSize(i)));

                // Right
                CheckChunk(currentLOD(i), new(
                    centerChunkPosition.X + (j     ) * chunkSize(i), 0,
                    centerChunkPosition.Z + (i + 1 ) * chunkSize(i)));

                // Left
                CheckChunk(currentLOD(i), new(
                    centerChunkPosition.X - (j + 1 ) * chunkSize(i), 0,
                    centerChunkPosition.Z - (i     ) * chunkSize(i)));
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