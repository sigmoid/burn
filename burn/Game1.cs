using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Peridot;
using burn.FluidSimulation;
using System;
using System.Diagnostics;

namespace burn;

public class Game1 : Core
{
    // Framerate tracking variables
    private int _frameCounter = 0;
    private float _timeCounter = 0;
    private float _frameTimeThreshold = 1.0f; // Log every 1 second
    private float _totalFrameTime = 0;

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
        // Track framerate
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _frameCounter++;
        _timeCounter += deltaTime;
        _totalFrameTime += deltaTime;
        
        // Log average framerate every second
        if (_timeCounter >= _frameTimeThreshold)
        {
            float averageFrameTime = _totalFrameTime / _frameCounter;
            float fps = 1.0f / (averageFrameTime > 0 ? averageFrameTime : 0.000001f);
            
            Console.WriteLine($"FPS: {fps:F2} | Frame Time: {averageFrameTime * 1000:F2}ms | Frames: {_frameCounter}");
            
            // Reset counters
            _frameCounter = 0;
            _timeCounter = 0;
            _totalFrameTime = 0;
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {

        // TODO: Add your drawing code here

        base.Draw(gameTime);
    }
}
