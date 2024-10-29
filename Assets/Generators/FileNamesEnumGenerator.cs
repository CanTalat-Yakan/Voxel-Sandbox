namespace Project;

public enum ComputeShaderFiles
{
    [AssetPath(@"ComputeShaders/ChunkNoiseGenerator.hlsl")]
    ChunkNoiseGenerator,
}

public enum ScriptFiles
{
    [AssetPath(@"Scripts/GameManager.cs")]
    GameManager,
    [AssetPath(@"Scripts/DataStructures/VectorTypes.cs")]
    VectorTypes,
    [AssetPath(@"Scripts/DataStructures/VoxelTypes.cs")]
    VoxelTypes,
    [AssetPath(@"Scripts/Gameplay/CharacterController.cs")]
    CharacterController,
    [AssetPath(@"Scripts/Gameplay/PlayerMovement.cs")]
    PlayerMovement,
    [AssetPath(@"Scripts/Gameplay/RayCaster.cs")]
    RayCaster,
    [AssetPath(@"Scripts/Generation/Chunk.cs")]
    Chunk,
    [AssetPath(@"Scripts/Generation/Generator.cs")]
    Generator,
    [AssetPath(@"Scripts/Generation/MeshBuilder.cs")]
    MeshBuilder,
    [AssetPath(@"Scripts/Generation/NoiseSampler.cs")]
    NoiseSampler,
}

public enum ShaderFiles
{
    [AssetPath(@"Shaders/VoxelShader.hlsl")]
    VoxelShader,
}

public enum TextureFiles
{
    [AssetPath(@"Textures/Screenshot.png")]
    Screenshot,
    [AssetPath(@"Textures/TextureAtlas.png")]
    TextureAtlas,
}

// Custom attribute to store asset paths
[AttributeUsage(AttributeTargets.Field)]
public class AssetPathAttribute : Attribute
{
    public string Path { get; }

    public AssetPathAttribute(string path) =>
        Path = path;
}

// Extension method to get the path from any enum value
public static class AssetExtensions
{
    public static string GetPath(this Enum value)
    {
        var memberInfo = value.GetType().GetMember(value.ToString());
        if (memberInfo is not null && memberInfo.Length > 0)
        {
            var attributes = memberInfo[0].GetCustomAttributes(typeof(AssetPathAttribute), false);
            if (attributes is not null && attributes.Length > 0)
                return ((AssetPathAttribute)attributes[0]).Path;
        }

        throw new ArgumentException("Enum value does not have an AssetPath attribute.");
    }
}