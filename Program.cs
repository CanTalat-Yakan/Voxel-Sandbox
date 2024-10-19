namespace VoxelSandbox;

internal class Program
{
    [STAThread]
    private static void Main() =>
        new Engine.Program().Run(initialization: () =>
            Engine.Kernel.Instance.SystemManager.MainEntityManager.CreateEntity().AddComponent<GameManager>());
}