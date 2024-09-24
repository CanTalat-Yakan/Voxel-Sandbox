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

        // Iterate through each voxel in the chunk
        foreach (var voxel in chunk.VoxelData)
        {
            if (chunk.GetVoxel(voxel.Key, out var voxelType))
                if ((voxelType is not VoxelType.None) && (voxelType is not VoxelType.Air))
                    // Add faces for each visible side of the voxel
                    AddVoxelFaces(chunk, voxel.Key, voxel.Value, vertices, indices);

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

    private void AddVoxelFaces(Chunk chunk, Vector3Byte voxelPosition, VoxelType voxelType, List<float> vertices, List<int> indices)
    {
        // Check each face direction for visibility
        for (int i = 0; i < Vector3Int.Directions.Length; i++)
        {
            Vector3Int normal = Vector3Int.Directions[i];
            Vector3Int tangent = Vector3Int.OrthogonalDirections[i];

            Vector3Byte adjacentVoxelPosition = voxelPosition + normal;

            //Check if the adjacent voxel is an empty voxel
            if (!Chunk.IsWithinBounds(adjacentVoxelPosition))
                AddFace(voxelPosition, voxelType, normal, tangent, vertices, indices);
            else if (chunk.GetVoxel(adjacentVoxelPosition, out var adjacentVoxel))
                if (adjacentVoxel is VoxelType.Air)
                    AddFace(voxelPosition, voxelType, normal, tangent, vertices, indices);
        }
    }

    private void AddFace(Vector3Byte voxelPosition, VoxelType voxelType, Vector3Int normal, Vector3Int tangent, List<float> vertices, List<int> indices)
    {
        var faceVertices = new Vector3[4];

        string enumName = voxelType.ToString();

        byte textureIndex = (byte)voxelType;
        byte normalIndex = 0;
        byte lightIndex = 0;

        // Compute the vertices of the face based on normal direction
        if (normal == Vector3Int.Top)
        {
            if (Enum.IsDefined(typeof(VoxelType), enumName + "_Top"))
                textureIndex = (byte)(VoxelType)Enum.Parse(typeof(VoxelType), enumName + "_Top");

            normalIndex = 0;

            faceVertices =
            [
                new Vector3(voxelPosition.X,         voxelPosition.Y + 1,     voxelPosition.Z    ),
                new Vector3(voxelPosition.X,         voxelPosition.Y + 1,     voxelPosition.Z + 1),
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y + 1,     voxelPosition.Z + 1),
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y + 1,     voxelPosition.Z    ),
            ];
        }
        else if (normal == Vector3Int.Bottom)
        {
            if (Enum.IsDefined(typeof(VoxelType), enumName + "_Bottom"))
                textureIndex = (byte)(VoxelType)Enum.Parse(typeof(VoxelType), enumName + "_Bottom");

            normalIndex = 1;

            faceVertices =
            [
                new Vector3(voxelPosition.X,         voxelPosition.Y,         voxelPosition.Z    ),
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y,         voxelPosition.Z    ),
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y,         voxelPosition.Z + 1),
                new Vector3(voxelPosition.X,         voxelPosition.Y,         voxelPosition.Z + 1),
            ];
        }
        else if (normal == Vector3Int.Right)
        {
            normalIndex = 2;

            faceVertices =
            [
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y,         voxelPosition.Z    ),
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y + 1,     voxelPosition.Z    ),
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y + 1,     voxelPosition.Z + 1),
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y,         voxelPosition.Z + 1),
            ];
        }
        else if (normal == Vector3Int.Left)
        {
            normalIndex = 3;

            faceVertices =
            [
                new Vector3(voxelPosition.X,         voxelPosition.Y,         voxelPosition.Z + 1),
                new Vector3(voxelPosition.X,         voxelPosition.Y + 1,     voxelPosition.Z + 1),
                new Vector3(voxelPosition.X,         voxelPosition.Y + 1,     voxelPosition.Z    ),
                new Vector3(voxelPosition.X,         voxelPosition.Y,         voxelPosition.Z    ),
            ];
        }
        else if (normal == Vector3Int.Front)
        {
            normalIndex = 4;

            faceVertices =
            [
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y,         voxelPosition.Z + 1),
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y + 1,     voxelPosition.Z + 1),
                new Vector3(voxelPosition.X,         voxelPosition.Y + 1,     voxelPosition.Z + 1),
                new Vector3(voxelPosition.X,         voxelPosition.Y,         voxelPosition.Z + 1),
            ];
        }
        else if (normal == Vector3Int.Back)
        {
            normalIndex = 5;

            faceVertices =
            [
                new Vector3(voxelPosition.X,         voxelPosition.Y,         voxelPosition.Z    ),
                new Vector3(voxelPosition.X,         voxelPosition.Y + 1,     voxelPosition.Z    ),
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y + 1,     voxelPosition.Z    ),
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y,         voxelPosition.Z    ),
            ];
        }

        // Add vertices
        vertices.AddRange([faceVertices[0].ToFloat(), Vector3Packer.PackBytesToFloat(0, textureIndex, normalIndex, lightIndex)]);
        vertices.AddRange([faceVertices[1].ToFloat(), Vector3Packer.PackBytesToFloat(1, textureIndex, normalIndex, lightIndex)]);
        vertices.AddRange([faceVertices[2].ToFloat(), Vector3Packer.PackBytesToFloat(2, textureIndex, normalIndex, lightIndex)]);
        vertices.AddRange([faceVertices[3].ToFloat(), Vector3Packer.PackBytesToFloat(3, textureIndex, normalIndex, lightIndex)]);

        //vertices.Add(new VoxelVertex(faceVertices[0], new(new PackData(normalIndex, lightIndex, 0).Encode(), 0, 0)));
        //vertices.Add(new VoxelVertex(faceVertices[1], new(new PackData(normalIndex, lightIndex, 1).Encode(), 0, 0)));
        //vertices.Add(new VoxelVertex(faceVertices[2], new(new PackData(normalIndex, lightIndex, 2).Encode(), 0, 0)));
        //vertices.Add(new VoxelVertex(faceVertices[3], new(new PackData(normalIndex, lightIndex, 3).Encode(), 0, 0)));
        //vertices.Add(new VoxelVertex(faceVertices[0], Vector2.One * atlasTileSize + atlasUV));
        //vertices.Add(new VoxelVertex(faceVertices[1], Vector2.UnitX * atlasTileSize + atlasUV));
        //vertices.Add(new VoxelVertex(faceVertices[2], Vector2.Zero * atlasTileSize + atlasUV));
        //vertices.Add(new VoxelVertex(faceVertices[3], Vector2.UnitY * atlasTileSize + atlasUV));
        //vertices.Add(new Vertex(faceVertices[0], normal.ToVector3(), tangent.ToVector3(), Vector2.One * atlasTileSize + atlasUV));
        //vertices.Add(new Vertex(faceVertices[1], normal.ToVector3(), tangent.ToVector3(), Vector2.UnitX * atlasTileSize + atlasUV));
        //vertices.Add(new Vertex(faceVertices[2], normal.ToVector3(), tangent.ToVector3(), Vector2.Zero * atlasTileSize + atlasUV));
        //vertices.Add(new Vertex(faceVertices[3], normal.ToVector3(), tangent.ToVector3(), Vector2.UnitY * atlasTileSize + atlasUV));

        // Add indices
        int startIndex = vertices.Count;
        indices.AddRange([startIndex, startIndex + 1, startIndex + 2, startIndex, startIndex + 2, startIndex + 3]);
    }
}