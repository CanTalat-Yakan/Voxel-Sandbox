namespace VoxelSandbox;

public enum VoxelType : ushort
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
    // Pack X, Y, Z (each 5 bits), vertexIndex (2 bits), normalIndex (3 bits), textureIndex (8 bits) and lightInfo (4 bits) into a 32-bit integer
    public static float PackFloat(byte x, byte y, byte z, byte vertexIndex, byte normalIndex, ushort textureIndex, byte indent)
    {
        uint packed = ((uint)indent << 31)          // 1 bit:  31
                    | ((uint)textureIndex << 23)    // 8 bits: 23-30
                    | ((uint)normalIndex << 20)     // 3 bits: 20-22
                    | ((uint)vertexIndex << 18)     // 2 bits: 18-19
                    | ((uint)z << 12)               // 6 bits: 12-17
                    | ((uint)y << 6)                // 6 bits: 6-11
                    | x;                            // 6 bits: 0-5
         
        // Convert the packed integer into a float
        return BitConverter.ToSingle(BitConverter.GetBytes(packed), 0);
    }
}