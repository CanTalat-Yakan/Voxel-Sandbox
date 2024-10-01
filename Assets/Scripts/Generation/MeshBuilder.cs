using System.Numerics;

using Engine.Components;
using Engine.DataStructures;
using Engine.Utilities;

namespace VoxelSandbox;

public sealed class MeshBuilder
{
    public const int MaxFacesPerVoxel = 6;
    public const int VerticesPerFace = 4;
    public const int IndicesPerFace = 6;
    public const int FloatsPerVertex = 2; // Position and Data

    public void GenerateMesh(Chunk chunk, GameManager GameManager)
    {
        int maxVoxels = chunk.ExposedVoxelData.Count;

        int maxVertices = maxVoxels * MaxFacesPerVoxel * VerticesPerFace;
        int maxIndices = maxVoxels * MaxFacesPerVoxel * IndicesPerFace;
        int maxVertexFloats = maxVertices * FloatsPerVertex;
        int compressionFactor = maxVertices > 1000 ? 2 : 1;

        // Preallocate arrays
        var vertices = new float[maxVertexFloats / compressionFactor];
        var indices = new int[maxIndices / compressionFactor];

        int vertexFloatCount = 0;
        int indexCount = 0;

        foreach (var voxel in chunk.ExposedVoxelData)
            // Add faces for each visible side of the voxel
            AddVoxelFaces(chunk, voxel, chunk.GetVoxelType(voxel), ref vertices, ref vertexFloatCount, ref indices, ref indexCount);

        if (vertexFloatCount == 0)
            return;

        chunk.Mesh.Entity.Transform.LocalPosition = chunk.WorldPosition.ToVector3();
        chunk.Mesh.Entity.Transform.LocalScale *= chunk.VoxelSize;

        chunk.Mesh.SetMeshData(indices, vertices, GetPositions(chunk), new InputLayoutHelper().AddUV());
        chunk.Mesh.SetMaterialTextures([new("TextureAtlas.png", 0)]);
        chunk.Mesh.SetMaterialPipeline("VoxelShader");
    }

    private void AddVoxelFaces(Chunk chunk, Vector3Short voxelPosition, VoxelType voxelType, ref float[] vertices, ref int vertexFloatCount, ref int[] indices, ref int indexCount)
    {
        ResizeArrays(ref vertices, ref vertexFloatCount, ref indices, ref indexCount);

        Vector3Short adjacentVoxelPosition = new();

        // Check each face direction for visibility
        for (byte normalIndex = 0; normalIndex < Vector3Int.Directions.Length; normalIndex++)
        {
            adjacentVoxelPosition.Set(
                (byte)(voxelPosition.X + Vector3Int.Directions[normalIndex].X),
                (byte)(voxelPosition.Y + Vector3Int.Directions[normalIndex].Y),
                (byte)(voxelPosition.Z + Vector3Int.Directions[normalIndex].Z));

            // Check if the adjacent voxel is empty
            if (!chunk.IsWithinBounds(adjacentVoxelPosition) || chunk.IsVoxelEmpty(adjacentVoxelPosition))
                AddFace(voxelPosition, voxelType, normalIndex, ref vertices, ref vertexFloatCount, ref indices, ref indexCount);
        }
    }

    private void ResizeArrays(ref float[] vertices, ref int vertexFloatCount, ref int[] indices, ref int indexCount)
    {
        if (vertexFloatCount + MaxFacesPerVoxel * VerticesPerFace * 2 >= vertices.Length)
        {
            Output.Log("Array Resized");
            Array.Resize(ref vertices, vertices.Length + vertices.Length / 10);
        }

        if (indexCount + MaxFacesPerVoxel * IndicesPerFace * 2 >= indices.Length)
        {
            Output.Log("Array Resized");
            Array.Resize(ref indices, indices.Length + indices.Length / 10);
        }
    }

    private void AddFace(Vector3Short voxelPosition, VoxelType voxelType, byte normalIndex, ref float[] vertices, ref int vertexFloatCount, ref int[] indices, ref int indexCount)
    {
        byte textureIndex = (byte)voxelType;
        byte lightIndex = 0;

        string enumName = voxelType.ToString();

        if (normalIndex == 0 && Enum.IsDefined(typeof(VoxelType), enumName + "_Top"))
            textureIndex = (byte)Enum.Parse<VoxelType>(enumName + "_Top");
        else if (normalIndex == 1 && Enum.IsDefined(typeof(VoxelType), enumName + "_Bottom"))
            textureIndex = (byte)Enum.Parse<VoxelType>(enumName + "_Bottom");
        else if (normalIndex == 2 && Enum.IsDefined(typeof(VoxelType), enumName + "_Front"))
            textureIndex = (byte)Enum.Parse<VoxelType>(enumName + "_Front");

        int x = voxelPosition.X;
        int y = voxelPosition.Y;
        int z = voxelPosition.Z;

        // Define face vertices
        Vector3Short[] faceVertices = normalIndex switch
        {
            0 => // Top
            [
                new(x    , y + 1, z    ),
                new(x    , y + 1, z + 1),
                new(x + 1, y + 1, z + 1),
                new(x + 1, y + 1, z    ),
            ],
            1 => // Bottom
            [
                new(x    , y    , z    ),
                new(x + 1, y    , z    ),
                new(x + 1, y    , z + 1),
                new(x    , y    , z + 1),
            ],
            2 => // Front
            [
                new(x + 1, y    , z + 1),
                new(x + 1, y + 1, z + 1),
                new(x    , y + 1, z + 1),
                new(x    , y    , z + 1),
            ],
            3 => // Back
            [
                new(x    , y    , z    ),
                new(x    , y + 1, z    ),
                new(x + 1, y + 1, z    ),
                new(x + 1, y    , z    ),
            ],
            4 => // Right
            [
                new(x + 1, y    , z    ),
                new(x + 1, y + 1, z    ),
                new(x + 1, y + 1, z + 1),
                new(x + 1, y    , z + 1),
            ],
            5 => // Left
            [
                new(x    , y    , z + 1),
                new(x    , y + 1, z + 1),
                new(x    , y + 1, z    ),
                new(x    , y    , z    ),
            ],
            _ => null
        };

        // Add Vertices
        for (byte i = 0; i < VerticesPerFace; i++)
        {
            vertices[vertexFloatCount++] = VoxelData.PackVector3ToFloat(faceVertices[i]); // 5 bits, 5 bits, 5 bits, Unused = 17 bits
            vertices[vertexFloatCount++] = VoxelData.PackBytesToFloat(i, textureIndex, normalIndex, lightIndex); // 2 bits, 8 bits, 3 bits, Unused = 19 bits 
            // Use 24 bits to represent color rgb. Put some in the first float and some in the second.
        }

        // Add indices
        int startIndex = (vertexFloatCount / 2) - 4; // Each vertex has 2 floats

        indices[indexCount++] = startIndex;
        indices[indexCount++] = startIndex + 1;
        indices[indexCount++] = startIndex + 2;
        indices[indexCount++] = startIndex;
        indices[indexCount++] = startIndex + 2;
        indices[indexCount++] = startIndex + 3;
    }

    private Vector3[] GetPositions(Chunk chunk)
    {
        var chunkSize = chunk.GetChunkSize().ToVector3();

        return
        [
            Vector3.Zero,
            Vector3.UnitX * chunkSize,
            Vector3.UnitZ * chunkSize,
            (Vector3.UnitX + Vector3.UnitZ) * chunkSize,

            Vector3.UnitY * chunkSize,
            (Vector3.UnitY + Vector3.UnitX) * chunkSize,
            (Vector3.UnitY + Vector3.UnitZ) * chunkSize,
            Vector3.One * chunkSize,
        ];
    }
}