using System.Numerics;

public class Generator
{
    public const int BaseChunkSize = 32; // Base size of the smallest chunk
    private readonly int[] LODSizes = { 32, 64, 128 }; // LOD sizes: 32, 64, 128

    private Dictionary<int, List<Chunk>> _generatedChunks = new(); // Dictionary to store generated chunks by LOD level
    private Queue<Chunk> _chunksToGenerate = new(); // List of chunks to be generated

    private Vector3 _playerPosition; // The current position of the player

    public void Initialize(Vector3 playerPosition)
    {
        _playerPosition = playerPosition;

        // Initialize generatedChunks dictionary for all LOD levels
        for (int i = 0; i < LODSizes.Length; i++)
            _generatedChunks[i] = new();

        UpdateChunks(playerPosition);
    }

    // Update chunks based on the player's position
    public void UpdateChunks(Vector3 newPlayerPosition)
    {
        _playerPosition = newPlayerPosition;

        // Clear the list of chunks to be generated
        _chunksToGenerate.Clear();

        CalculateNativeChunks(newPlayerPosition);

        // Calculate the chunks for each LOD
        //for (int lodLevel = 1; lodLevel < LODSizes.Length; lodLevel++)
        //    CalculateChunksForLod(newPlayerPosition, lodLevel);

        // Disable out-of-bounds chunks
        DisableOutOfBoundsChunks();
    }

    private void CheckChunk(Vector3 chunkPos, int lod = 0)
    {
        if (!IsChunkGenerated(chunkPos, lod))
        {
            Chunk newChunk = new(chunkPos, LODSizes[lod]);
            _chunksToGenerate.Enqueue(newChunk); // Add to the generation list if not generated
        }
        else
            _generatedChunks[0].FirstOrDefault(chunk => chunk.WorldPosition == chunkPos).Mesh.IsEnabled = true;
    }

    private void CalculateNativeChunks(Vector3 position)
    {
        int chunkSize = LODSizes[0];
        int nativeRadius = 10;

        // Calculate the center chunk position for the player
        Vector3 centerChunkPos = new(
            (int)(position.X / 32) * 32,
            (int)(position.Y / 32) * 32,
            (int)(position.Z / 32) * 32);

        for (int i = 0; i < nativeRadius; i++)
            for (int j = -i; j <= i; j++)
            {
                CheckChunk(new(
                    centerChunkPos.X + i * chunkSize,
                    0,
                    centerChunkPos.Z + j * chunkSize));
                CheckChunk(new(
                    centerChunkPos.X + j * chunkSize,
                    0,
                    centerChunkPos.Z + (i + 1) * chunkSize));

                CheckChunk(new(
                    centerChunkPos.X - (i + 1) * chunkSize,
                    0,
                    centerChunkPos.Z - (j - 1) * chunkSize));
                CheckChunk(new(
                    centerChunkPos.X - (j + 1) * chunkSize,
                    0,
                    centerChunkPos.Z - (i) * chunkSize));
            }
    }

    // Calculate the chunks for a specific LOD level
    private void CalculateChunksForLod(Vector3 position, int lodLevel)
    {
        int chunkSize = LODSizes[lodLevel];
        int lodRadius = (lodLevel + 1) * 2; // Radius for each LOD ring (e.g., 2, 4, 6)

        // Calculate the center chunk position for the player
        Vector3 centerChunkPos = new(
            (int)(position.X / chunkSize) * chunkSize,
            (int)(position.Y / chunkSize) * chunkSize,
            (int)(position.Z / chunkSize) * chunkSize);

        // Iterate through each chunk in the current LOD ring
        for (int x = -lodRadius; x <= lodRadius; x++)
            for (int z = -lodRadius; z <= lodRadius; z++)
                CheckChunk(new(
                    centerChunkPos.X + x * chunkSize,
                    0, // Assuming Y is always 0 for this example
                    centerChunkPos.Z + z * chunkSize));
    }

    // Check if a chunk is already generated
    private bool IsChunkGenerated(Vector3 chunkPos, int lodLevel) =>
        _generatedChunks[lodLevel].Exists(chunk => chunk.WorldPosition == chunkPos);

    // Disable chunks that are out of bounds
    private void DisableOutOfBoundsChunks()
    {
        foreach (var lod in _generatedChunks)
        {
            int lodLevel = lod.Key;

            foreach (var chunk in lod.Value)
                if (IsOutOfBounds(chunk.Position, lodLevel))
                    chunk.Mesh.IsEnabled = false;
        }
    }

    // Check if a chunk is out of the current LOD bounds
    private bool IsOutOfBounds(Vector3 chunkPos, int lodLevel)
    {
        int chunkSize = LODSizes[lodLevel];
        int maxLodRadius = (lodLevel + 1) * 2; // Maximum radius for LOD levels
        Vector3 centerChunkPos = new(
            (int)(_playerPosition.X / BaseChunkSize) * BaseChunkSize,
            0,
            (int)(_playerPosition.Z / BaseChunkSize) * BaseChunkSize);

        Vector3 offset = chunkPos - centerChunkPos;
        return Math.Abs(offset.X) > maxLodRadius * chunkSize ||
               Math.Abs(offset.Z) > maxLodRadius * chunkSize;
    }

    // Generate new chunks based on the to-be-generated list
    public void GenerateChunks()
    {
        foreach (var chunk in _chunksToGenerate)
        {
            int lodLevel = Array.IndexOf(LODSizes, chunk.Size); // Determine the LOD level based on chunk size

            if (lodLevel != -1 && !_generatedChunks[lodLevel].Contains(chunk))
            {
                _generatedChunks[lodLevel].Add(chunk);

                // Implement logic to visually generate or load the chunk in the game
            }
        }
    }

    // Retrieve the dictionary of generated chunks
    public Dictionary<int, List<Chunk>> GetGeneratedChunks() =>
        _generatedChunks;

    // Retrieve the list of chunks to be generated
    public Queue<Chunk> GetChunksToGenerate() =>
        _chunksToGenerate;
}
