using System.Numerics;

using Engine;
using Engine.Components;

namespace VoxelSandbox;

public class MeshBuilder
{
    public void GenerateMesh(Chunk chunk)
    {
        List<int> indices = new();
        List<float> vertices = new();
        List<Vector3> positions = new();

        byte air = (byte)VoxelType.Air;

        foreach (var voxel in chunk.VoxelData)
            if ((byte)voxel.Value > air)
                // Add faces for each visible side of the voxel
                AddVoxelFaces(chunk, voxel, vertices, indices, positions);

        var entity = GameManager.Instance.Entity.Manager.CreateEntity();
        entity.Transform.LocalPosition = chunk.WorldPosition.ToVector3();
        entity.Transform.LocalScale *= chunk.VoxelSize;

        chunk.Mesh = entity.AddComponent<Mesh>();
        chunk.Mesh.SetMeshData(Kernel.Instance.Context.CreateMeshData(indices, vertices, positions, inputLayoutElements: "t"));
        chunk.Mesh.SetMaterialTextures([new("TextureAtlasBig2.png", 0)]);
        chunk.Mesh.SetMaterialPipeline("VoxelShader");
    }

    private void AddVoxelFaces(Chunk chunk, KeyValuePair<Vector3Byte, VoxelType> voxel, List<float> vertices, List<int> indices, List<Vector3> positions)
    {
        // Check each face direction for visibility
        for (int i = 0; i < Vector3Int.Directions.Length; i++)
        {
            Vector3Int normal = Vector3Int.Directions[i];
            Vector3Int tangent = Vector3Int.OrthogonalDirections[i];

            Vector3Byte adjacentVoxelPosition = voxel.Key + normal;

            //Check if the adjacent voxel is an empty voxel
            if (!Chunk.IsWithinBounds(adjacentVoxelPosition))
                AddFace(voxel, normal, tangent, vertices, indices);
            else if (chunk.GetVoxel(adjacentVoxelPosition, out var adjacentVoxel))
                if (adjacentVoxel is VoxelType.Air)
                    AddFace(voxel, normal, tangent, vertices, indices);
        }

        positions.Add(voxel.Key.ToVector3() * chunk.VoxelSize);
    }

    private void AddFace(KeyValuePair<Vector3Byte, VoxelType> voxel, Vector3Int normal, Vector3Int tangent, List<float> vertices, List<int> indices)
    {
        var faceVertices = new Vector3Byte[4];

        byte textureIndex = (byte)voxel.Value;
        byte normalIndex = (byte)Array.IndexOf(Vector3Int.Directions, normal); ;
        byte lightIndex = 0;

        string enumName = voxel.Value.ToString();

        if (normalIndex == 0)
            if (Enum.IsDefined(typeof(VoxelType), enumName + "_Top"))
                textureIndex = (byte)Enum.Parse<VoxelType>(enumName + "_Top");

        if (normalIndex == 1)
            if (Enum.IsDefined(typeof(VoxelType), enumName + "_Bottom"))
                textureIndex = (byte)Enum.Parse<VoxelType>(enumName + "_Bottom");

        // Compute the vertices of the face based on normal direction
        if (normal == Vector3Int.Top)
            faceVertices =
            [
                new(voxel.Key.X,         voxel.Key.Y + 1,     voxel.Key.Z    ),
                new(voxel.Key.X,         voxel.Key.Y + 1,     voxel.Key.Z + 1),
                new(voxel.Key.X + 1,     voxel.Key.Y + 1,     voxel.Key.Z + 1),
                new(voxel.Key.X + 1,     voxel.Key.Y + 1,     voxel.Key.Z    ),
            ];
        else if (normal == Vector3Int.Bottom)
            faceVertices =
            [
                new(voxel.Key.X,         voxel.Key.Y,         voxel.Key.Z    ),
                new(voxel.Key.X + 1,     voxel.Key.Y,         voxel.Key.Z    ),
                new(voxel.Key.X + 1,     voxel.Key.Y,         voxel.Key.Z + 1),
                new(voxel.Key.X,         voxel.Key.Y,         voxel.Key.Z + 1),
            ];
        else if (normal == Vector3Int.Right)
            faceVertices =
            [
                new(voxel.Key.X + 1,     voxel.Key.Y,         voxel.Key.Z    ),
                new(voxel.Key.X + 1,     voxel.Key.Y + 1,     voxel.Key.Z    ),
                new(voxel.Key.X + 1,     voxel.Key.Y + 1,     voxel.Key.Z + 1),
                new(voxel.Key.X + 1,     voxel.Key.Y,         voxel.Key.Z + 1),
            ];
        else if (normal == Vector3Int.Left)
            faceVertices =
            [
                new(voxel.Key.X,         voxel.Key.Y,         voxel.Key.Z + 1),
                new(voxel.Key.X,         voxel.Key.Y + 1,     voxel.Key.Z + 1),
                new(voxel.Key.X,         voxel.Key.Y + 1,     voxel.Key.Z    ),
                new(voxel.Key.X,         voxel.Key.Y,         voxel.Key.Z    ),
            ];
        else if (normal == Vector3Int.Front)
            faceVertices =
            [
                new(voxel.Key.X + 1,     voxel.Key.Y,         voxel.Key.Z + 1),
                new(voxel.Key.X + 1,     voxel.Key.Y + 1,     voxel.Key.Z + 1),
                new(voxel.Key.X,         voxel.Key.Y + 1,     voxel.Key.Z + 1),
                new(voxel.Key.X,         voxel.Key.Y,         voxel.Key.Z + 1),
            ];
        else if (normal == Vector3Int.Back)
            faceVertices =
            [
                new(voxel.Key.X,         voxel.Key.Y,         voxel.Key.Z),
                new(voxel.Key.X,         voxel.Key.Y + 1,     voxel.Key.Z),
                new(voxel.Key.X + 1,     voxel.Key.Y + 1,     voxel.Key.Z),
                new(voxel.Key.X + 1,     voxel.Key.Y,         voxel.Key.Z),
            ];

        // offset index for the air value;
        textureIndex--;

        // Add vertices
        vertices.AddRange([faceVertices[0].ToFloat(), Vector3Packer.PackBytesToFloat(0, textureIndex, normalIndex, lightIndex)]);
        vertices.AddRange([faceVertices[1].ToFloat(), Vector3Packer.PackBytesToFloat(1, textureIndex, normalIndex, lightIndex)]);
        vertices.AddRange([faceVertices[2].ToFloat(), Vector3Packer.PackBytesToFloat(2, textureIndex, normalIndex, lightIndex)]);
        vertices.AddRange([faceVertices[3].ToFloat(), Vector3Packer.PackBytesToFloat(3, textureIndex, normalIndex, lightIndex)]);

        // Add indices
        int startIndex = vertices.Count;
        indices.AddRange([startIndex, startIndex + 1, startIndex + 2, startIndex, startIndex + 2, startIndex + 3]);
    }
}