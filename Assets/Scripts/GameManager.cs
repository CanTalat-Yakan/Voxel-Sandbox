using Engine;
using Engine.Components;
using Engine.ECS;
using Engine.Loader;
using Engine.Utilities;

namespace VoxelSandbox;

public sealed class GameManager : Component
{
    public Generator Generator = new();

    public NoiseSampler NoiseSampler = new();
    public MeshBuilder MeshBuilder = new();

    private bool _processingChunkGeneration = false;

    public override void OnAwake()
    {
        Input.SetLockMouse(true);
        Entity.Manager.ReturnEntity(Entity.Manager.GetEntityFromTag("DefaultBoot"));

        ImageLoader.LoadTexture(AssetsPaths.ASSETS + "Textures\\TextureAtlas.png");
        Kernel.Instance.Context.CreateShader(AssetsPaths.ASSETS + "Shaders\\VoxelShader");

        Camera.Main.Entity.Transform.SetPosition(y: 1100);

        Entity.Manager.CreateEntity(name: "Controller").AddComponent<PlayerController>().Initialize(this);
    }

    public override void OnStart() =>
        Generator.Initialize(this);

    public override void OnUpdate()
    {
        ChunkGenerationTask();
        MeshBuildingTask();
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