using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Peridot;
using burn.FluidSimulation;
using System;
using System.Diagnostics;
using System.Linq;
using Peridot.UI;
using burn.Components;
using Peridot.EntityComponentScene.Serialization;
using peridot.EntityComponentScene.Physics;
using peridot.Physics;
using Genbox.VelcroPhysics.Extensions.DebugView;

namespace burn;

public class Game1 : Core
{
    // Framerate tracking variables
    private int _frameCounter = 0;
    private float _timeCounter = 0;
    private float _frameTimeThreshold = 1.0f; // Log every 1 second
    private float _totalFrameTime = 0;

    FluidSimUI _fluidSimUI;
    FluidSimulator _fluidSimulator;

	public Game1()
    : base("GPU Fluid Simulation", 1300, 1300, false)
    {
        // Use a square window for the fluid simulation
        Core.Gravity = new Vector2(0, 10);
	}

    protected override void Initialize()
    {
        ButtonRegistry.RegisterButtons(Core.InputManager);


		base.Initialize();

        CreateWall(new Vector2(0, Core.GraphicsDevice.Viewport.Height - 200), new Vector2(Core.GraphicsDevice.Viewport.Width, 100));
        //CreateWall(new Vector2(0, 0), new Vector2(100, Core.GraphicsDevice.Viewport.Height));
	}

    private void CreateWall(Vector2 position, Vector2 size)
    {
		var wallEntity = EntityFactory.FromString(
	$"""
                <Entity Name="wall">
                <Position>
                    <X>{position.X}</X>
                    <Y>{position.Y}</Y>
                    </Position>
                    <Component Type="RigidbodyComponent">
                    <Property Name="BodyType" Value="Static" />
                    </Component>
                </Entity>
                """
	);
		var wallCollider = new PolygonColliderComponent();

		Vector2 topLeft = new Vector2(0, 0);
		Vector2 topRight = PhysicsSystem.ToSimUnits(new Vector2(size.X, 0));
		Vector2 bottomRight = PhysicsSystem.ToSimUnits(size);
		Vector2 bottomLeft = PhysicsSystem.ToSimUnits(new Vector2(0, size.Y));

		wallCollider.Vertices = new System.Collections.Generic.List<Vector2>
			{
			topLeft,
			topRight,
			bottomRight,
			bottomLeft
			};
		wallEntity.AddComponent(wallCollider);

		Core.CurrentScene.AddEntity(wallEntity);
	}


	protected override void LoadContent()
    {
        base.LoadContent();

        // Load the scene from XML
        CurrentScene = Scene.FromFile(Core.Content, "scenes/fluid_testbed.xml");

        var fluidSimComponent = CurrentScene.GetEntities().Where(x => x.GetComponent<FluidSimulationComponent>() != null).FirstOrDefault();
        _fluidSimulator = fluidSimComponent.GetComponent<FluidSimulationComponent>().GetFluidSimulation();

        _fluidSimUI = new FluidSimUI(Content.Load<SpriteFont>("fonts/JosefinSans"), _fluidSimulator);
        //Core.UISystem.AddElement(_fluidSimUI.GetUIElement());
    }

    protected override void Update(GameTime gameTime)
    {
        if (Core.InputManager.GetButton("MiddleClick").IsPressed)
        {
            CreateEntity();
        }

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

    private void CreateEntity()
    {
		var mousePos = Core.InputManager.GetMousePosition();
		var newEntity = EntityFactory.FromString(
			$"""
                <Entity Name="BurnableSprite">
                    <Position>
                    <X>{mousePos.X * Core.GraphicsDevice.Viewport.Width}</X>
                    <Y>{mousePos.Y * Core.GraphicsDevice.Viewport.Height}</Y>
                    </Position>
                    <Component Type="BurnableSpriteComponent">
                    <Property Name="spritePath" Value="acacia_log" />
                    <Property Name="burnRate" Value="0.1" />
                    </Component>
                </Entity>
            """
		);

		var polygonColliderComponent = new PolygonColliderComponent();
		polygonColliderComponent.Vertices = new System.Collections.Generic.List<Vector2>
			{
				PhysicsSystem.ToSimUnits(new Vector2(0, 0)),
				PhysicsSystem.ToSimUnits(new Vector2(128, 0)),
				PhysicsSystem.ToSimUnits(new Vector2(128, 128)),
				PhysicsSystem.ToSimUnits(new Vector2(0, 128))
			};
		newEntity.AddComponent(polygonColliderComponent);
		newEntity.AddComponent(new RigidbodyComponent(BodyType.Dynamic));
		Core.CurrentScene.AddEntity(newEntity);
	}
}
