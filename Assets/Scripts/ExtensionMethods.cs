using System.Numerics;

namespace VoxelSandbox;

public static class ExtensionMethods
{
    public static Vector3Int ToVector3Int(this Vector3 value) =>
        new((int)value.X, (int)value.Y, (int)value.Z);
    public static Vector3Byte ToVector3Byte(this Vector3 value) =>
        new((int)value.X, (int)value.Y, (int)value.Z);

    public static float ToFloat(this Vector3Byte vector) =>
        Vector3Packer.PackVector3ToFloat((byte)vector.X, (ushort)vector.Y, (byte)vector.Z);

    private static IEnumerable<float> ToFloats(this Vector3 vector)
    {
        yield return vector.X;
        yield return vector.Y;
        yield return vector.Z;
    }

    private static IEnumerable<float> ToFloats(this Vector2 vector)
    {
        yield return vector.X;
        yield return vector.Y;
    }
}