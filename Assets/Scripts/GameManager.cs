using Engine;
using Engine.ECS;
using Engine.Essentials;
using Engine.Loader;
using Engine.Utilities;

namespace VoxelSandbox;

public sealed class GameManager : Component
{
    public static bool LOCKED { get; private set; } = false;

    public static readonly int Seed = 12345;

    public static readonly int LODCount = 1;
    public static readonly int NativeRadius = 8;

    public Generator Generator = new();

    public NoiseSampler NoiseSampler = new();
    public MeshBuilder MeshBuilder = new();

    private bool _processingChunkGeneration = false;

    public override void OnAwake()
    {
        ImageLoader.LoadFile(AssetPaths.ASSETS + "Textures\\TextureAtlas.png");
        Kernel.Instance.Context.CreateShader(AssetPaths.ASSETS + "Shaders\\VoxelShader");

        Entity.Manager.CreateEntity(name: "Controller").AddComponent<PlayerController>().Initialize(this);
        Entity.Manager.CreateEntity(name: "Sky").AddComponent<DefaultSky>().Initialize();

        Input.SetMouseLockState(MouseLockState.LockedInvisible, 0.5, 0.5);
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
        {
            LOCKED = !LOCKED;

            if (LOCKED)
                Input.SetMouseLockState(MouseLockState.Unlocked);
            else
                Input.SetMouseLockState(MouseLockState.LockedInvisible, 0.5, 0.5);
        }

        if (!LOCKED)
            Input.SetCursorIcon();
    }

    public void ChunkGenerationTask(Chunk chunk = null)
    {
        if (_processingChunkGeneration)
            return;

        if (Generator.ChunksToGenerate.IsEmpty && chunk is null)
            return;

        if (chunk is null)
            if (!Generator.ChunksToGenerate.TryDequeue(out chunk))
                return;

        _processingChunkGeneration = true;
        Task.Run(() => NoiseSampler.GenerateChunkContent(chunk, this));
        _processingChunkGeneration = false;
    }

    public void MeshBuildingTask(Chunk chunk = null)
    {
        if (_processingChunkGeneration)
            return;

        if (Generator.ChunksToBuild.IsEmpty && chunk is null)
            return;

        if (chunk is null)
            if (!Generator.ChunksToBuild.TryDequeue(out chunk))
                return;

        Task.Run(() => MeshBuilder.GenerateMesh(chunk));
    }
}