public class ButtonRegistry
{
    public static void RegisterButtons(IInputManager inputManager)
    {
        inputManager.AddButton("AddFuel", MouseButton.Left);
        inputManager.AddButton("AddVelocity", MouseButton.Right);
        inputManager.AddButton("AddTemperature", MouseButton.Middle);
        inputManager.AddButton("AddObstacle", Microsoft.Xna.Framework.Input.Keys.Space);
        inputManager.AddButton("LeftMouse", MouseButton.Left);
        inputManager.AddButton("MiddleClick", MouseButton.Middle);
        inputManager.AddButton("Inventory", Microsoft.Xna.Framework.Input.Keys.Tab);
        inputManager.AddButton("TestUI", Microsoft.Xna.Framework.Input.Keys.T);
    }

}