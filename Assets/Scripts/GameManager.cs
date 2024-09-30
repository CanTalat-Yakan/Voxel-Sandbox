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

    public override void OnAwake()
    {
        ImageLoader.LoadTexture(AssetsPaths.ASSETS + "Textures\\TextureAtlas.png");
        Kernel.Instance.Context.CreateShader(AssetsPaths.ASSETS + "Shaders\\VoxelShader");

        //PlayerController.Initialize();
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
            var chunksToGenerate = Generator.ChunksToGenerate.ToArray();
            Generator.ChunksToGenerate.Clear();

            Parallel.ForEach(chunksToGenerate, options, chunk =>
            {
                stopwatch.Restart();

                NoiseSampler.GenerateChunkContent(chunk, this);

                Output.Log($"CG: {(int)(stopwatch.Elapsed.TotalSeconds * 1000.0)} ms");
            });

            if (!Generator.ChunksToGenerate.IsEmpty)
                ChunkGenerationThreadParallel();
        });

        ChunkGenerationThread.Start();
    }

    public void MeshBuildingThread()
    {
        if (Generator.ChunksToBuild.IsEmpty)
            return;

        Stopwatch stopwatch = new();

        Thread MeshBuildingThread = new(() =>
        {
            if (Generator.ChunksToBuild.TryDequeue(out var chunk))
            {
                stopwatch.Start();

                MeshBuilder.GenerateMesh(chunk, this);

                Output.Log($"MB: {(int)(stopwatch.Elapsed.TotalSeconds * 1000.0)} ms");
            }
        });

        MeshBuildingThread.Start();
    }

}