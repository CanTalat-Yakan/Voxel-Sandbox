using System.Diagnostics;
using System.Numerics;

using Engine.Helpers;
using Engine.Utilities;

namespace VoxelSandbox;

public sealed class MeshBuilder
{
    public const int MaxFacesPerVoxel = 6;
    public const int VerticesPerFace = 4;
    public const int IndicesPerFace = 6;

    private Stopwatch _stopwatch = new();

    public void GenerateMesh(Chunk chunk)
    {
        _stopwatch.Restart();

        int maxVoxels = chunk.ExposedVoxelPosition.Count;

        int maxVertices = maxVoxels * MaxFacesPerVoxel * VerticesPerFace;
        int maxIndices = maxVoxels * MaxFacesPerVoxel * IndicesPerFace;
        int compressionFactor = maxVertices > 1000 ? 2 : 1;

        // Preallocate arrays
        var vertices = new float[maxVertices / compressionFactor];
        var indices = new int[maxIndices / compressionFactor];

        int vertexFloatCount = 0;
        int indexCount = 0;

        foreach (var voxelPosition in chunk.ExposedVoxelPosition)
            // Add faces for each visible side of the voxel
            AddVoxelFaces(chunk, voxelPosition, chunk.GetVoxelType(voxelPosition), ref vertices, ref vertexFloatCount, ref indices, ref indexCount);

        if (vertexFloatCount == 0)
            return;

        chunk.Mesh.Entity.Transform.LocalPosition = chunk.WorldPosition.ToVector3();
        chunk.Mesh.Entity.Transform.LocalScale *= chunk.VoxelSize;

        chunk.Mesh.SetMeshData(vertices, indices, GetPositions(chunk), new InputLayoutHelper().AddFloat());
        chunk.Mesh.Order = 0;

        if (!chunk.MeshInitialized)
        {
            _ = Assets.PipelineStateObjects;
            chunk.Mesh.Material.SetRootSignature();
            chunk.Mesh.Material.SetTextures(Project.TextureFiles.TextureAtlas);
            chunk.Mesh.Material.SetPipeline(Project.ShaderFiles.VoxelShader);
            chunk.MeshInitialized = true;
        }

        chunk.IsChunkDirty = false;

        Output.Log($"MB: {_stopwatch.Elapsed.TotalMilliseconds * 1000:F0} µs");
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
            if (chunk.IsVoxelEmpty(ref adjacentVoxelPosition) && voxelType != VoxelType.None)
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
            textureIndex = (byte)(textureIndex + 1);
        else if (normalIndex == 1 && Enum.IsDefined(typeof(VoxelType), enumName + "_Bottom"))
            textureIndex = (byte)(textureIndex + 2);
        else if (normalIndex == 2 && Enum.IsDefined(typeof(VoxelType), enumName + "_Front"))
            textureIndex = (byte)(textureIndex + 3);

        // Calculate minus one times two to get from the range 1-30 to 0-29 to 0-58 for half blocks
        // The shader divides it by two and adds 1 again
        byte x = (byte)((voxelPosition.X - 1) * 2);
        byte y = (byte)((voxelPosition.Y - 1) * 2);
        byte z = (byte)((voxelPosition.Z - 1) * 2);

        // Define vertex offsets based on the normal index
        (byte offsetX, byte offsetY, byte offsetZ)[] vertexOffsets = normalIndex switch
        {
            0 => // Top
            [
                (0, 2, 0),
                (0, 2, 2),
                (2, 2, 2),
                (2, 2, 0)
            ],
            1 => // Bottom
            [
                (0, 0, 0),
                (2, 0, 0),
                (2, 0, 2),
                (0, 0, 2)
            ],
            2 => // Front
            [
                (2, 0, 2),
                (2, 2, 2),
                (0, 2, 2),
                (0, 0, 2)
            ],
            3 => // Back
            [
                (0, 0, 0),
                (0, 2, 0),
                (2, 2, 0),
                (2, 0, 0)
            ],
            4 => // Right
            [
                (2, 0, 0),
                (2, 2, 0),
                (2, 2, 2),
                (2, 0, 2)
            ],
            5 => // Left
            [
                (0, 0, 2),
                (0, 2, 2),
                (0, 2, 0),
                (0, 0, 0)
            ],
            _ => throw new ArgumentException("Invalid normal index")
        };

        // Add Vertices
        for (byte vertexIndex = 0; vertexIndex < VerticesPerFace; vertexIndex++)
        {
            byte vertexOffsetX = (byte)(x + vertexOffsets[vertexIndex].offsetX);
            byte vertexOffsetY = (byte)(y + vertexOffsets[vertexIndex].offsetY);
            byte vertexOffsetZ = (byte)(z + vertexOffsets[vertexIndex].offsetZ);

            vertices[vertexFloatCount++] = VoxelData.PackFloat(vertexOffsetX, vertexOffsetY, vertexOffsetZ, vertexIndex, normalIndex, textureIndex, lightIndex);
        }

        // Add indices
        int startIndex = vertexFloatCount - 4;

        indices[indexCount++] = startIndex;
        indices[indexCount++] = startIndex + 1;
        indices[indexCount++] = startIndex + 2;
        indices[indexCount++] = startIndex;
        indices[indexCount++] = startIndex + 2;
        indices[indexCount++] = startIndex + 3;
    }

    private Vector3[] GetPositions(Chunk chunk)
    {
        var chunkSize = chunk.GetChunkSize().ToVector3() + Vector3.One;

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