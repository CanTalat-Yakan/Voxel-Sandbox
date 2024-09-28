using System.Numerics;

using Engine.Components;
using Engine.DataStructures;

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

        // Preallocate arrays
        var indices = new int[maxIndices / 4];
        var vertices = new float[maxVertexFloats / 4];

        int vertexFloatCount = 0;
        int indexCount = 0;

        foreach (var voxel in chunk.ExposedVoxelData)
            // Add faces for each visible side of the voxel
            AddVoxelFaces(chunk, voxel, chunk.GetVoxelType(voxel), ref vertices, ref vertexFloatCount, ref indices, ref indexCount);

        var entity = GameManager.Entity.Manager.CreateEntity();
        entity.Transform.LocalPosition = chunk.WorldPosition.ToVector3();
        entity.Transform.LocalScale *= chunk.VoxelSize;

        chunk.Mesh = entity.AddComponent<Mesh>();
        chunk.Mesh.SetMeshData(indices, vertices, GetPositions(chunk), new InputLayoutHelper().AddUV());
        chunk.Mesh.SetMaterialTextures([new("TextureAtlas.png", 0)]);
        chunk.Mesh.SetMaterialPipeline("VoxelShader");
    }

    private void AddVoxelFaces(Chunk chunk, Vector3Byte voxelPosition, VoxelType voxelType, ref float[] vertices, ref int vertexFloatCount, ref int[] indices, ref int indexCount)
    {
        if (vertexFloatCount + MaxFacesPerVoxel * VerticesPerFace * 2 >= vertices.Length)
            Array.Resize(ref vertices, vertices.Length + vertices.Length / 10);

        if (indexCount + MaxFacesPerVoxel * IndicesPerFace * 2 >= indices.Length)
            Array.Resize(ref indices, indices.Length + indices.Length / 10);

        var faceVertices = new Vector3Int[4];

        // Check each face direction for visibility
        for (byte normalIndex = 0; normalIndex < Vector3Int.Directions.Length; normalIndex++)
        {
            Vector3Byte adjacentVoxelPosition = voxelPosition + Vector3Int.Directions[normalIndex];

            // Check if the adjacent voxel is empty
            if (!chunk.IsWithinBounds(adjacentVoxelPosition) || chunk.IsVoxelEmpty(adjacentVoxelPosition))
                AddFace(voxelPosition, voxelType, normalIndex, ref vertices, ref vertexFloatCount, ref indices, ref indexCount, ref faceVertices);
        }
    }

    private void AddFace(Vector3Byte voxelPosition, VoxelType voxelType, byte normalIndex, ref float[] vertices, ref int vertexFloatCount, ref int[] indices, ref int indexCount, ref Vector3Int[] faceVertices)
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
        switch (normalIndex)
        {
            case 0: // Top
                faceVertices[0].Set(x + 0, y + 1, z + 0);
                faceVertices[1].Set(x + 0, y + 1, z + 1);
                faceVertices[2].Set(x + 1, y + 1, z + 1);
                faceVertices[3].Set(x + 1, y + 1, z + 0);
                break;
            case 1: // Bottom
                faceVertices[0].Set(x + 0, y + 0, z + 0);
                faceVertices[1].Set(x + 1, y + 0, z + 0);
                faceVertices[2].Set(x + 1, y + 0, z + 1);
                faceVertices[3].Set(x + 0, y + 0, z + 1);
                break;
            case 2: // Front
                faceVertices[0].Set(x + 1, y + 0, z + 1);
                faceVertices[1].Set(x + 1, y + 1, z + 1);
                faceVertices[2].Set(x + 0, y + 1, z + 1);
                faceVertices[3].Set(x + 0, y + 0, z + 1);
                break;
            case 3: // Back
                faceVertices[0].Set(x + 0, y + 0, z + 0);
                faceVertices[1].Set(x + 0, y + 1, z + 0);
                faceVertices[2].Set(x + 1, y + 1, z + 0);
                faceVertices[3].Set(x + 1, y + 0, z + 0);
                break;
            case 4: // Right
                faceVertices[0].Set(x + 1, y + 0, z + 0);
                faceVertices[1].Set(x + 1, y + 1, z + 0);
                faceVertices[2].Set(x + 1, y + 1, z + 1);
                faceVertices[3].Set(x + 1, y + 0, z + 1);
                break;
            case 5: // Left
                faceVertices[0].Set(x + 0, y + 0, z + 1);
                faceVertices[1].Set(x + 0, y + 1, z + 1);
                faceVertices[2].Set(x + 0, y + 1, z + 0);
                faceVertices[3].Set(x + 0, y + 0, z + 0);
                break;
        }

        // Add vertices without 'new' keyword
        for (byte i = 0; i < 4; i++)
        {
            vertices[vertexFloatCount++] = VoxelData.PackVector3ToFloat(faceVertices[i]);
            vertices[vertexFloatCount++] = VoxelData.PackBytesToFloat(i, textureIndex, normalIndex, lightIndex);
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