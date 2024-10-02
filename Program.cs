internal class Program
{
    [STAThread]
    private static void Main() =>
        new Engine.Program().Run(true, null, Frame);

    private static bool _initialized = false;
    public static void Frame()
    {
        if (!_initialized)
            Engine.Kernel.Instance.SystemManager.MainEntityManager.CreateEntity().AddComponent<VoxelSandbox.GameManager>();

        _initialized = true;
    }
}