using System;
using burn.FluidSimulation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot;
using Peridot.Components;
using Peridot.Graphics;

namespace burn.Components;

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

    public BurnableSpriteComponent(string spritePath, float burnRate)
    {
        _sprite = new Sprite(Core.TextureAtlas.GetRegion(spritePath));
        _sdfTexture = CreateSDF.ConvertImageToSDF(_sprite.Region.Texture);
        _burnRate = Math.Clamp(burnRate, 0.01f, 1.0f);
        _currentBurnAmount = 0.0f;
        _isBurning = true;
        _burnDecayEffect = Core.Content.Load<Effect>(_burnDecayShaderPath);
        _clippedRenderTarget = new RenderTarget2D(Core.GraphicsDevice, _sprite.Region.Texture.Width, _sprite.Region.Texture.Height);
    }

    public override void Initialize()
    {
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


}