using System.Diagnostics;

using Engine;
using Engine.Components;
using Engine.ECS;
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

    public override void OnStart()
    {
        Generator.Initialize(new Vector3Int(0, 0, 0));

        int chunkGenerationThreadCount = 4;
        for (int i = 0; i < chunkGenerationThreadCount; i++)
            ChunkGenerationThread();

        //MeshBuildingThread();
    }

    public override void OnUpdate()
    {
        if (Input.GetKey(Vortice.DirectInput.Key.B, InputState.Down))
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
                    if (Generator.ChunksToBuild.TryDequeue(out var chunk))
                    {
                        stopwatch.Restart();

                        MeshBuilder.GenerateMesh(chunk, this);

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
                    if (Generator.ChunksToGenerate.TryDequeue(out var chunk))
                    {
                        stopwatch.Restart();

                        NoiseSampler.GenerateChunkContent(chunk, this);

                        Output.Log($"CG: {(int)(stopwatch.Elapsed.TotalSeconds * 1000.0)} ms");
                    }
        });

        ChunkGenerationThread.Start();
    }
}