using System.Numerics;

using Engine.Components;
using Engine.DataStructures;

namespace VoxelSandbox;

public class MeshBuilder
{
    public void GenerateMesh(Chunk chunk)
    {
        List<int> indices = new();
        List<VoxelVertex> vertices = new();
        List<Vector3> positions = [Vector3.Zero, chunk.GetChunkSize().ToVector3()];

        byte air = (byte)VoxelType.Air;

        foreach (var voxel in chunk.VoxelData)
            if ((byte)voxel.Value > air)
                // Add faces for each visible side of the voxel
                AddVoxelFaces(chunk, voxel, vertices, indices);

        var entity = GameManager.Instance.Entity.Manager.CreateEntity();
        entity.Transform.LocalPosition = chunk.WorldPosition.ToVector3();
        entity.Transform.LocalScale *= chunk.VoxelSize;

        chunk.Mesh = entity.AddComponent<Mesh>();
        chunk.Mesh.SetMeshData(indices, ToFloats(vertices).ToList(), positions, null, new InputLayoutDescriptionHelper().AddUV());
        chunk.Mesh.SetMaterialTextures([new("TextureAtlasBig2.png", 0)]);
        chunk.Mesh.SetMaterialPipeline("VoxelShader");
    }

    private IEnumerable<float> ToFloats(List<VoxelVertex> voxelVertices)
    {
        foreach (var voxelVertex in voxelVertices)
        {
            yield return voxelVertex.Data.X;
            yield return voxelVertex.Data.Y;
        }
    }

    private void AddVoxelFaces(Chunk chunk, KeyValuePair<Vector3Byte, VoxelType> voxel, List<VoxelVertex> vertices, List<int> indices)
    {
        // Check each face direction for visibility
        for (byte normalIndex = 0; normalIndex < Vector3Int.Directions.Length; normalIndex++)
        {
            Vector3Byte adjacentVoxelPosition = voxel.Key + Vector3Int.Directions[normalIndex];

            //Check if the adjacent voxel is an empty voxel
            if (!Chunk.IsWithinBounds(adjacentVoxelPosition))
                AddFace(voxel, normalIndex, vertices, indices);
            else if (chunk.GetVoxel(adjacentVoxelPosition, out var adjacentVoxel))
                if (adjacentVoxel is VoxelType.Air)
                    AddFace(voxel, normalIndex, vertices, indices);
        }
    }

    private void AddFace(KeyValuePair<Vector3Byte, VoxelType> voxel, byte normalIndex, List<VoxelVertex> vertices, List<int> indices)
    {
        Vector3Byte[] faceVertices = null;

        byte textureIndex = (byte)voxel.Value;
        byte lightIndex = 0;

        string enumName = voxel.Value.ToString();

        if (normalIndex == 0)
            if (Enum.IsDefined(typeof(VoxelType), enumName + "_Top"))
                textureIndex = (byte)Enum.Parse<VoxelType>(enumName + "_Top");

        if (normalIndex == 1)
            if (Enum.IsDefined(typeof(VoxelType), enumName + "_Bottom"))
                textureIndex = (byte)Enum.Parse<VoxelType>(enumName + "_Bottom");

        if (normalIndex == 0) // Top
            faceVertices =
            [
                new(voxel.Key.X,         voxel.Key.Y + 1,     voxel.Key.Z    ),
                new(voxel.Key.X,         voxel.Key.Y + 1,     voxel.Key.Z + 1),
                new(voxel.Key.X + 1,     voxel.Key.Y + 1,     voxel.Key.Z + 1),
                new(voxel.Key.X + 1,     voxel.Key.Y + 1,     voxel.Key.Z    ),
            ];
        else if (normalIndex == 1) // Bottom
            faceVertices =
            [
                new(voxel.Key.X,         voxel.Key.Y,         voxel.Key.Z    ),
                new(voxel.Key.X + 1,     voxel.Key.Y,         voxel.Key.Z    ),
                new(voxel.Key.X + 1,     voxel.Key.Y,         voxel.Key.Z + 1),
                new(voxel.Key.X,         voxel.Key.Y,         voxel.Key.Z + 1),
            ];
        else if (normalIndex == 2) // Right
            faceVertices =
            [
                new(voxel.Key.X + 1,     voxel.Key.Y,         voxel.Key.Z    ),
                new(voxel.Key.X + 1,     voxel.Key.Y + 1,     voxel.Key.Z    ),
                new(voxel.Key.X + 1,     voxel.Key.Y + 1,     voxel.Key.Z + 1),
                new(voxel.Key.X + 1,     voxel.Key.Y,         voxel.Key.Z + 1),
            ];
        else if (normalIndex == 3) // Left
                    faceVertices =
            [
                new(voxel.Key.X,         voxel.Key.Y,         voxel.Key.Z + 1),
                new(voxel.Key.X,         voxel.Key.Y + 1,     voxel.Key.Z + 1),
                new(voxel.Key.X,         voxel.Key.Y + 1,     voxel.Key.Z    ),
                new(voxel.Key.X,         voxel.Key.Y,         voxel.Key.Z    ),
            ];
        else if (normalIndex == 4) // Front
            faceVertices =
            [
                new(voxel.Key.X + 1,     voxel.Key.Y,         voxel.Key.Z + 1),
                new(voxel.Key.X + 1,     voxel.Key.Y + 1,     voxel.Key.Z + 1),
                new(voxel.Key.X,         voxel.Key.Y + 1,     voxel.Key.Z + 1),
                new(voxel.Key.X,         voxel.Key.Y,         voxel.Key.Z + 1),
            ];
        else if (normalIndex == 5) // Back
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
        vertices.Add(new VoxelVertex(new(VoxelData.PackVector3ToFloat((byte)faceVertices[0].X, (ushort)faceVertices[0].Y, (byte)faceVertices[0].Z), VoxelData.PackBytesToFloat(0, textureIndex, normalIndex, lightIndex))));
        vertices.Add(new VoxelVertex(new(VoxelData.PackVector3ToFloat((byte)faceVertices[1].X, (ushort)faceVertices[1].Y, (byte)faceVertices[1].Z), VoxelData.PackBytesToFloat(1, textureIndex, normalIndex, lightIndex))));
        vertices.Add(new VoxelVertex(new(VoxelData.PackVector3ToFloat((byte)faceVertices[2].X, (ushort)faceVertices[2].Y, (byte)faceVertices[2].Z), VoxelData.PackBytesToFloat(2, textureIndex, normalIndex, lightIndex))));
        vertices.Add(new VoxelVertex(new(VoxelData.PackVector3ToFloat((byte)faceVertices[3].X, (ushort)faceVertices[3].Y, (byte)faceVertices[3].Z), VoxelData.PackBytesToFloat(3, textureIndex, normalIndex, lightIndex))));

        // Add indices
        int startIndex = vertices.Count;
        indices.AddRange([startIndex, startIndex + 1, startIndex + 2, startIndex, startIndex + 2, startIndex + 3]);
    }
}