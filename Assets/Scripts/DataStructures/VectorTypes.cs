namespace VoxelSandbox;

public struct Vector3Short
{
    public ushort Data { get; set; }

    public int X => (Data >> 0) & 0b00011111;    // Bits 0–4 (5 bits)
    public int Y => (Data >> 5) & 0b00011111;    // Bits 5–9 (5 bits)
    public int Z => (Data >> 10) & 0b00011111;   // Bits 10–14 (5 bits)

    public static readonly Vector3Short Zero = new(0, 0, 0);
    public static readonly Vector3Short One = new(1, 1, 1);
    public static readonly Vector3Short UnitXZ = new(1, 0, 1);
    public static readonly Vector3Short UnitX = new(1, 0, 0);
    public static readonly Vector3Short UnitY = new(0, 1, 0);
    public static readonly Vector3Short UnitZ = new(0, 0, 1);

    public Vector3Short(int x, int y, int z)
    {
        if (x < 0 || x >= 32)
            throw new ArgumentOutOfRangeException(nameof(x), "X must be between 0 and 31.");
        if (y < 0 || y >= 32)
            throw new ArgumentOutOfRangeException(nameof(y), "Y must be between 0 and 31.");
        if (z < 0 || z >= 32)
            throw new ArgumentOutOfRangeException(nameof(z), "Z must be between 0 and 31.");

        Data = (ushort)(
            ((x & 0b00011111) << 0) |
            ((y & 0b00011111) << 5) |
            ((z & 0b00011111) << 10)
        );
    }

    public Vector3Short Set(int x, int y, int z)
    {
        Data = (ushort)(
            ((x & 0b00011111) << 0) |
            ((y & 0b00011111) << 5) |
            ((z & 0b00011111) << 10));

        return this;
    }

    public override string ToString() =>
        $"({X}, {Y}, {Z})";

    public System.Numerics.Vector3 ToVector3() =>
        new(X, Y, Z);

    public Vector3Int ToVector3Int() =>
        new(X, Y, Z);

    public override bool Equals(object obj)
    {
        if (obj is Vector3Short other)
            return Data == other.Data;

        return false;
    }

    public override int GetHashCode() =>
        Data.GetHashCode();

    // Operator overloads
    public static Vector3Short operator *(Vector3Short a, int b) =>
        a.Set(a.X * b, a.Y * b, a.Z * b);

    public static Vector3Short operator -(Vector3Short a, System.Numerics.Vector3 b) =>
        a.Set(a.X - (int)b.X, a.Y - (int)b.Y, a.Z - (int)b.Z);

    public static Vector3Short operator -(Vector3Short a, Vector3Int b) =>
        a.Set(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3Short operator +(Vector3Short a, Vector3Int b) =>
        a.Set(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3Short operator +(Vector3Short a, Vector3Short b) =>
        a.Set(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static bool operator ==(Vector3Short a, Vector3Short b) =>
        a.Data == b.Data;

    public static bool operator !=(Vector3Short a, Vector3Short b) =>
        a.Data != b.Data;
}


public struct Vector3Int
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public static readonly Vector3Int Zero = new(0, 0, 0);
    public static readonly Vector3Int One = new(1, 1, 1);
    public static readonly Vector3Int UnitX = new(1, 0, 0);
    public static readonly Vector3Int UnitY = new(0, 1, 0);
    public static readonly Vector3Int UnitZ = new(0, 0, 1);

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

    public Vector3Int Set(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;

        return this;
    }

    public Vector3Int Set(int? x = null, int? y = null, int? z = null)
    {
        if (x is not null)
            X = x.Value;
        if (y is not null)
            Y = y.Value;
        if (z is not null)
            Z = z.Value;

        return this;
    }

    public override string ToString() =>
        $"({X}, {Y}, {Z})";

    public static Vector3Int FromVector3(System.Numerics.Vector3 vector) =>
        new((int)vector.X, (int)vector.Y, (int)vector.Z);

    public System.Numerics.Vector3 ToVector3() =>
        new(X, Y, Z);

    public Vector3Short ToVector3Byte() =>
        new(X, Y, Z);

    public override bool Equals(object obj)
    {
        if (obj is Vector3Int other)
            return X == other.X && Y == other.Y && Z == other.Z;

        return false;
    }

    public override int GetHashCode()
    {
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

    public static Vector3Int operator +(Vector3Int a, int b) =>
        a.Set(a.X + b, a.Y + b, a.Z + b);

    public static Vector3Int operator +(Vector3Int a, Vector3Short b) =>
        a.Set(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3Int operator +(Vector3Int a, Vector3Int b) =>
        a.Set(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3Int operator -(Vector3Int a, Vector3Int b) =>
        a.Set(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static bool operator ==(Vector3Int a, Vector3Int b) =>
        a.X == b.X && a.Y == b.Y && a.Z == b.Z;

    public static bool operator !=(Vector3Int a, Vector3Int b) =>
        !(a == b);
}