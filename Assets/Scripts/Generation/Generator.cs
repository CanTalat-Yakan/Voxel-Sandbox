﻿namespace VoxelSandbox;

public class Generator
{
    // Sparse storage for chunks
    // Dictionary to store generated chunks by LOD level
    public static Dictionary<int, Dictionary<Vector3Int, Chunk>> GeneratedChunks = new();

    public const int BaseChunkSizeXZ = 32;
    public const int BaseChunkSizeY = 384;
    public readonly int[] LODSizes = { 32, 64, 128 };

    public Queue<Chunk> ChunksToGenerate = new();
    public Queue<Chunk> ChunksToBuild = new();

    public void Initialize(Vector3Int playerPosition)
    {
        // Initialize generatedChunks dictionary for all LOD levels
        for (int i = 0; i < LODSizes.Length; i++)
            GeneratedChunks[i] = new();

        UpdateChunks(playerPosition);
    }

    public static void GetChunkFromPosition(Vector3Int playerPosition, out Chunk chunk, out Vector3Byte localVoxelPosition)
    {
        Vector3Int chunkPosition = new Vector3Int();
        chunkPosition.Y = 0;

        chunkPosition.X = playerPosition.X / BaseChunkSizeXZ * BaseChunkSizeXZ;
        if (playerPosition.X < 0 && (playerPosition.X % BaseChunkSizeXZ) != 0)
            chunkPosition.X -= BaseChunkSizeXZ;

        chunkPosition.Z = playerPosition.Z / BaseChunkSizeXZ * BaseChunkSizeXZ;
        if (playerPosition.Z < 0 && (playerPosition.Z % BaseChunkSizeXZ) != 0)
            chunkPosition.Z -= BaseChunkSizeXZ;

        chunk = null;
        if (GeneratedChunks[0].ContainsKey(chunkPosition))
            chunk = GeneratedChunks[0][chunkPosition];

        localVoxelPosition = (playerPosition - chunkPosition).ToVector3Byte();
    }

    public void UpdateChunks(Vector3Int newPlayerPosition)
    {
        ChunksToGenerate.Clear();
        ChunksToBuild.Clear();

        CalculateChunks(newPlayerPosition);
    }

    private void CalculateChunks(Vector3Int worldPosition)
    {
        int lod = 0;
        int chunkSize = LODSizes[0];
        int nativeRadius = 12;

        // Calculate the center chunk position for the player
        Vector3Int centerChunkPos = new(
            worldPosition.X / BaseChunkSizeXZ * BaseChunkSizeXZ,
            0,
            worldPosition.Z / BaseChunkSizeXZ * BaseChunkSizeXZ);

        foreach (var chunk in GeneratedChunks[lod].Values)
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
            GeneratedChunks[lod][chunkWorldPosition].Mesh.IsEnabled = true;
        else
        {
            Chunk newChunk = new(chunkWorldPosition, LODSizes[lod]);
            GeneratedChunks[lod].Add(chunkWorldPosition, newChunk);
            ChunksToGenerate.Enqueue(newChunk);
        }
    }

    private bool IsChunkGenerated(Vector3Int chunkPosition, int lodLevel) =>
        GeneratedChunks[lodLevel].Keys.Contains(chunkPosition);
}
