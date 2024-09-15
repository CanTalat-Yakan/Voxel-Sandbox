using System.Numerics;

using Engine.Components;

public enum VoxelType
{
    Air,
    Solid
}

public struct Voxel(byte type, byte lightLevel)
{
    public byte Type = type;
    public byte LightLevel = lightLevel; // Light level (0-255)
}

public class Chunk
{
    public Mesh Mesh;

    public Vector3 WorldPosition { get; private set; } // Position of the chunk in world space
    public Vector3 Position => WorldPosition / Size; // Position of the chunk in world space divided by the chunk size
    public int Size { get; private set; } // Size of the chunk (32, 64, 128, etc.)
    public int VoxelSize => Size / Generator.BaseChunkSize;

    private byte[,,] _blockPositions; // 3D array for block positions
    private Voxel[,,] _voxelData; // 3D array for voxel properties

    public Chunk(Vector3 worldPosition, int chunkSize)
    {
        if ((chunkSize & (chunkSize - 1)) != 0 || chunkSize < 32)
            throw new ArgumentException("Size must be a power of 2 and at least 32.");

        WorldPosition = worldPosition;
        Size = chunkSize;

        // Initialize arrays
        _blockPositions = new byte[chunkSize, chunkSize, chunkSize];
        _voxelData = new Voxel[chunkSize, chunkSize, chunkSize];
    }

    // Convert local position to world position
    public Vector3 LocalToWorld(Vector3 localPos) =>
        WorldPosition + localPos;

    // Convert world position to local position
    public Vector3 WorldToLocal(Vector3 worldPos) =>
        worldPos - WorldPosition;

    // Get voxel at local position
    public Voxel GetVoxel(Vector3 localPos) =>
        GetVoxel((int)localPos.X, (int)localPos.Y, (int)localPos.Z);

    public Voxel GetVoxel(int localPosX, int localPosY, int localPosZ)
    {
        if (IsWithinBounds(localPosX, localPosY, localPosZ))
            return _voxelData[localPosX, localPosY, localPosZ];
        else
            throw new IndexOutOfRangeException("Local position is out of chunk bounds.");
    }

    // Set voxel at local position
    public void SetVoxel(int localPosX, int localPosY, int localPosZ, Voxel voxel)
    {
        if (IsWithinBounds(localPosX, localPosY, localPosZ))
        {
            _voxelData[localPosX, localPosY, localPosZ] = voxel;
            _blockPositions[localPosX, localPosY, localPosZ] = 1; // Mark as occupied
        }
        else
            throw new IndexOutOfRangeException("Local position is out of chunk bounds.");
    }

    // Check if a position is within chunk bounds
    public bool IsWithinBounds(Vector3 pos) =>
        IsWithinBounds((int)pos.X, (int)pos.Y, (int)pos.Z);

    public bool IsWithinBounds(int posX, int posY, int posZ) =>
        posX >= 0 && posX < Size && posY >= 0 && posY < Size && posZ >= 0 && posZ < Size;
}