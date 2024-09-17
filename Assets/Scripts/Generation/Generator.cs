using System.Numerics;

namespace VoxelSandbox;

public class Generator
{
    public const int BaseChunkSizeXZ = 32;
    public const int BaseChunkSizeY = 384;
    public readonly int[] LODSizes = { 32, 64, 128 };

    public Queue<Chunk> ChunksToGenerate = new();
    public Queue<Chunk> ChunksToBuild = new();

    // Sparse storage for chunks
    // Dictionary to store generated chunks by LOD level
    private Dictionary<int, Dictionary<Vector3Int, Chunk>> _generatedChunks = new();

    public void Initialize(Vector3 playerPosition)
    {
        // Initialize generatedChunks dictionary for all LOD levels
        for (int i = 0; i < LODSizes.Length; i++)
            _generatedChunks[i] = new();

        UpdateChunks(playerPosition);
    }

    public void UpdateChunks(Vector3 newPlayerPosition)
    {
        // Clear the list of chunks to be generated
        ChunksToGenerate.Clear();

        CalculateChunks(newPlayerPosition);
    }

    private void CalculateChunks(Vector3 worldPosition)
    {
        int lod = 0;
        int chunkSize = LODSizes[0];
        int nativeRadius = 20;

        // Calculate the center chunk position for the player
        Vector3Int centerChunkPos = new(
            (int)(worldPosition.X / BaseChunkSizeXZ) * BaseChunkSizeXZ,
            0,
            (int)(worldPosition.Z / BaseChunkSizeXZ) * BaseChunkSizeXZ);

        foreach (var chunk in _generatedChunks[lod].Values)
            chunk.Mesh.IsEnabled = false;

        for (int i = 0; i < nativeRadius; i++)
            for (int j = -i; j <= i; j++)
            {
                // Front
                CheckChunk(lod, new(
                    centerChunkPos.X + i * chunkSize,
                    0,
                    centerChunkPos.Z + j * chunkSize));

                // Back
                CheckChunk(lod, new(
                    centerChunkPos.X - (i + 1) * chunkSize,
                    0,
                    centerChunkPos.Z - (j - 1) * chunkSize));

                // Right
                CheckChunk(lod, new(
                    centerChunkPos.X + j * chunkSize,
                    0,
                    centerChunkPos.Z + (i + 1) * chunkSize));

                // Left
                CheckChunk(lod, new(
                    centerChunkPos.X - (j + 1) * chunkSize,
                    0,
                    centerChunkPos.Z - (i) * chunkSize));
            }
    }

    private void CheckChunk(int lod, Vector3Int chunkWorldPosition)
    {
        if (IsChunkGenerated(chunkWorldPosition, lod))
            _generatedChunks[0][chunkWorldPosition].Mesh.IsEnabled = true;
        else
        {
            Chunk newChunk = new(chunkWorldPosition, LODSizes[lod]);
            ChunksToGenerate.Enqueue(newChunk); // Add to the generation list if not generated
        }
    }

    private bool IsChunkGenerated(Vector3Int chunkPosition, int lodLevel) =>
        _generatedChunks[lodLevel].Keys.Contains(chunkPosition);
}
