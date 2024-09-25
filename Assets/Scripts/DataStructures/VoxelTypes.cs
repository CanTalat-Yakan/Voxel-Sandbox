namespace VoxelSandbox;

public enum VoxelType : byte
{
    None,
    Stone,
    Grass,
    Grass_Top,
    Dirt,
    Sand,
    Sandstone,
    Sandstone_Top,
    Sandstone_Bottom,
    IronOre,
    DiamondOre,
    Log,
    Log_Top,
    Leaves,
}

public static class VoxelData
{
    public static IEnumerable<float> Pack(Vector3Int position, byte uv, byte tile, byte normal, byte light)
    {
        yield return PackVector3ToFloat(position);
        yield return PackBytesToFloat(uv, tile, normal, light);
    }

    // Pack a Vector3Byte with X and Z (8 bits each) and Y (16 bits) into a float
    public static float PackVector3ToFloat(Vector3Int position)
    {
        // Combine X (8 bits), Y (16 bits), and Z (8 bits) into a 32-bit integer
        uint packed = ((uint)position.X << 24) | ((uint)position.Y << 8) | (byte)position.Z;

        // Convert the packed integer into a float
        return BitConverter.ToSingle(BitConverter.GetBytes(packed), 0);
    }

    // Pack 4 bytes into a float
    public static float PackBytesToFloat(byte b1, byte b2, byte b3, byte b4)
    {
        // Combine the 4 bytes into a 32-bit integer
        uint packed = ((uint)b1 << 24) | ((uint)b2 << 16) | ((uint)b3 << 8) | b4;

        // Convert the packed integer into a float
        return BitConverter.ToSingle(BitConverter.GetBytes(packed), 0);
    }
}