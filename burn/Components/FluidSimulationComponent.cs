using Peridot.Components;
using burn.FluidSimulation;
using Peridot;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Microsoft.VisualBasic;

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

        var mouseScreen = Core.InputManager.GetMousePosition() * new Vector2(screenWidth, screenHeight);
        var mouseWorld = Core.Camera.ScreenToWorld(mouseScreen);

        var simTopLeft = pos;
        var simBottomRight = pos + new Vector2(_size, _size);

        if (IsMouseInBounds(mouseWorld, simTopLeft, simBottomRight))
        {
            float localX = (mouseWorld.X - simTopLeft.X) / _size;
            float localY = (mouseWorld.Y - simTopLeft.Y) / _size;
            Vector2 simPosition = new Vector2(localX, localY);

            float previousLocalX = (_previousMouse.X - simTopLeft.X) / _size;
            float previousLocalY = (_previousMouse.Y - simTopLeft.Y) / _size;
            Vector2 previousSimPosition = new Vector2(previousLocalX, previousLocalY);

            Vector2 drag = simPosition - previousSimPosition;

            if (Core.InputManager.GetButton("AddDensity")?.IsHeld == true)
            {
                _simulation.AddDensity(simPosition, 100.0f, 0.05f);
            }
            if (Core.InputManager.GetButton("AddVelocity")?.IsHeld == true)
            {
                _simulation.AddForce(simPosition, drag * 5000.0f, 0.05f);
            }
        }

        _previousMouse = mouseWorld;

    }

    public override void DrawOffscreen()
    {
        ProcessInput();

        _simulation.Update(_gameTime);
        _simulation.Draw(_simRenderTarget);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
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

    private bool IsMouseInBounds(Vector2 mousePosition, Vector2 simTopLeft, Vector2 simBottomRight)
    {
        return mousePosition.X >= simTopLeft.X && mousePosition.X <= simBottomRight.X &&
               mousePosition.Y >= simTopLeft.Y && mousePosition.Y <= simBottomRight.Y;
    }   
}