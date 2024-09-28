using System.Diagnostics;

using Engine;
using Engine.Components;
using Engine.ECS;
using Engine.Editor;
using Engine.Loader;
using Engine.Utilities;

namespace VoxelSandbox;

public sealed class GameManager : Component
{
    public Generator Generator = new();

    public override void OnAwake()
    {
        ImageLoader.LoadTexture(AssetsPaths.ASSETS + "Textures\\TextureAtlas.png");
        Kernel.Instance.Context.CreateShader(AssetsPaths.ASSETS + "Shaders\\VoxelShader");

        if (false)
        {
            var controller = Entity.Manager.CreateCamera(name: "Controller").Entity;
            controller.Transform.SetPosition(y: 200);
            controller.AddComponent<PlayerMovement>();
            controller.AddComponent<RayCaster>().SetCamera(controller);
            controller.GetComponent<Camera>()[0].FOV = 100;
            controller.GetComponent<Camera>()[0].Clipping.Y = 10000;
        }
    }

    private static ParallelOptions _options = new() { MaxDegreeOfParallelism = Environment.ProcessorCount - 2 };

    public override void OnStart() =>
        Generator.Initialize(this);

    public override void OnUpdate() =>
        MeshBuildingThread();

    public void ChunkGenerationThread()
    {
        NoiseSampler noiseSampler = new();
        Stopwatch stopwatch = new();

        Thread ChunkGenerationThread = new(() =>
        {
            Parallel.ForEach(Generator.ChunksToGenerate.AsEnumerable(), _options, chunk =>
            {
                stopwatch.Restart();

                noiseSampler.GenerateChunkContent(chunk, this);

                Output.Log($"CG: {(int)(stopwatch.Elapsed.TotalSeconds * 1000.0)} ms");
            });
        });

        ChunkGenerationThread.Start();
    }

    public void MeshBuildingThread()
    {
        MeshBuilder meshBuilder = new();
        Stopwatch stopwatch = new();

        Thread MeshBuildingThread = new(() =>
        {
            if (Generator.ChunksToBuild.Any())
                if (Generator.ChunksToBuild.TryDequeue(out var chunk))
                {
                    stopwatch.Restart();

                    meshBuilder.GenerateMesh(chunk, this);

                    Output.Log($"MB: {(int)(stopwatch.Elapsed.TotalSeconds * 1000.0)} ms");
                }
        });

        MeshBuildingThread.Start();
    }

}