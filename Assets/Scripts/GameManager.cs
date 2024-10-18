using Engine;
using Engine.ECS;
using Engine.Loader;
using Engine.Utilities;

namespace VoxelSandbox;

public sealed class GameManager : Component
{
    public static bool LOCKED { get; private set; } = false;

    public Generator Generator = new();

    public NoiseSampler NoiseSampler = new();
    public MeshBuilder MeshBuilder = new();

    private bool _processingChunkGeneration = false;

    public override void OnAwake()
    {
        ImageLoader.LoadTexture(AssetsPaths.ASSETS + "Textures\\TextureAtlas.png");
        Kernel.Instance.Context.CreateShader(AssetsPaths.ASSETS + "Shaders\\VoxelShader");

        Entity.Manager.CreateEntity(name: "Controller").AddComponent<PlayerController>().Initialize(this);
    }

    public override void OnStart() =>
        Generator.Initialize(this);

    public override void OnUpdate()
    {
        ChunkGenerationTask();
        MeshBuildingTask();
    }

    public override void OnLateUpdate()
    {
        if (Input.GetKey(Key.Escape, InputState.Down))
            LOCKED = !LOCKED;

        Input.SetMouseLockState(!LOCKED);
        if (!LOCKED)
            Input.SetMouseRelativePosition(0.5f, 0.5f);
    }

    public void ChunkGenerationTask(Chunk chunk = null)
    {
        if (_processingChunkGeneration || Generator.ChunksToGenerate.IsEmpty)
            return;

        if (chunk is null)
            if (!Generator.ChunksToGenerate.TryDequeue(out chunk))
                return;

        _processingChunkGeneration = true;
        Task.Run(() => { NoiseSampler.GenerateChunkContent(chunk, this); });
        _processingChunkGeneration = false;
    }

    public void MeshBuildingTask(Chunk chunk = null)
    {
        if (_processingChunkGeneration || Generator.ChunksToBuild.IsEmpty)
            return;

        if (chunk is null)
            if (!Generator.ChunksToBuild.TryDequeue(out chunk))
                return;

        Task.Run(() => { MeshBuilder.GenerateMesh(chunk); });
    }
}