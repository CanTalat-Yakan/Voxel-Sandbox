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

    public override void OnStart()
    {
        Generator.Initialize(new Vector3Int(0, 0, 0));

        ChunkGenerationThread();
        MeshBuildingThread();
    }

    private void ChunkGenerationThread()
    {
        NoiseSampler NoiseSampler = new();
        Stopwatch stopwatch = new();
        stopwatch.Start();

        Thread ChunkGenerationThread = new(() =>
        {
            NoiseSampler NoiseSampler = new();
            Parallel.ForEach(Generator.ChunksToGenerate.AsEnumerable(), _options, chunk =>
            {
                NoiseSampler.GenerateChunkContent(chunk, this);
            });
        });

        ChunkGenerationThread.Start();
    }

    private void MeshBuildingThread()
    {
        MeshBuilder MeshBuilder = new();
        Stopwatch stopwatch = new();
        stopwatch.Start();

        Thread MeshBuildingThread = new(() =>
        {
            while (true)
                if (Generator.ChunksToBuild.Any())
                    if (Generator.ChunksToBuild.TryDequeue(out var chunk))
                    {
                        stopwatch.Restart();

                        MeshBuilder.GenerateMesh(chunk, this);

                        Output.Log($"MB: {(int)(stopwatch.Elapsed.TotalSeconds * 1000.0)} ms");
                    }
        });

        MeshBuildingThread.Start();
    }

}