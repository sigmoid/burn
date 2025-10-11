using System.Numerics;
using Peridot;

public class TabUIGlobals
{
    public static Vector2 WindowPosition;
    public static Vector2 WindowSize = new Vector2(800, 800);

    public TabUIGlobals()
    {
        var screenCenter = new Vector2(Core.GraphicsDevice.Viewport.Width / 2, Core.GraphicsDevice.Viewport.Height / 2);
        WindowPosition = new Vector2(screenCenter.X - WindowSize.X / 2, screenCenter.Y - WindowSize.Y / 2);
    }
}