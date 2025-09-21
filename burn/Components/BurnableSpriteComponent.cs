using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text.Json.Serialization;
using burn.FluidSimulation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using peridot.EntityComponentScene.Physics;
using peridot.Physics;
using Peridot;
using Peridot.Components;
using Peridot.Graphics;

namespace burn.Components;

public class BurnableData
{
    public List<BurnableMesh> Meshes { get; set; } 
}
public class BurnableMesh
{
    public float Amount { get; set; }
    public List<BurnableVertex> Vertices { get; set;}
}

public class BurnableVertex
{
    public float X { get; set; }
    public float Y { get; set;}
}

public class BurnableSpriteComponent : Component
{
    private Sprite _sprite;
    private Texture2D _sdfTexture;
    private float _burnRate;
    private float _currentBurnAmount;
    private bool _isBurning = true;
    private FluidSimulator _fluidSimulation;
    private Effect _burnDecayEffect;
    private string _burnDecayShaderPath = "shaders/burn-decay";

    private RenderTarget2D _clippedRenderTarget;

    private PolygonColliderComponent _collider;

	private BurnableData _burnableData;
    private string _spritePath; // Store the path for independent SDF creation

	public BurnableSpriteComponent(string spritePath, float burnRate)
    {
        _spritePath = spritePath;
        _sprite = new Sprite(Core.TextureAtlas.GetRegion(spritePath));
        
        LoadShapeData(spritePath);

        // Create a unique SDF texture for this instance
        _sdfTexture = CreateSDF.ConvertImageToSDF(_sprite.Region.Texture);
        _burnRate = Math.Clamp(burnRate, 0.01f, 1.0f);
        _currentBurnAmount = 0.0f; // Each instance starts with its own burn amount
        _isBurning = true;
        _burnDecayEffect = Core.Content.Load<Effect>(_burnDecayShaderPath);
        _clippedRenderTarget = new RenderTarget2D(Core.GraphicsDevice, _sprite.Region.Texture.Width, _sprite.Region.Texture.Height);
    }

    public override void Initialize()
    {
        _collider = RequireComponent<PolygonColliderComponent>();

		var entities = Core.CurrentScene.GetEntities();
        var fluidSimEntity = entities.Find(e => e.GetComponent<FluidSimulationComponent>() != null);
        if (fluidSimEntity != null)
        {
            var fluidSimComponent = fluidSimEntity.GetComponent<FluidSimulationComponent>();
            _fluidSimulation = fluidSimComponent.GetFluidSimulation();
        }
        else
        {
            throw new Exception("No FluidSimulationComponent found in the scene.");
        }
    }

    public override void Update(GameTime gameTime)
    {
        _fluidSimulation.AddSprite(this);

        UpdateBurnDecay((float)gameTime.ElapsedGameTime.TotalSeconds);


    }

    public Sprite GetSprite() => _sprite;
    public Texture2D GetSDFTexture() => _sdfTexture;
    public float GetBurnAmount() => _currentBurnAmount;
    public float GetBurnRate() => _burnRate;
    public bool IsBurning() => _isBurning;
    public void SetBurning(bool isBurning) => _isBurning = isBurning;
    public Vector2 GetScale() => Vector2.One;
    public float GetRotation() => 0f;

    private void UpdateBurnDecay(float deltaTime)
    {
        if (!_isBurning)
            return;

        _currentBurnAmount += _burnRate * deltaTime;

        _collider.SetVertices(GetTransformedVertices());
	}

    private List<Vector2> GetTransformedVertices()
    {
        var mesh = GetBurnableMesh(_currentBurnAmount);

        var verts = mesh.Vertices.Select(x => new Vector2(x.X, x.Y)).ToList();
        verts = verts.Select(v => v * new Vector2(_sprite.Width, _sprite.Height)).ToList();
        verts = verts.Select(v => PhysicsSystem.ToSimUnits(v)).ToList();

        return verts;
	}


    private void DrawFullScreenQuad(GraphicsDevice device, int width, int height)
    {
        var fullScreenVertices = new VertexPositionTexture[4];
        fullScreenVertices[0] = new VertexPositionTexture(new Vector3(0, height, 0), new Vector2(0, 1)); // Bottom-left
        fullScreenVertices[1] = new VertexPositionTexture(new Vector3(width, height, 0), new Vector2(1, 1)); // Bottom-right
        fullScreenVertices[2] = new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(0, 0)); // Top-left
        fullScreenVertices[3] = new VertexPositionTexture(new Vector3(width, 0, 0), new Vector2(1, 0)); // Top-right

        var vertices = new[]
        {
            new VertexPositionTexture(new Vector3(0, height, 0), new Vector2(0, 1)), // Bottom-left
            new VertexPositionTexture(new Vector3(width, height, 0), new Vector2(1, 1)), // Bottom-right
            new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(0, 0)), // Top-left
            new VertexPositionTexture(new Vector3(width, 0, 0), new Vector2(1, 0)) // Top-right
        };

        var indices = new[] { 0, 1, 2, 2, 1, 3 };

        device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 4, indices, 0, 2);
    }

    private BurnableMesh GetBurnableMesh(float t)
	{
        float closest = 0.0f;
		BurnableMesh closestMesh = null;

		for (int i = 0; i < _burnableData.Meshes.Count; i++)
        {
            var currentMesh = _burnableData.Meshes[i];

            if (currentMesh.Amount <= t && currentMesh.Amount > closest)
            {
				closest = currentMesh.Amount;
				closestMesh = currentMesh;
			}
		}

        if (closestMesh == null)
            closestMesh = _burnableData.Meshes.First();

        return closestMesh;
	}

	private void LoadShapeData(string spritePath)
	{
		string shapeDataName = spritePath + "_collision.json";

		//load shape data from json file
		var shapeDataText = System.IO.File.ReadAllText("Content/images/" + shapeDataName);
		var shapeData = JsonConvert.DeserializeObject<BurnableData>(shapeDataText);

		_burnableData = shapeData;
	}
}