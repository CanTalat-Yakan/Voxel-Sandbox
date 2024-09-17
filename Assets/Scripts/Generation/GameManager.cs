using System.Numerics;

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
            {
                if (Generator.ChunksToGenerate.Any())
                    NoiseSampler.GenerateChunkContent(Generator.ChunksToGenerate.Dequeue());
            }
        });
        Thread thread2 = new(() =>
        {
            while (true)
            {
                if (Generator.ChunksToBuild.Any())
                    MeshBuilder.GenerateMesh(Generator.ChunksToBuild.Dequeue());
            }
        });

        thread.Start();
        thread2.Start();
    }
}