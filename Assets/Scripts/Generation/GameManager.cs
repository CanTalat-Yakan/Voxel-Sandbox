using System.Numerics;

using Engine.Components;
using Engine.ECS;
using Engine.Loader;
using Engine.Utilities;

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

        ImageLoader.LoadTexture(AssetsPaths.ASSETS + "Textures\\Dirt.png");

        foreach (var entity in Entity.Manager.Entities.Values)
            if (entity.ID != Entity.ID && entity.GetComponentTypes().Contains(typeof(Mesh)) && !entity.Data.Name.Equals("Sky"))
                entity.Data.IsEnabled = false;

        _camera.Entity.Transform.LocalPosition += Vector3.UnitY * 30;
        _camera.Entity.Transform.EulerAngles = Vector3.Zero;
    }

    public override void OnStart()
    {
        Generator.Initialize(_camera.Entity.Transform.Position);

        // Create a new thread to run the chunk processing
        Thread thread = new(() =>
        {
            while (true)
            {
                if (_timer > 1.0f / 120.0f)
                {
                    _timer = 0;

                    if (Generator.GetChunksToGenerate().Any())
                    {
                        var chunk = Generator.GetChunksToGenerate().Dequeue();

                        NoiseSampler.GenerateChunkContent(chunk);
                        MeshBuilder.GenerateMesh(chunk);
                    }
                }
            }
        });

        // Start the thread
        thread.Start();
    }

    public override void OnUpdate() =>
        _timer += Time.DeltaF;
}