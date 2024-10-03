using System.Diagnostics;

using Engine;
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
        ImageLoader.LoadTexture(AssetsPaths.ASSETS + "Textures\\TextureAtlas.png");
        Kernel.Instance.Context.CreateShader(AssetsPaths.ASSETS + "Shaders\\VoxelShader");

        //PlayerController.Initialize(this);
    }

    public override void OnStart() =>
        Generator.Initialize(this);

    public override void OnUpdate()
    {
        if (Generator.ChunksToGenerate.TryDequeue(out var chunk))
            ChunkGenerationTask(chunk);
        MeshBuildingTask();
    }

    public void ChunkGenerationTask(Chunk chunk)
    {
        Stopwatch stopwatch = new();

        Task.Run(() =>
        {
            stopwatch.Start();

            NoiseSampler.GenerateChunkContent(chunk, this);

            Output.Log($"CG: {GetFormattedTime(stopwatch)}");
        });
    }

    public void MeshBuildingTask(Chunk chunk = null)
    {
        if (_processingChunkGeneration || Generator.ChunksToBuild.IsEmpty)
            return;

        if (chunk is null)
            if (!Generator.ChunksToBuild.TryDequeue(out chunk))
                return;

        Stopwatch stopwatch = new();

        Task.Run(() =>
        {
            stopwatch.Start();

            MeshBuilder.GenerateMesh(chunk, this);

            Output.Log($"MB: {GetFormattedTime(stopwatch)}");
        });
    }

    private string GetFormattedTime(Stopwatch stopwatch) =>
        stopwatch.Elapsed.TotalMilliseconds switch
        {
            double ms when ms >= 3 => $"{(int)ms} ms",
            double ms when ms < 3 => $"{ms * 1000:F0} µs",
            _ => "Unknown"
        };
}