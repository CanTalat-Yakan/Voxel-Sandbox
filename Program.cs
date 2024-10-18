internal class Program
{
    private static bool _initialized = false;

    [STAThread]
    private static void Main() =>
        new Engine.Program().Run(true, false, null, Frame);

    public static void Frame()
    {
        if (_initialized)
            return;

        _initialized = true;

        Engine.Kernel.Instance.SystemManager.MainEntityManager.CreateEntity().AddComponent<VoxelSandbox.GameManager>();
    }
}