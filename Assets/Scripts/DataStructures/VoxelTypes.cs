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
    public static float PackFloat(byte x, byte y, byte z, byte vertexIndex, byte normalIndex, ushort textureIndex, byte lightIndex)
    {
        uint packed = ((uint)lightIndex << 28)    // 4 bits: bits 28-31
                    | ((uint)textureIndex << 20)   // 8 bits: bits 20-27
                    | ((uint)normalIndex << 17)    // 3 bits: bits 17-19
                    | ((uint)vertexIndex << 15)    // 2 bits: bits 15-16
                    | ((uint)z << 10)               // 5 bits: bits 10-14
                    | ((uint)y << 5)                // 5 bits: bits 5-9
                    | ((uint)x);                    // 5 bits: bits 0-4

        // Convert the packed integer into a float
        return BitConverter.ToSingle(BitConverter.GetBytes(packed), 0);
    }
}