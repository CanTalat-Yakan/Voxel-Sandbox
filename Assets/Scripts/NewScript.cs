using Engine.ECS;
using Engine.Editor;
using Engine.Utilities;

public class NewScript : Component
{
    [Show]
    private string _text = "Helloo World!";

    // Use this for initialization.
    public override void OnStart()
    {
        Output.Log(_text);
    }

    // Update is called once per frame.
    public override void OnUpdate()
    {

    }
}
