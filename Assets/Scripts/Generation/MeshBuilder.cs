using System.Numerics;

using Engine;
using Engine.Components;
using Engine.DataStructures;
using Engine.Helper;

namespace VoxelSandbox;

public class MeshBuilder
{
    public void GenerateMesh(Chunk chunk)
    {
        List<int> indices = new();
        List<Vertex> vertices = new();
        List<Vector3> positions = new();

        // Iterate through each voxel in the chunk
        foreach (var voxel in chunk.VoxelData)
        {
            // Skip border voxel
            if (!chunk.IsWithinBounds(voxel.Key))
                continue;

            // Add faces for each visible side of the voxel
            AddVoxelFaces(chunk, chunk.VoxelSize, voxel.Key, voxel.Value, vertices, indices);

            positions.Add(voxel.Key.ToVector3());
        }

        var entity = GameManager.Instance.Entity.Manager.CreateEntity();
        entity.Transform.LocalPosition = chunk.WorldPosition.ToVector3() - Vector3.UnitY * 64;

        chunk.Mesh = entity.AddComponent<Mesh>();
        chunk.Mesh.SetMeshData(Kernel.Instance.Context.CreateMeshData(indices, vertices.ToFloats(), positions));
        chunk.Mesh.SetMaterialTextures([new("TextureAtlasBig.png", 0)]);
        chunk.Mesh.SetMaterialPipeline("SimpleLit");
    }

    private void AddVoxelFaces(Chunk chunk, int voxelSize, Vector3Byte voxelPosition, VoxelType voxelType, List<Vertex> vertices, List<int> indices)
    {
        // Check each face direction for visibility
        for (int i = 0; i < Vector3Int.Directions.Length; i++)
        {
            Vector3Int normal = Vector3Int.Directions[i];
            Vector3Int tangent = Vector3Int.OrthogonalDirections[i];

            Vector3Byte adjacentPosition = voxelPosition + normal;

            bool removedUnexposedVoxels = true;
            if (removedUnexposedVoxels)
            {
                //Check if the adjacent voxel is an empty voxel
                if (chunk.GetVoxel(voxelPosition, out var voxelTypeCheck))
                    if ((voxelTypeCheck != VoxelType.None) && (voxelTypeCheck != VoxelType.Air))
                        if (chunk.GetVoxel(adjacentPosition, out var adjacentVoxelType))
                            if (adjacentVoxelType == VoxelType.None)
                                AddFace(voxelSize, voxelPosition, voxelType, normal, tangent, vertices, indices);
            }
            else if (!chunk.HasVoxel(adjacentPosition))
                AddFace(voxelSize, voxelPosition, voxelType, normal, tangent, vertices, indices);
        }
    }

    private void AddFace(int voxelSize, Vector3Byte voxelPosition, VoxelType voxelType, Vector3Int normal, Vector3Int tangent, List<Vertex> vertices, List<int> indices)
    {
        var faceVertices = new Vector3[4];

        // Compute the vertices of the face based on normal direction
        if (normal == Vector3Int.Top)
            faceVertices =
            [
                new Vector3(voxelPosition.X,         voxelPosition.Y + 1,     voxelPosition.Z    ) * voxelSize,
                new Vector3(voxelPosition.X,         voxelPosition.Y + 1,     voxelPosition.Z + 1) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y + 1,     voxelPosition.Z + 1) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y + 1,     voxelPosition.Z    ) * voxelSize,
            ];
        else if (normal == Vector3Int.Bottom)
            faceVertices =
            [
                new Vector3(voxelPosition.X,         voxelPosition.Y,         voxelPosition.Z    ) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y,         voxelPosition.Z    ) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y,         voxelPosition.Z + 1) * voxelSize,
                new Vector3(voxelPosition.X,         voxelPosition.Y,         voxelPosition.Z + 1) * voxelSize,
            ];
        else if (normal == Vector3Int.Right)
            faceVertices =
            [
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y,         voxelPosition.Z    ) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y + 1,     voxelPosition.Z    ) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y + 1,     voxelPosition.Z + 1) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y,         voxelPosition.Z + 1) * voxelSize,
            ];
        else if (normal == Vector3Int.Left)
            faceVertices =
            [
                new Vector3(voxelPosition.X,         voxelPosition.Y,         voxelPosition.Z + 1) * voxelSize,
                new Vector3(voxelPosition.X,         voxelPosition.Y + 1,     voxelPosition.Z + 1) * voxelSize,
                new Vector3(voxelPosition.X,         voxelPosition.Y + 1,     voxelPosition.Z    ) * voxelSize,
                new Vector3(voxelPosition.X,         voxelPosition.Y,         voxelPosition.Z    ) * voxelSize,
            ];
        else if (normal == Vector3Int.Front)
            faceVertices =
            [
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y,         voxelPosition.Z + 1) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y + 1,     voxelPosition.Z + 1) * voxelSize,
                new Vector3(voxelPosition.X,         voxelPosition.Y + 1,     voxelPosition.Z + 1) * voxelSize,
                new Vector3(voxelPosition.X,         voxelPosition.Y,         voxelPosition.Z + 1) * voxelSize,
            ];
        else if (normal == Vector3Int.Back)
            faceVertices =
            [
                new Vector3(voxelPosition.X,         voxelPosition.Y,         voxelPosition.Z    ) * voxelSize,
                new Vector3(voxelPosition.X,         voxelPosition.Y + 1,     voxelPosition.Z    ) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y + 1,     voxelPosition.Z    ) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y,         voxelPosition.Z    ) * voxelSize,
            ];

        // Get the indexed texture coordinate of the atlas texture array
        Vector2 atlasUV = TextureAtlas.GetTextureCoordinate((int)voxelType);
        float textureSize = TextureAtlas.TextureSize;

        // Add vertices
        vertices.Add(new Vertex(faceVertices[0], normal.ToVector3(), tangent.ToVector3(), Vector2.One * textureSize + atlasUV));
        vertices.Add(new Vertex(faceVertices[1], normal.ToVector3(), tangent.ToVector3(), Vector2.UnitX * textureSize + atlasUV));
        vertices.Add(new Vertex(faceVertices[2], normal.ToVector3(), tangent.ToVector3(), Vector2.Zero * textureSize + atlasUV));
        vertices.Add(new Vertex(faceVertices[3], normal.ToVector3(), tangent.ToVector3(), Vector2.UnitY * textureSize + atlasUV));

        // Add indices
        int startIndex = vertices.Count;
        indices.AddRange([startIndex, startIndex + 1, startIndex + 2, startIndex, startIndex + 2, startIndex + 3]);
    }
}