public class ButtonRegistry
{
    public static void RegisterButtons(IInputManager inputManager)
    {
        inputManager.AddButton("AddFuel", MouseButton.Left);
        inputManager.AddButton("AddVelocity", MouseButton.Right);
        inputManager.AddButton("AddTemperature", MouseButton.Middle);
    }

}