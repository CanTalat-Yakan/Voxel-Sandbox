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

    public override void OnUpdate() =>
        MeshBuildingThread();

    public void ChunkGenerationThreadParallel()
    {
        Stopwatch stopwatch = new();

        ParallelOptions options = new() { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 };

        Thread ChunkGenerationThread = new(() =>
        {
            _processingChunkGeneration = true;

            var chunksToGenerate = Generator.ChunksToGenerate.ToArray();
            Generator.ChunksToGenerate.Clear();

            Parallel.ForEach(chunksToGenerate, options, chunk =>
            {
                stopwatch.Restart();

                NoiseSampler.GenerateChunkContent(chunk, this);

                Output.Log($"CG: {GetFormattedTime(stopwatch)}");

            });

            if (!Generator.ChunksToGenerate.IsEmpty)
                ChunkGenerationThreadParallel();

            _processingChunkGeneration = false;
        });

        ChunkGenerationThread.Start();
    }

    public void MeshBuildingThread()
    {
        if (_processingChunkGeneration || Generator.ChunksToBuild.IsEmpty)
            return;

        Stopwatch stopwatch = new();

        Thread MeshBuildingThread = new(() =>
        {
            if (Generator.ChunksToBuild.TryDequeue(out var chunk))
            {
                stopwatch.Start();

                MeshBuilder.GenerateMesh(chunk, this);

                Output.Log($"MB: {GetFormattedTime(stopwatch)}");
            }
        });

        MeshBuildingThread.Start();
    }

    private string GetFormattedTime(Stopwatch stopwatch) =>
        stopwatch.Elapsed.TotalMilliseconds switch
        {
            double ms when ms >= 1.5 => $"{ms:F3} ms",
            double ms when ms < 1.5 => $"{ms * 1000:F0} µs",
            _ => "Unknown"
        };
}