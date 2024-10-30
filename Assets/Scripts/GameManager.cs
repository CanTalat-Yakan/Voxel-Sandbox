using Engine;
using Engine.ECS;
using Engine.Essentials;
using Engine.Interoperation;
using Engine.Loaders;
using Engine.Utilities;
using Project;

namespace VoxelSandbox;

public sealed class GameManager : Component
{
    public static bool PAUSED { get; private set; } = false;

    public static readonly int Seed = 12345;

    public static readonly int LODCount = 1;
    public static readonly int NativeRadius = 8;

    public Generator Generator = new();

    public NoiseSampler NoiseSampler = new();
    public MeshBuilder MeshBuilder = new();

    private bool _processingChunkGeneration = false;

    public override void OnAwake()
    {
        ImageLoader.LoadFile(Project.TextureFiles.TextureAtlas.GetFullPath());

        Kernel.Instance.Context.CreateShader(localPaths: Project.ShaderFiles.VoxelShader.GetPath());
        Kernel.Instance.Context.CreateComputeShader(localPaths: Project.ComputeShaderFiles.ChunkNoiseGenerator.GetPath());

        Entity.Manager.CreateEntity(name: "Controller").AddComponent<PlayerMovement>().Initialize(this);
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
            PAUSED = !PAUSED;

            if (PAUSED)
                Input.SetMouseLockState(MouseLockState.Unlocked);
            else
                Input.SetMouseLockState(MouseLockState.LockedInvisible, 0.5, 0.5);
        }

        if (!PAUSED)
            Input.SetCursorIcon(SystemCursor.IDC_CROSS);
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