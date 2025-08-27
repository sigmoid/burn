using Peridot.Components;
using burn.FluidSimulation;
using Peridot;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using System;

namespace burn.Components;

public class FluidSimulationComponent : Component
{
    public int Size => _size;
    private FluidSimulator _simulation;
    private RenderTarget2D _simRenderTarget;
    private int _size;

    private GameTime _gameTime;

    private Vector2 _previousMouse;

    public FluidSimulationComponent(int size)
    {
        _size = size;
    }

    public override void Initialize()
    {
        _simulation = new FluidSimulator(Core.GraphicsDevice, _size);
        _simRenderTarget = new RenderTarget2D(Core.GraphicsDevice, _size, _size, false, SurfaceFormat.Color, DepthFormat.None);

        _simulation.LoadContent(Core.Content);

        //_simulation.AddDensity(new Vector2(0.5f, 0.5f), 2000.0f);
    }

    public override void Update(GameTime gameTime)
    {
        _gameTime = gameTime;
    }

    private void ProcessInput()
    {
        var pos = Entity.Position;
        var screenWidth = Core.ScreenWidth;
        var screenHeight = Core.ScreenHeight;

        // Get mouse position in screen space
        var mouseScreen = Core.InputManager.GetMousePosition() * new Vector2(screenWidth, screenHeight);

        // Convert mouse position to world space using the camera's inverse view matrix
        Matrix invView = Matrix.Invert(Core.Camera.GetViewMatrix());
        Vector2 mouseWorld = Vector2.Transform(mouseScreen, invView);

        // Calculate simulation bounds in world space
        Vector2 simTopLeft = pos;
        Vector2 simBottomRight = pos + new Vector2(_size, _size);

        // Check if mouse is inside the simulation bounds
        if (mouseWorld.X >= simTopLeft.X && mouseWorld.X <= simBottomRight.X &&
            mouseWorld.Y >= simTopLeft.Y && mouseWorld.Y <= simBottomRight.Y)
        {
            // Mouse is within the simulation bounds
            // Calculate normalized local coordinates within the simulation
            float localX = (mouseWorld.X - simTopLeft.X) / _size;
            float localY = (mouseWorld.Y - simTopLeft.Y) / _size;
            Vector2 simPosition = new Vector2(localX, localY);

            float previousLocalX = (_previousMouse.X - simTopLeft.X) / _size;
            float previousLocalY = (_previousMouse.Y - simTopLeft.Y) / _size;
            Vector2 previousSimPosition = new Vector2(previousLocalX, previousLocalY);

            Vector2 drag = simPosition - previousSimPosition;


            if (Core.InputManager.GetButton("AddDensity")?.IsHeld == true)
            {
                _simulation.AddDensity(simPosition, 100.0f);
            }
            if (Core.InputManager.GetButton("AddVelocity")?.IsHeld == true)
            {
                _simulation.AddForce(simPosition, drag * 5000.0f);
            }
        }

        _previousMouse = mouseWorld;

    }

    public override void DrawOffscreen()
    {
        // Render the fluid simulation to its render target

        // Core.GraphicsDevice.SetRenderTarget(_simRenderTarget);
        // Core.GraphicsDevice.Clear(Color.Black); // or Color.Transparent
        // Core.GraphicsDevice.SetRenderTarget(null);

        ProcessInput();

        _simulation.Update(_gameTime);
        _simulation.Draw(_simRenderTarget);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        // Draw the render target at the entity's world position
        var pos = Entity.Position;

        spriteBatch.Draw(_simRenderTarget, pos, Color.White);
    }

    public override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _simulation?.Dispose();
            _simRenderTarget?.Dispose();
        }
        base.Dispose(disposing);
    }
}