using Engine.DataStructures;

internal class Program
{
    [STAThread]
    private static void Main() =>
        new Engine.Program().Run(true, new() { WindowCommand = WindowCommand.Normal}, Frame);

    private static bool _initialized = false;
    public static void Frame()
    {
        if (!_initialized)
            Engine.Kernel.Instance.SystemManager.MainEntityManager.CreateEntity().AddComponent<VoxelSandbox.GameManager>();

        _initialized = true;
    }
}