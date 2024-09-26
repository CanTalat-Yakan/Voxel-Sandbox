using System.Diagnostics;

using Engine;
using Engine.Components;
using Engine.ECS;
using Engine.Loader;
using Engine.Utilities;

namespace VoxelSandbox;

public sealed class GameManager : Component
{
    public static GameManager Instance { get; private set; }

    public Generator Generator = new();

    public override void OnAwake()
    {
        Instance = this;

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

    public override void OnStart()
    {
        Generator.Initialize(new Vector3Int(0, 0, 0));

        int chunkGenerationThreadCount = 2;
        for (int i = 0; i < chunkGenerationThreadCount; i++)
            ChunkGenerationThread();

        MeshBuildingThread();
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
                {
                    stopwatch.Restart();
                    MeshBuilder.GenerateMesh(Generator.ChunksToBuild.Dequeue());

                    Output.Log($"MB: {(int)(stopwatch.Elapsed.TotalSeconds * 1000.0)} ms");
                }
        });

        MeshBuildingThread.Start();
    }

    private void ChunkGenerationThread()
    {
        NoiseSampler NoiseSampler = new();
        Stopwatch stopwatch = new();
        stopwatch.Start();

        // Create a new thread to run the chunk processing
        Thread ChunkGenerationThread = new(() =>
        {
            while (true)
                if (Generator.ChunksToGenerate.Any())
                {
                    stopwatch.Restart();
                    NoiseSampler.GenerateChunkContent(Generator.ChunksToGenerate.Dequeue());

                    Output.Log($"CB: {(int)(stopwatch.Elapsed.TotalSeconds * 1000.0)} ms");
                }
        });

        ChunkGenerationThread.Start();
    }
}