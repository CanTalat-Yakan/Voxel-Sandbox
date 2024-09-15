using System.Numerics;

using Engine.Components;
using Engine.ECS;
using Engine.Utilities;

public class GameManager : Component
{
    public static GameManager Instance { get; private set; }

    public Generator Generator = new();
    public MeshBuilder MeshBuilder = new();
    public NoiseSampler NoiseSampler = new();

    Camera _camera = Camera.Main;

    public override void OnStart()
    {
        Instance = this;

        foreach (var entity in Entity.Manager.Entities.Values)
            if (entity.ID != Entity.ID && entity.GetComponentTypes().Contains(typeof(Mesh)) && !entity.Data.Name.Equals("Sky"))
                entity.Data.IsEnabled = false;

        _camera.Entity.Transform.LocalPosition += Vector3.UnitY * 30;
        _camera.Entity.Transform.EulerAngles = Vector3.Zero;

        Generator.Initialize(_camera.Entity.Transform.Position);
        Generator.UpdateChunks(_camera.Entity.Transform.Position);

        //ParallelOptions options = new() { MaxDegreeOfParallelism = Environment.ProcessorCount };
        //Parallel.ForEach(_generator.GetChunksToGenerate(), options, chunk =>
        //{
        //    _noiseSampler.GenerateChunkContent(chunk);
        //    _meshBuilder.GenerateMesh(chunk);
        //});

        //foreach (var chunk in Generator.GetChunksToGenerate())
        //{
        //    NoiseSampler.GenerateChunkContent(chunk);
        //    MeshBuilder.GenerateMesh(chunk);
        //}
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

    float _timer = 0;
    public override void OnUpdate()
    {
        //    if (_timer > 1.0f / 120.0f)
        //    {
        //        _timer = 0;

        //        //if (Generator.GetChunksToGenerate().Any())
        //        //{
        //        //    var chunk = Generator.GetChunksToGenerate().Dequeue();

        //        //    NoiseSampler.GenerateChunkContent(chunk);
        //        //    MeshBuilder.GenerateMesh(chunk);
        //        //}
        //    }

        _timer += Time.DeltaF;
    }
}