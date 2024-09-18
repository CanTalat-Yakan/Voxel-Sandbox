using System.Numerics;
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

    private Camera _camera = Camera.Main;
    private float _timer = 0;

    public override void OnAwake()
    {
        Instance = this;
         
        ImageLoader.LoadTexture(AssetsPaths.ASSETS + "Textures\\TextureAtlasBig.png");
        Kernel.Instance.Context.CreateShader(AssetsPaths.ASSETS + "Shaders\\VoxelShader");

        _camera.Entity.Transform.LocalPosition += Vector3.UnitY * 80;
        _camera.Entity.Transform.EulerAngles = Vector3.Zero;
        _camera.Clipping.Y = 10000;
    }

    public override void OnStart()
    {
        Generator.Initialize(_camera.Entity.Transform.Position);

        // Create a new thread to run the chunk processing
        Thread thread = new(() =>
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
        Thread thread2 = new(() =>
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

        thread.Start();
        thread2.Start();
    }
}