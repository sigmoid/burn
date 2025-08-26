using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Peridot;
using burn.FluidSimulation;

namespace burn;

public class Game1 : Core
{

    public Game1()
    : base("GPU Fluid Simulation", 800, 800, false)
    {
        // Use a square window for the fluid simulation
    }

    protected override void Initialize()
    {
        ButtonRegistry.RegisterButtons(Core.InputManager);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        base.LoadContent();

        // Load the scene from XML
        CurrentScene = Scene.FromFile(Core.Content, "scenes/fluid_testbed.xml");

    }

    protected override void Update(GameTime gameTime)
    {

        // TODO: Add your update logic here

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {

        // TODO: Add your drawing code here

        base.Draw(gameTime);
    }
}
