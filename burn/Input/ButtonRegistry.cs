public class ButtonRegistry
{
    public static void RegisterButtons(IInputManager inputManager)
    {
        inputManager.AddButton("AddFuel", MouseButton.Left);
        inputManager.AddButton("AddVelocity", MouseButton.Right);
        inputManager.AddButton("AddTemperature", MouseButton.Middle);
        inputManager.AddButton("ToggleVorticity", Microsoft.Xna.Framework.Input.Keys.V);
    }

}