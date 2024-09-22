using System.Numerics;

namespace VoxelSandbox;

public static class ExtensionMethods
{
    public static Vector3Int ToVector3Int(this Vector3 value) =>
        new((int)value.X, (int)value.Y, (int)value.Z);
    public static Vector3Byte ToVector3Byte(this Vector3 value) =>
        new((int)value.X, (int)value.Y, (int)value.Z);

    public static List<float> ToFloats(this List<VoxelVertex> vertices) =>
        vertices.SelectMany((VoxelVertex vertex) => vertex.position.ToFloats().Concat(vertex.uv.ToFloats())).ToList();

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