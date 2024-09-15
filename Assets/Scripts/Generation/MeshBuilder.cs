using System.Numerics;

using Engine;
using Engine.Components;
using Engine.DataStructures;
using Engine.Helper;

public class MeshBuilder
{
    private int _voxelsize;

    // Define possible face directions
    Vector3[] _faceNormals =
    {
        Vector3.UnitY, // Top
        -Vector3.UnitY, // Bottom
        Vector3.UnitX, // Right
        -Vector3.UnitX, // Left
        Vector3.UnitZ, // Front
        -Vector3.UnitZ // Back
    };

    // Modular function to generate the mesh from a chunk
    public void GenerateMesh(Chunk chunk)
    {
        List<int> indices = new();
        List<Vertex> vertices = new();
        List<Vector3> positions = new();

        _voxelsize = chunk.VoxelSize;

        // Iterate through each voxel in the chunk
        for (int x = 0; x < chunk.Size; x += _voxelsize)
            for (int y = 0; y < chunk.Size; y += _voxelsize)
                for (int z = 0; z < chunk.Size; z += _voxelsize)
                {
                    Voxel voxel = chunk.GetVoxel(x, y, z);
                    Vector3 localPos = new Vector3(x, y, z);

                    // Skip empty or air voxels
                    if (voxel.Type == 0) continue;

                    // Add faces for each visible side of the voxel
                    AddVoxelFaces(chunk, localPos, voxel, vertices, indices);

                    positions.Add(new Vector3(x, y, z));
                }

        var entity = GameManager.Instance.Entity.Manager.CreateEntity();
        entity.Transform.LocalPosition = chunk.WorldPosition;
        chunk.Mesh = entity.AddComponent<Mesh>();

        chunk.Mesh.SetMeshData(Kernel.Instance.Context.CreateMeshData(indices, vertices.ToFloats(), positions));
        chunk.Mesh.SetMaterialTextures([new("Default.png", 0)]);
        chunk.Mesh.SetMaterialPipeline("SimpleLit");
    }

    // Adds voxel faces to the mesh
    private void AddVoxelFaces(Chunk chunk, Vector3 localPos, Voxel voxel, List<Vertex> vertices, List<int> indices)
    {
        // Check each face direction for visibility
        for (int i = 0; i < _faceNormals.Length; i++)
        {
            Vector3 normal = _faceNormals[i];
            Vector3 adjacentPos = localPos + normal;

            // Check if the adjacent voxel is within bounds and is an air voxel
            if (!chunk.IsWithinBounds(adjacentPos))
            {
                if (GameManager.Instance.NoiseSampler.GetVoxel((int)adjacentPos.X, (int)adjacentPos.Y, (int)adjacentPos.Z, chunk.WorldPosition).Type == (int)VoxelType.Air)
                    // Add the face to the mesh
                    AddFace(localPos, normal, vertices, indices);
            }
            else if (chunk.GetVoxel(adjacentPos).Type == (int)VoxelType.Air)
                // Add the face to the mesh
                AddFace(localPos, normal, vertices, indices);
        }
    }

    // Adds a single face to the mesh
    private void AddFace(Vector3 position, Vector3 normal, List<Vertex> vertices, List<int> indices)
    {
        // Define face vertices
        Vector3[] faceVertices = new Vector3[4];

        // Compute the vertices of the face based on normal direction
        if (normal == Vector3.UnitY) // Top face
        {
            faceVertices[0] = position + new Vector3(0, 1, 0) * _voxelsize;
            faceVertices[1] = position + new Vector3(0, 1, 1) * _voxelsize;
            faceVertices[2] = position + new Vector3(1, 1, 1) * _voxelsize;
            faceVertices[3] = position + new Vector3(1, 1, 0) * _voxelsize;
        }
        if (normal == -Vector3.UnitY) // Bottom face
        {
            faceVertices[0] = position + new Vector3(0, 0, 0) * _voxelsize;
            faceVertices[1] = position + new Vector3(1, 0, 0) * _voxelsize;
            faceVertices[2] = position + new Vector3(1, 0, 1) * _voxelsize;
            faceVertices[3] = position + new Vector3(0, 0, 1) * _voxelsize;
        }
        if (normal == Vector3.UnitX) // Right face
        {
            faceVertices[0] = position + new Vector3(1, 0, 0) * _voxelsize;
            faceVertices[1] = position + new Vector3(1, 1, 0) * _voxelsize;
            faceVertices[2] = position + new Vector3(1, 1, 1) * _voxelsize;
            faceVertices[3] = position + new Vector3(1, 0, 1) * _voxelsize;
        }
        if (normal == -Vector3.UnitX) // Left face
        {
            faceVertices[0] = position + new Vector3(0, 0, 1) * _voxelsize;
            faceVertices[1] = position + new Vector3(0, 1, 1) * _voxelsize;
            faceVertices[2] = position + new Vector3(0, 1, 0) * _voxelsize;
            faceVertices[3] = position + new Vector3(0, 0, 0) * _voxelsize;
        }
        if (normal == Vector3.UnitZ) // Front face
        {
            faceVertices[0] = position + new Vector3(1, 0, 1) * _voxelsize;
            faceVertices[1] = position + new Vector3(1, 1, 1) * _voxelsize;
            faceVertices[2] = position + new Vector3(0, 1, 1) * _voxelsize;
            faceVertices[3] = position + new Vector3(0, 0, 1) * _voxelsize;
        }
        if (normal == -Vector3.UnitZ) // Back face
        {
            faceVertices[0] = position + new Vector3(0, 0, 0) * _voxelsize;
            faceVertices[1] = position + new Vector3(0, 1, 0) * _voxelsize;
            faceVertices[2] = position + new Vector3(1, 1, 0) * _voxelsize;
            faceVertices[3] = position + new Vector3(1, 0, 0) * _voxelsize;
        }

        // Add vertices
        vertices.Add(new Vertex(faceVertices[0], normal, new(0, 0), Vector3.Zero));
        vertices.Add(new Vertex(faceVertices[1], normal, new(0, 1), Vector3.Zero));
        vertices.Add(new Vertex(faceVertices[2], normal, new(1, 1), Vector3.Zero));
        vertices.Add(new Vertex(faceVertices[3], normal, new(1, 0), Vector3.Zero));

        // Add indices
        int startIndex = vertices.Count;
        indices.AddRange([startIndex, startIndex + 1, startIndex + 2, startIndex, startIndex + 2, startIndex + 3]);
    }
}