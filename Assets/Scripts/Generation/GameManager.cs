﻿using System.Numerics;

using Engine;
using Engine.Components;
using Engine.ECS;
using Engine.Essentials;
using Engine.Loader;
using Engine.Utilities;

namespace VoxelSandbox;

public class GameManager : Component
{
    public static GameManager Instance { get; private set; }

    public Generator Generator = new();
    public MeshBuilder MeshBuilder = new();
    public NoiseSampler NoiseSampler = new();

    private Camera _camera;
    private float _timer = 0;

    public override void OnAwake()
    {
        Instance = this;

        foreach (var entity in Entity.Manager.Entities.Values)
            if (entity.Data.Tag != "DefaultSky")
                Entity.Manager.DestroyEntity(entity);

        ImageLoader.LoadTexture(AssetsPaths.ASSETS + "Textures\\TextureAtlasBig.png");
        Kernel.Instance.Context.CreateShader(AssetsPaths.ASSETS + "Shaders\\VoxelShader");

        for (int i = 0; i < 4; i++)
        {
            if (Generator.ChunksToGenerate.Any())
                NoiseSampler.GenerateChunkContent(Generator.ChunksToGenerate.Dequeue());
            if (Generator.ChunksToBuild.Any())
                MeshBuilder.GenerateMesh(Generator.ChunksToBuild.Dequeue());
        }

        var controller = Entity.Manager.CreateEntity(name: "Controller");
        _camera = Entity.Manager.CreateCamera(parent: controller);
        _camera.Entity.Transform.SetPosition(y: 152);
        _camera.Entity.Transform.EulerAngles = Vector3.Zero;
        _camera.Entity.AddComponent<PlayerMovement>();
    }

    public override void OnStart()
    {
        Generator.Initialize(new Vector3Int(0, 0, 0));

        // Create a new thread to run the chunk processing
        Thread ChunkGenerationThread = new(() =>
        {
            while (true)
                if (Generator.ChunksToGenerate.Any())
                {
                    Profiler.Start(out var stopwatch);
                    NoiseSampler.GenerateChunkContent(Generator.ChunksToGenerate.Dequeue());
                    //Output.Log($"CB: {(int)(stopwatch.Elapsed.TotalSeconds * 1000.0)} ms");
                    Profiler.Stop(stopwatch, "Chunks Generation");
                }
        });
        Thread MeshBuildingThread = new(() =>
        {
            while (true)
                if (Generator.ChunksToBuild.Any())
                {
                    Profiler.Start(out var stopwatch);
                    MeshBuilder.GenerateMesh(Generator.ChunksToBuild.Dequeue());
                    //Output.Log($"MB: {(int)(stopwatch.Elapsed.TotalSeconds * 1000.0)} ms");
                    Profiler.Stop(stopwatch, "Mesh Generation");
                }
        });

        ChunkGenerationThread.Start();
        MeshBuildingThread.Start();
    }
}
