public class ButtonRegistry
{
    public static void RegisterButtons(IInputManager inputManager)
    {
        inputManager.AddButton("AddDensity", MouseButton.Left);
        inputManager.AddButton("AddVelocity", MouseButton.Right);
    }

}