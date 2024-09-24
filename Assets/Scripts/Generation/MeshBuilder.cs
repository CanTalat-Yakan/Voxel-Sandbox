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
        byte voxelTypeMax = (byte)VoxelType.Air;

        // Iterate through each voxel in the chunk
        foreach (var voxel in chunk.VoxelData)
        {
            if ((byte)voxel.Value > 0 && (byte)voxel.Value < voxelTypeMax)
                // Add faces for each visible side of the voxel
                AddVoxelFaces(chunk, voxel, vertices, indices);

            positions.Add(voxel.Key.ToVector3() * chunk.VoxelSize);
        }

        var entity = GameManager.Instance.Entity.Manager.CreateEntity();
        entity.Transform.LocalPosition = chunk.WorldPosition.ToVector3();
        entity.Transform.LocalScale *= chunk.VoxelSize;

        chunk.Mesh = entity.AddComponent<Mesh>();
        chunk.Mesh.SetMeshData(Kernel.Instance.Context.CreateMeshData(indices, vertices, positions, inputLayoutElements: "t"));
        chunk.Mesh.SetMaterialTextures([new("TextureAtlasBig2.png", 0)]);
        chunk.Mesh.SetMaterialPipeline("VoxelShader");
    }

    private void AddVoxelFaces(Chunk chunk, KeyValuePair<Vector3Byte, VoxelType> voxel, List<float> vertices, List<int> indices)
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
    }

    private void AddFace(KeyValuePair<Vector3Byte, VoxelType> voxel, Vector3Int normal, Vector3Int tangent, List<float> vertices, List<int> indices)
    {
        var faceVertices = new Vector3Byte[4];

        byte textureIndex = (byte)voxel.Value;
        byte normalIndex = 0;
        byte lightIndex = 0;

        string enumName = voxel.Value.ToString();

        // Compute the vertices of the face based on normal direction
        if (normal == Vector3Int.Top)
        {
            if (Enum.IsDefined(typeof(VoxelType), enumName + "_Top"))
                textureIndex = (byte)(VoxelType)Enum.Parse(typeof(VoxelType), enumName + "_Top");

            normalIndex = 0;

            faceVertices =
            [
                new(voxel.Key.X,         voxel.Key.Y + 1,     voxel.Key.Z    ),
                new(voxel.Key.X,         voxel.Key.Y + 1,     voxel.Key.Z + 1),
                new(voxel.Key.X + 1,     voxel.Key.Y + 1,     voxel.Key.Z + 1),
                new(voxel.Key.X + 1,     voxel.Key.Y + 1,     voxel.Key.Z    ),
            ];
        }
        else if (normal == Vector3Int.Bottom)
        {
            if (Enum.IsDefined(typeof(VoxelType), enumName + "_Bottom"))
                textureIndex = (byte)(VoxelType)Enum.Parse(typeof(VoxelType), enumName + "_Bottom");

            normalIndex = 1;

            faceVertices =
            [
                new(voxel.Key.X,         voxel.Key.Y,         voxel.Key.Z    ),
                new(voxel.Key.X + 1,     voxel.Key.Y,         voxel.Key.Z    ),
                new(voxel.Key.X + 1,     voxel.Key.Y,         voxel.Key.Z + 1),
                new(voxel.Key.X,         voxel.Key.Y,         voxel.Key.Z + 1),
            ];
        }
        else if (normal == Vector3Int.Right)
        {
            normalIndex = 2;

            faceVertices =
            [
                new(voxel.Key.X + 1,     voxel.Key.Y,         voxel.Key.Z    ),
                new(voxel.Key.X + 1,     voxel.Key.Y + 1,     voxel.Key.Z    ),
                new(voxel.Key.X + 1,     voxel.Key.Y + 1,     voxel.Key.Z + 1),
                new(voxel.Key.X + 1,     voxel.Key.Y,         voxel.Key.Z + 1),
            ];
        }
        else if (normal == Vector3Int.Left)
        {
            normalIndex = 3;

            faceVertices =
            [
                new(voxel.Key.X,         voxel.Key.Y,         voxel.Key.Z + 1),
                new(voxel.Key.X,         voxel.Key.Y + 1,     voxel.Key.Z + 1),
                new(voxel.Key.X,         voxel.Key.Y + 1,     voxel.Key.Z    ),
                new(voxel.Key.X,         voxel.Key.Y,         voxel.Key.Z    ),
            ];
        }
        else if (normal == Vector3Int.Front)
        {
            normalIndex = 4;

            faceVertices =
            [
                new(voxel.Key.X + 1,     voxel.Key.Y,         voxel.Key.Z + 1),
                new(voxel.Key.X + 1,     voxel.Key.Y + 1,     voxel.Key.Z + 1),
                new(voxel.Key.X,         voxel.Key.Y + 1,     voxel.Key.Z + 1),
                new(voxel.Key.X,         voxel.Key.Y,         voxel.Key.Z + 1),
            ];
        }
        else if (normal == Vector3Int.Back)
        {
            normalIndex = 5;

            faceVertices =
            [
                new(voxel.Key.X,         voxel.Key.Y,         voxel.Key.Z),
                new(voxel.Key.X,         voxel.Key.Y + 1,     voxel.Key.Z),
                new(voxel.Key.X + 1,     voxel.Key.Y + 1,     voxel.Key.Z),
                new(voxel.Key.X + 1,     voxel.Key.Y,         voxel.Key.Z),
            ];
        }

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