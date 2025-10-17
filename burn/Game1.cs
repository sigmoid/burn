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
using burn.Systems.Inventory;

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

    PlayerInventory _playerInventory;
    InventoryUI _inventoryUI;

    RunnerManager _runnerManager;
    RunnerUI _runnerUI;

    CraftingUI _craftingUI;

    TabUIManager _tabUIManager;
    private bool _isTabUIVisible = false;

    private ItemDropManager _itemDropManager;

    ItemPlacementManager _itemPlacementManager;

    public Game1()
    : base("GPU Fluid Simulation", 1300, 1300, false, "fonts/Default")
    {
        Core.Gravity = new Vector2(0, 10);
    }

    protected override void Initialize()
    {
        ButtonRegistry.RegisterButtons(Core.InputManager);

        base.Initialize();

        CreateWall(new Vector2(0, Core.GraphicsDevice.Viewport.Height - 200), new Vector2(Core.GraphicsDevice.Viewport.Width, 100));

        CraftingRecipeRegistry.CreateRecipes();

        TabUIGlobals tabUIGlobals = new TabUIGlobals();
        _playerInventory = new PlayerInventory();
        _inventoryUI = new InventoryUI(_playerInventory);
        _itemDropManager = new ItemDropManager();
        _runnerManager = new RunnerManager(_playerInventory, _itemDropManager);
        _runnerUI = new RunnerUI(_runnerManager);
        _craftingUI = new CraftingUI();
        _tabUIManager = new TabUIManager(_runnerUI, _inventoryUI, _craftingUI);
        _tabUIManager.SetVisibility(false);

        _itemPlacementManager = new ItemPlacementManager(_playerInventory);

        Core.DeveloperConsole.RegisterCommandHandler(new AddInventoryItem(_playerInventory));
        Core.DeveloperConsole.RegisterCommandHandler(new AddRunnerCommandHandler(_runnerManager));
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
        var uiElement = _fluidSimUI.GetUIElement();
        uiElement.SetVisibility(false);
        Core.UISystem.AddElement(uiElement);

        Core.DeveloperConsole.RegisterCommandHandler(new ToggleFluidUICommandHandler(uiElement));
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

        {
            if (Core.InputManager.GetButton("Inventory").IsPressed)
            {
                _isTabUIVisible = !_isTabUIVisible;
                _tabUIManager.SetVisibility(_isTabUIVisible);
            }
        }
        
        _itemPlacementManager.Update();
        _runnerManager.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {

        // TODO: Add your drawing code here

        base.Draw(gameTime);
    }
}
