namespace VoxelSandbox;

public struct Vector2Byte
{
    public byte X;
    public byte Z;

    public Vector2Byte(int x, int z)
    {
        X = (byte)x;
        Z = (byte)z;
    }

    public override bool Equals(object obj)
    {
        if (obj is Vector2Byte other)
            return X == other.X && Z == other.Z;

        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + X.GetHashCode();
            hash = hash * 31 + Z.GetHashCode();
            return hash;
        }
    }


    public static bool operator ==(Vector2Byte a, Vector2Byte b) =>
        a.X == b.X && a.Z == b.Z;

    public static bool operator !=(Vector2Byte a, Vector2Byte b) =>
        !(a == b);
}

public struct Vector3Byte
{
    public byte Byte1; // X (7 bits) + lower Y bit (1 bit)
    public byte Byte2; // Middle Y bits (8 bits)
    public byte Byte3; // Z (7 bits) + upper Y bit (1 bit)

    // Properties to extract X, Y, Z from the bytes
    public int X => Byte1 & 0b01111111; // Bits 0–6 (7 bits)

    public int Y =>
          ((Byte1 >> 7) & 0b00000001)        // Bit 7 of byte1 (Y bit 0)
        | ((Byte2 & 0b11111111) << 1)        // Bits 0–7 of byte2 (Y bits 1–8)
        | ((Byte3 >> 7) & 0b00000001) << 9;  // Bit 7 of byte3 (Y bit 9)

    public int Z => Byte3 & 0b01111111; // Bits 0–6 (7 bits)

    public Vector3Byte(int x, int y, int z)
    {
        if (x < 0 || x >= 128)
            throw new ArgumentOutOfRangeException(nameof(x), "X must be between 0 and 127.");
        if (y < 0 || y >= 1024)
            throw new ArgumentOutOfRangeException(nameof(y), "Y must be between 0 and 1023.");
        if (z < 0 || z >= 128)
            throw new ArgumentOutOfRangeException(nameof(z), "Z must be between 0 and 127.");

        Byte1 = (byte)((x & 0b01111111) | ((y & 0b000000001) << 7)); // X and Y bit 0
        Byte2 = (byte)((y >> 1) & 0b11111111);                      // Y bits 1–8
        Byte3 = (byte)((z & 0b01111111) | ((y >> 9) & 0b00000001) << 7); // Z and Y bit 9
    }

    public override string ToString() =>
        $"({X}, {Y}, {Z})";

    public System.Numerics.Vector3 ToVector3() =>
        new(X, Y, Z);

    public Vector3Int ToVector3Int() =>
        new(X, Y, Z);
    
    public Vector2Byte ToVector2Byte() =>
        new(X, Z);

    public override bool Equals(object obj)
    {
        if (obj is Vector3Byte other)
            return Byte1 == other.Byte1 && Byte2 == other.Byte2 && Byte3 == other.Byte3;

        return false;
    }

    public override int GetHashCode()
    {
        // Combine hash codes of X, Y, and Z for a unique hash
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + Byte1.GetHashCode();
            hash = hash * 31 + Byte2.GetHashCode();
            hash = hash * 31 + Byte3.GetHashCode();
            return hash;
        }
    }

    public static Vector3Byte operator -(Vector3Byte a, System.Numerics.Vector3 b) =>
        new(a.X - (int)b.X, a.Y - (int)b.Y, a.Z - (int)b.Z);

    public static Vector3Byte operator -(Vector3Byte a, Vector3Int b) =>
        new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

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
    public static readonly Vector3Int Front = new(0, 0, 1);
    public static readonly Vector3Int Back = new(0, 0, -1);
    public static readonly Vector3Int Right = new(1, 0, 0);
    public static readonly Vector3Int Left = new(-1, 0, 0);

    public static readonly Vector3Int[] Directions =
    {
        Top,
        Bottom,
        Front,
        Back,
        Right,
        Left
    };

    public Vector3Int(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public void Set(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public override string ToString() =>
        $"({X}, {Y}, {Z})";

    public System.Numerics.Vector3 ToVector3() =>
        new(X, Y, Z);

    public Vector3Byte ToVector3Byte() =>
        new(X, Y, Z);

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

    public static Vector3Int operator *(Vector3Int a, int b)
    {
        a.X *= b;
        a.Y *= b;
        a.Z *= b;

        return a;
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