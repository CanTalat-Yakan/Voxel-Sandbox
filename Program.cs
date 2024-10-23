namespace VoxelSandbox;

internal class Program
{
    [STAThread]
    private static void Main() =>
        new Engine.Program().Run(
            config: Engine.Config.GetDefault(
                multiSample: Engine.MultiSample.None,
                title: "Voxel-Sandbox",
                width: 2560, height: 1440),
            initialization: () =>
                Engine.Kernel.Instance.SystemManager.MainEntityManager.CreateEntity().AddComponent<GameManager>());
}