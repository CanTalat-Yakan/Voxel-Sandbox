using System.Numerics;

public enum VoxelType
{
    None,
    Solid
}

public struct Vector3Byte
{
    private byte _byte1; // X (6 bits) + lower Y bits (2 bits)
    private byte _byte2; // Middle Y bits (8 bits)
    private byte _byte3; // Z (6 bits) + upper Y bits (1 bit) + 1 unused bit

    public int X => _byte1 & 0b00111111; // Bits 0–5
    public int Y => ((_byte1 >> 6) & 0b00000011)       // Bits 6–7 of byte1 (Y bits 0–1)
                  | ((_byte2 & 0b11111111) << 2)       // Bits 0–7 of byte2 shifted to Y bits 2–9
                  | ((_byte3 >> 6) & 0b00000001) << 10;     // Bit 6 of byte3 (Y bit 10)
    public int Z => _byte3 & 0b00111111; // Bits 0–5

    public Vector3Byte(int x, int y, int z)
    {
        if (x < 0 || x >= 64 || y < 0 || y >= 512 || z < 0 || z >= 64)
            throw new ArgumentOutOfRangeException();

        _byte1 = (byte)((x & 0b00111111) | ((y & 0b00000011) << 6)); // X and lower Y bits
        _byte2 = (byte)((y >> 2) & 0b11111111);                      // Middle Y bits
        _byte3 = (byte)((z & 0b00111111) | ((y >> 10) << 6));        // Z and upper Y bit
    }

    public Vector3 ToVector3() =>
        new Vector3(X, Y, Z);

    public Vector3Int ToVector3Int() =>
        new(X, Y, Z);

    public override string ToString() =>
        $"({X}, {Y}, {Z})";

    public override bool Equals(object obj)
    {
        if (obj is Vector3Byte other)
            return X == other.X && Y == other.Y && Z == other.Z;

        return false;
    }

    public override int GetHashCode()
    {
        // Combine hash codes of X, Y, and Z for a unique hash
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + X.GetHashCode();
            hash = hash * 31 + Y.GetHashCode();
            hash = hash * 31 + Z.GetHashCode();
            return hash;
        }
    }

    public static Vector3Byte operator +(Vector3Byte a, Vector3Int b) =>
        new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3Byte operator +(Vector3Byte a, Vector3Byte b) =>
        new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    
    public static bool operator ==(Vector3Byte a, Vector3Byte b) =>
        a.X == b.X && a.Y == b.Y && a.Z == b.Z;

    public static bool operator !=(Vector3Byte a, Vector3Byte b) =>
        !(a == b);
}

public struct Vector3Int
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public static readonly Vector3Int Top = new(0, 1, 0);
    public static readonly Vector3Int Bottom = new(0, -1, 0);
    public static readonly Vector3Int Right = new(1, 0, 0);
    public static readonly Vector3Int Left = new(-1, 0, 0);
    public static readonly Vector3Int Front = new(0, 0, 1);
    public static readonly Vector3Int Back = new(0, 0, -1);

    public static readonly Vector3Int[] Directions =
    {
        Top,
        Bottom,
        Right,
        Left,
        Front,
        Back
    };

    public Vector3Int(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3 ToVector3() =>
        new(X, Y, Z);

    public Vector3Byte ToVector3Byte() =>
        new(X, Y, Z);

    public override string ToString() =>
        $"({X}, {Y}, {Z})";

    public override bool Equals(object obj)
    {
        if (obj is Vector3Int other)
            return X == other.X && Y == other.Y && Z == other.Z;

        return false;
    }

    public override int GetHashCode()
    {
        // Combine hash codes of X, Y, and Z for a unique hash
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + X.GetHashCode();
            hash = hash * 31 + Y.GetHashCode();
            hash = hash * 31 + Z.GetHashCode();
            return hash;
        }
    }

    public static Vector3Int operator /(Vector3Int a, int b)
    {
        a.X /= b;
        a.Y /= b;
        a.Z /= b;

        return a;
    }

    public static Vector3Int operator +(Vector3Int a, Vector3Byte b) =>
        new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3Int operator +(Vector3Int a, Vector3Int b) =>
        new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3Int operator -(Vector3Int a, Vector3Int b) =>
        new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static bool operator ==(Vector3Int a, Vector3Int b) =>
        a.X == b.X && a.Y == b.Y && a.Z == b.Z;

    public static bool operator !=(Vector3Int a, Vector3Int b) =>
        !(a == b);
}