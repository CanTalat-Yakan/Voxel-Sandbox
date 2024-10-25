namespace VoxelSandbox;

class Program
{
    [STAThread]
    private static void Main() =>
        new Engine.Program().Run(
            config: Engine.Config.GetDefault(
                multiSample: Engine.MultiSample.None,
                title: "Voxel-Sandbox",
                width: 1080, height: 720),
            initialization: () =>
                Engine.Kernel.Instance.SystemManager.MainEntityManager.CreateEntity().AddComponent<GameManager>());
}