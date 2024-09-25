using Engine;
using Engine.Components;
using Engine.ECS;
using Engine.Loader;
using Engine.Utilities;

namespace VoxelSandbox;

public class GameManager : Component
{
    public static GameManager Instance { get; private set; }

    public Generator Generator = new();
    public MeshBuilder MeshBuilder = new();
    public NoiseSampler NoiseSampler = new();

    public override void OnAwake()
    {
        Instance = this;

        //foreach (var entity in Entity.Manager.Entities.Values)
        //    if (entity.Data.Tag != "DefaultSky")
        //        Entity.Manager.DestroyEntity(entity);

        ImageLoader.LoadTexture(AssetsPaths.ASSETS + "Textures\\TextureAtlasBig2.png");
        Kernel.Instance.Context.CreateShader(AssetsPaths.ASSETS + "Shaders\\VoxelShader");

        var controller = Entity.Manager.CreateCamera(name: "Controller").Entity;
        controller.Transform.SetPosition(y: 200);
        controller.AddComponent<PlayerMovement>();
        controller.AddComponent<RayCaster>().SetCamera(controller);
        controller.GetComponent<Camera>()[0].FOV = 100;
        controller.GetComponent<Camera>()[0].Clipping.Y = 10000;
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
                    Output.Log($"CB: {(int)(stopwatch.Elapsed.TotalSeconds * 1000.0)} ms");
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
                    Output.Log($"MB: {(int)(stopwatch.Elapsed.TotalSeconds * 1000.0)} ms");
                    Profiler.Stop(stopwatch, "Mesh Generation");
                }
        });

        ChunkGenerationThread.Start();
        MeshBuildingThread.Start();
    }
}
