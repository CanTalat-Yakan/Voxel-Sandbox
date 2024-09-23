using System.Numerics;

using Engine;
using Engine.Components;
using Engine.DataStructures;

namespace VoxelSandbox;

public class MeshBuilder
{
    private bool _initialized = false;
    private RootSignature _rootSignature;

    public void Initialize()
    {
        if (!_initialized)
        {
            _initialized = true;
            _rootSignature = Kernel.Instance.Context.CreateRootSignatureFromString("C");

            return;
        }

        Properties properties = new();

        properties.Normal =
            [
                Vector3Int.Top.ToVector3(),
                Vector3Int.Bottom.ToVector3(),
                Vector3Int.Right.ToVector3(),
                Vector3Int.Left.ToVector3(),
                Vector3Int.Front.ToVector3(),
                Vector3Int.Back.ToVector3()
            ];
        properties.Tangent =
            [
                Vector3Int.Right.ToVector3(),
                Vector3Int.Left.ToVector3(),
                Vector3Int.Front.ToVector3(),
                Vector3Int.Back.ToVector3(),
                Vector3Int.Top.ToVector3(),
                Vector3Int.Bottom.ToVector3()
            ];
        properties.TextureCoordinate =
            [
                Vector2.One,
                Vector2.UnitX,
                Vector2.Zero,
                Vector2.UnitY
            ];

        Kernel.Instance.Context.GraphicsContext.SetRootSignature(_rootSignature);

        Kernel.Instance.Context.UploadBuffer.Upload(properties, out var offset);
        Kernel.Instance.Context.UploadBuffer.SetConstantBufferView(offset, 2);
    }

    public void GenerateMesh(Chunk chunk)
    {
        List<int> indices = new();
        List<VoxelVertex> vertices = new();
        List<Vector3> positions = new();

        // Iterate through each voxel in the chunk
        foreach (var voxel in chunk.VoxelData)
        {
            if (chunk.GetVoxel(voxel.Key, out var voxelType))
                if ((voxelType is not VoxelType.None) && (voxelType is not VoxelType.Air))
                    // Add faces for each visible side of the voxel
                    AddVoxelFaces(chunk, chunk.VoxelSize, voxel.Key, voxel.Value, vertices, indices);

            positions.Add(voxel.Key.ToVector3() * chunk.VoxelSize);
        }

        var entity = GameManager.Instance.Entity.Manager.CreateEntity();
        entity.Transform.LocalPosition = chunk.WorldPosition.ToVector3();

        chunk.Mesh = entity.AddComponent<Mesh>();
        chunk.Mesh.SetMeshData(Kernel.Instance.Context.CreateMeshData(indices, vertices.ToFloats(), positions, inputLayoutElements: "PN"));
        chunk.Mesh.SetMaterialTextures([new("TextureAtlasBig2.png", 0)]);
        chunk.Mesh.SetMaterialPipeline("VoxelShader");
        //chunk.Mesh.Material.SetRootSignature("CCCs");
        //Initialize();
    }

    private void AddVoxelFaces(Chunk chunk, int voxelSize, Vector3Byte voxelPosition, VoxelType voxelType, List<VoxelVertex> vertices, List<int> indices)
    {
        // Check each face direction for visibility
        for (int i = 0; i < Vector3Int.Directions.Length; i++)
        {
            Vector3Int normal = Vector3Int.Directions[i];
            Vector3Int tangent = Vector3Int.OrthogonalDirections[i];

            Vector3Byte adjacentVoxelPosition = voxelPosition + normal;

            //Check if the adjacent voxel is an empty voxel
            if (!Chunk.IsWithinBounds(adjacentVoxelPosition))
                AddFace(voxelSize, voxelPosition, voxelType, normal, tangent, vertices, indices);
            else if (chunk.GetVoxel(adjacentVoxelPosition, out var adjacentVoxel))
                if (adjacentVoxel is VoxelType.Air)
                    AddFace(voxelSize, voxelPosition, voxelType, normal, tangent, vertices, indices);
        }
    }

    private void AddFace(int voxelSize, Vector3Byte voxelPosition, VoxelType voxelType, Vector3Int normal, Vector3Int tangent, List<VoxelVertex> vertices, List<int> indices)
    {
        var faceVertices = new Vector3[4];

        // Get the indexed texture coordinate of the atlas
        Vector2 atlasUV = TextureAtlas.GetTextureCoordinate((int)voxelType);
        float atlasTileSize = TextureAtlas.AtlasTileSize;
        string enumName = voxelType.ToString();
        byte normalIndex = 0;
        byte lightIndex = 0;

        // Compute the vertices of the face based on normal direction
        if (normal == Vector3Int.Top)
        {
            normalIndex = 0;

            faceVertices =
            [
                new Vector3(voxelPosition.X,         voxelPosition.Y + 1,     voxelPosition.Z    ) * voxelSize,
                new Vector3(voxelPosition.X,         voxelPosition.Y + 1,     voxelPosition.Z + 1) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y + 1,     voxelPosition.Z + 1) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y + 1,     voxelPosition.Z    ) * voxelSize,
            ];

            if (Enum.IsDefined(typeof(VoxelType), enumName + "_Top"))
                atlasUV = TextureAtlas.GetTextureCoordinate((int)(VoxelType)Enum.Parse(typeof(VoxelType), enumName + "_Top"));
        }
        else if (normal == Vector3Int.Bottom)
        {
            normalIndex = 1;

            faceVertices =
            [
                new Vector3(voxelPosition.X,         voxelPosition.Y,         voxelPosition.Z    ) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y,         voxelPosition.Z    ) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y,         voxelPosition.Z + 1) * voxelSize,
                new Vector3(voxelPosition.X,         voxelPosition.Y,         voxelPosition.Z + 1) * voxelSize,
            ];

            if (Enum.IsDefined(typeof(VoxelType), enumName + "_Bottom"))
                atlasUV = TextureAtlas.GetTextureCoordinate((int)(VoxelType)Enum.Parse(typeof(VoxelType), enumName + "_Bottom"));
        }
        else if (normal == Vector3Int.Right)
        {
            normalIndex = 2;

            faceVertices =
            [
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y,         voxelPosition.Z    ) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y + 1,     voxelPosition.Z    ) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y + 1,     voxelPosition.Z + 1) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y,         voxelPosition.Z + 1) * voxelSize,
            ];
        }
        else if (normal == Vector3Int.Left)
        {
            normalIndex = 3;

            faceVertices =
            [
                new Vector3(voxelPosition.X,         voxelPosition.Y,         voxelPosition.Z + 1) * voxelSize,
                new Vector3(voxelPosition.X,         voxelPosition.Y + 1,     voxelPosition.Z + 1) * voxelSize,
                new Vector3(voxelPosition.X,         voxelPosition.Y + 1,     voxelPosition.Z    ) * voxelSize,
                new Vector3(voxelPosition.X,         voxelPosition.Y,         voxelPosition.Z    ) * voxelSize,
            ];
        }
        else if (normal == Vector3Int.Front)
        {
            normalIndex = 4;

            faceVertices =
            [
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y,         voxelPosition.Z + 1) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y + 1,     voxelPosition.Z + 1) * voxelSize,
                new Vector3(voxelPosition.X,         voxelPosition.Y + 1,     voxelPosition.Z + 1) * voxelSize,
                new Vector3(voxelPosition.X,         voxelPosition.Y,         voxelPosition.Z + 1) * voxelSize,
            ];
        }
        else if (normal == Vector3Int.Back)
        {
            normalIndex = 5;

            faceVertices =
            [
                new Vector3(voxelPosition.X,         voxelPosition.Y,         voxelPosition.Z    ) * voxelSize,
                new Vector3(voxelPosition.X,         voxelPosition.Y + 1,     voxelPosition.Z    ) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y + 1,     voxelPosition.Z    ) * voxelSize,
                new Vector3(voxelPosition.X + 1,     voxelPosition.Y,         voxelPosition.Z    ) * voxelSize,
            ];
        }

        // Add vertices
        vertices.Add(new VoxelVertex(faceVertices[0], new(new PackData(normalIndex, lightIndex, 0).Encode(), 0, 0)));
        vertices.Add(new VoxelVertex(faceVertices[1], new(new PackData(normalIndex, lightIndex, 1).Encode(), 0, 0)));
        vertices.Add(new VoxelVertex(faceVertices[2], new(new PackData(normalIndex, lightIndex, 2).Encode(), 0, 0)));
        vertices.Add(new VoxelVertex(faceVertices[3], new(new PackData(normalIndex, lightIndex, 3).Encode(), 0, 0)));
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