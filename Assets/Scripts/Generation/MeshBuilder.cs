using System.Numerics;

using Engine.Components;
using Engine.DataStructures;

namespace VoxelSandbox;

public class MeshBuilder
{
    public void GenerateMesh(Chunk chunk)
    {
        int maxVoxels = chunk.VoxelData.Count;

        const int MaxFacesPerVoxel = 6;
        const int VerticesPerFace = 4;
        const int IndicesPerFace = 6;
        const int FloatsPerVertex = 2; // Position and Data

        int maxVertices = maxVoxels * MaxFacesPerVoxel * VerticesPerFace;
        int maxIndices = maxVoxels * MaxFacesPerVoxel * IndicesPerFace;
        int maxVertexFloats = maxVertices * FloatsPerVertex;

        // Preallocate arrays
        int[] indices = new int[maxIndices];
        float[] vertices = new float[maxVertexFloats];

        int vertexFloatCount = 0;
        int indexCount = 0;

        foreach (var voxel in chunk.VoxelData)
            if ((byte)voxel.Value > 0)
                // Add faces for each visible side of the voxel
                AddVoxelFaces(chunk, voxel, vertices, ref vertexFloatCount, indices, ref indexCount);

        var entity = GameManager.Instance.Entity.Manager.CreateEntity();
        entity.Transform.LocalPosition = chunk.WorldPosition.ToVector3();
        entity.Transform.LocalScale *= chunk.VoxelSize;

        chunk.Mesh = entity.AddComponent<Mesh>();
        chunk.Mesh.SetMeshData(indices, vertices, GetPositions(chunk), null, new InputLayoutHelper().AddUV());
        chunk.Mesh.SetMaterialTextures([new("TextureAtlasBig2.png", 0)]);
        chunk.Mesh.SetMaterialPipeline("VoxelShader");
    }

    private IEnumerable<float> ToFloats(List<VoxelVertex> voxelVertices)
    {
        foreach (var voxelVertex in voxelVertices)
        {
            yield return voxelVertex.Position;
            yield return voxelVertex.Data;
        }
    }

    private void AddVoxelFaces(Chunk chunk, KeyValuePair<Vector3Byte, VoxelType> voxel, float[] vertices, ref int vertexFloatCount, int[] indices, ref int indexCount)
    {
        // Check each face direction for visibility
        for (byte normalIndex = 0; normalIndex < Vector3Int.Directions.Length; normalIndex++)
        {
            Vector3Byte adjacentVoxelPosition = voxel.Key + Vector3Int.Directions[normalIndex];

            //Check if the adjacent voxel is an empty voxel
            if (!Chunk.IsWithinBounds(adjacentVoxelPosition))
                AddFace(voxel, normalIndex, vertices, ref vertexFloatCount, indices, ref indexCount);
            else if (chunk.GetVoxel(adjacentVoxelPosition, out var adjacentVoxel))
                if (adjacentVoxel is VoxelType.None)
                    AddFace(voxel, normalIndex, vertices, ref vertexFloatCount, indices, ref indexCount);
        }
    }

    private Vector3Int[] _faceVertices = new Vector3Int[4];
    private void AddFace(KeyValuePair<Vector3Byte, VoxelType> voxel, byte normalIndex, float[] vertices, ref int vertexFloatCount, int[] indices, ref int indexCount)
    {
        byte textureIndex = (byte)voxel.Value;
        byte lightIndex = 0;

        string enumName = voxel.Value.ToString();

        if (normalIndex == 0 && Enum.IsDefined(typeof(VoxelType), enumName + "_Top"))
            textureIndex = (byte)Enum.Parse<VoxelType>(enumName + "_Top");
        else if (normalIndex == 1 && Enum.IsDefined(typeof(VoxelType), enumName + "_Bottom"))
            textureIndex = (byte)Enum.Parse<VoxelType>(enumName + "_Bottom");
        else if (normalIndex == 2 && Enum.IsDefined(typeof(VoxelType), enumName + "_Front"))
            textureIndex = (byte)Enum.Parse<VoxelType>(enumName + "_Front");

        int x = voxel.Key.X;
        int y = voxel.Key.Y;
        int z = voxel.Key.Z;

        // Define face vertices
        switch (normalIndex)
        {
            case 0: // Top
                _faceVertices[0].Set(x + 0, y + 1, z + 0);
                _faceVertices[1].Set(x + 0, y + 1, z + 1);
                _faceVertices[2].Set(x + 1, y + 1, z + 1);
                _faceVertices[3].Set(x + 1, y + 1, z + 0);
                break;
            case 1: // Bottom
                _faceVertices[0].Set(x + 0, y + 0, z + 0);
                _faceVertices[1].Set(x + 1, y + 0, z + 0);
                _faceVertices[2].Set(x + 1, y + 0, z + 1);
                _faceVertices[3].Set(x + 0, y + 0, z + 1);
                break;
            case 2: // Front
                _faceVertices[0].Set(x + 1, y + 0, z + 1);
                _faceVertices[1].Set(x + 1, y + 1, z + 1);
                _faceVertices[2].Set(x + 0, y + 1, z + 1);
                _faceVertices[3].Set(x + 0, y + 0, z + 1);
                break;
            case 3: // Back
                _faceVertices[0].Set(x + 0, y + 0, z + 0);
                _faceVertices[1].Set(x + 0, y + 1, z + 0);
                _faceVertices[2].Set(x + 1, y + 1, z + 0);
                _faceVertices[3].Set(x + 1, y + 0, z + 0);
                break;
            case 4: // Right
                _faceVertices[0].Set(x + 1, y + 0, z + 0);
                _faceVertices[1].Set(x + 1, y + 1, z + 0);
                _faceVertices[2].Set(x + 1, y + 1, z + 1);
                _faceVertices[3].Set(x + 1, y + 0, z + 1);
                break;
            case 5: // Left
                _faceVertices[0].Set(x + 0, y + 0, z + 1);
                _faceVertices[1].Set(x + 0, y + 1, z + 1);
                _faceVertices[2].Set(x + 0, y + 1, z + 0);
                _faceVertices[3].Set(x + 0, y + 0, z + 0);
                break;
        }

        // Add vertices without 'new' keyword
        for (byte i = 0; i < 4; i++)
        {
            vertices[vertexFloatCount++] = VoxelData.PackVector3ToFloat(_faceVertices[i]);
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