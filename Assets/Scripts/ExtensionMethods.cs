using System.Numerics;

namespace VoxelSandbox;

public static class ExtensionMethods
{
    public static Vector3Int ToVector3Int(this Vector3 value) =>
        new((int)value.X, (int)value.Y, (int)value.Z);
    public static Vector3Byte ToVector3Byte(this Vector3 value) =>
        new((int)value.X, (int)value.Y, (int)value.Z);
}