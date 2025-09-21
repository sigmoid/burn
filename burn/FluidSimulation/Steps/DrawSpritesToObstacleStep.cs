using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Peridot.Graphics;
using burn.FluidSimulation.Utils;
using Peridot;
using burn.Components;

namespace burn.FluidSimulation.Steps
{
    /// <summary>
    /// Data structure to hold sprite information for rendering to obstacle field
    /// </summary>
    public struct ObstacleSpriteData
    {
        public Sprite Sprite;
        public Vector2 Position;
        public float Rotation;
        public Vector2 Scale;
        public Color Color;
        public float ObstacleValue; // The obstacle strength (0-1)
        public Texture2D SDFTexture; // SDF texture for burnable sprites
        public float BurnAmount; // Current burn amount for burnable sprites
        public Vector2 Size => new Vector2(Sprite.Region.Texture.Width, Sprite.Region.Texture.Height);

        public ObstacleSpriteData(Sprite sprite, Vector2 position, float obstacleValue = 1.0f,
                                 float rotation = 0f, Vector2? scale = null, Color? color = null,
                                 Texture2D sdfTexture = null, float burnAmount = 0f)
        {
            Sprite = sprite;
            Position = position;
            Rotation = rotation;
            Scale = scale ?? Vector2.One;
            Color = color ?? Color.White;
            ObstacleValue = obstacleValue;
            SDFTexture = sdfTexture;
            BurnAmount = burnAmount;
        }
    }

    /// <summary>
    /// Simulation step that draws sprites to the obstacle render target using SpriteBatch.
    /// Allows for complex obstacle shapes defined by sprite textures.
    /// After drawing obstacles, converts obstacle pixels to fuel using a shader effect.
    /// </summary>
    public class DrawSpritesToObstacleStep : IFluidSimulationStep
    {
        private readonly string _targetField;
        private readonly List<BurnableSpriteComponent> _sprites;
        private readonly bool _clearTarget;
        private readonly BlendState _blendState;
        private readonly bool _convertToFuel;
        private readonly float _fuelConversionRate;

        private readonly Effect _burnDecayEffect;

        private Effect _effect;
        private string shaderPath = "shaders/fluid-simulation/obstacle-to-fuel";
        private string _temperatureField = "temperature";
        private string _burnDecayShaderPath = "shaders/burn-decay";
        private float _ignitionTemperature = 0.1f;

        public DrawSpritesToObstacleStep(string targetField = "spriteObstacle", string temperatureField = "temperature", bool clearTarget = true,
                                       BlendState blendState = null, bool convertToFuel = true, float fuelConversionRate = 1.0f, float ignitionTemperature = 0.1f)
        {
            _targetField = targetField;
            _sprites = new List<BurnableSpriteComponent>();
            _clearTarget = clearTarget;
            _blendState = blendState ?? BlendState.AlphaBlend;
            _convertToFuel = convertToFuel;
            _fuelConversionRate = fuelConversionRate;

            _effect = Core.Content.Load<Effect>(shaderPath);
            _burnDecayEffect = Core.Content.Load<Effect>(_burnDecayShaderPath);
            _ignitionTemperature = ignitionTemperature;
            _temperatureField = temperatureField;
        }

        public void AddSprite(BurnableSpriteComponent spriteData)
        {
            _sprites.Add(spriteData);
        }

        public void ClearSprites()
        {
            _sprites.Clear();
        }

        public int SpriteCount => _sprites.Count;

        public void Execute(GraphicsDevice device, int gridSize, IRenderTargetProvider renderTargetProvider, float deltaTime)
        {
            if (_sprites.Count == 0)
                return;

            var targetRT = renderTargetProvider.GetTemp(_targetField);
            var currentRT = renderTargetProvider.GetCurrent(_targetField);

            device.SetRenderTarget(targetRT);

            if (_clearTarget)
            {
                device.Clear(Color.Transparent);
            }
            else
            {
                var spriteBatch = new SpriteBatch(device);
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp);
                spriteBatch.Draw(currentRT, Vector2.Zero, Color.White);
                spriteBatch.End();
                spriteBatch.Dispose();
            }

            var obstacleSpriteBatch = new SpriteBatch(device);

            obstacleSpriteBatch.Begin(SpriteSortMode.Immediate, _blendState, SamplerState.LinearClamp, effect: _burnDecayEffect);

            foreach (var spriteData in _sprites)
            {
                if (spriteData.GetSprite()?.Region?.Texture != null && spriteData.GetSDFTexture() != null)
                {
                    _burnDecayEffect.Parameters["sdfTexture"].SetValue(spriteData.GetSDFTexture());
                    _burnDecayEffect.Parameters["burnAmount"].SetValue(spriteData.GetBurnAmount());
                    _burnDecayEffect.Parameters["mainTexture"].SetValue(spriteData.GetSprite().Region.Texture);
                    _burnDecayEffect.CurrentTechnique = _burnDecayEffect.Techniques["BurnDecay"];
                    _burnDecayEffect.CurrentTechnique.Passes[0].Apply();

                    Color renderColor = Color.White;

                    obstacleSpriteBatch.Draw(
                        spriteData.GetSprite().Region.Texture,
                        spriteData.Entity.Position,
                        spriteData.GetSprite().Region.SourceRectangle,
                        renderColor,
                        spriteData.Entity.Rotation,
                        spriteData.GetSprite().Origin,
                        spriteData.GetScale(),
                        SpriteEffects.None,
                        0f);
                }
            }

            obstacleSpriteBatch.End();
            obstacleSpriteBatch.Dispose();

            device.SetRenderTarget(null);

            renderTargetProvider.Swap(_targetField);

            if (_convertToFuel)
            {
                ConvertObstacleToFuel(device, gridSize, _effect, renderTargetProvider, deltaTime);
            }

            var temperatureRT = renderTargetProvider.GetCurrent(_temperatureField);
            var pixels = new Color[gridSize * gridSize];
            temperatureRT.GetData(pixels);


            foreach (var sprite in _sprites)
            {
                // check the center pixel of the sprite
                int topLeftX = (int)sprite.Entity.Position.X;
                int topLeftY = (int)sprite.Entity.Position.Y;
                int centerX = topLeftX + (int)sprite.GetSprite().Region.Width / 2;
                int centerY = topLeftY + (int)sprite.GetSprite().Region.Height / 2;

                // Check if the center pixel is on fire
                int pixelIndex = centerY * gridSize + centerX;

                if(pixelIndex < 0 || pixelIndex >= pixels.Length)
				{
					sprite.SetBurning(false);
					continue;
				}

				if (pixelIndex < pixels.Length && pixels[pixelIndex].R > _ignitionTemperature)
                {
                    sprite.SetBurning(true);
                }
                else
                {
                    sprite.SetBurning(false);
                }
            }

            ClearSprites();
        }

        /// <summary>
        /// Converts obstacle pixels to fuel using a shader effect.
        /// </summary>
        private void ConvertObstacleToFuel(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
        {
            var obstacleRT = renderTargetProvider.GetCurrent(_targetField);
            var fuelRT = renderTargetProvider.GetCurrent("fuel");
            var tempFuelRT = renderTargetProvider.GetTemp("fuel");

            // Set shader parameters
            effect.Parameters["renderTargetSize"].SetValue(new Vector2(gridSize, gridSize));
            effect.Parameters["sourceTexture"].SetValue(fuelRT);
            effect.Parameters["spriteObstacleTexture"].SetValue(obstacleRT);
            effect.Parameters["sourceStrength"].SetValue(_fuelConversionRate);
            effect.Parameters["timeStep"].SetValue(deltaTime);

            // Render to temp fuel target
            device.SetRenderTarget(tempFuelRT);

            effect.CurrentTechnique = effect.Techniques["ObstacleToFuel"];
            effect.CurrentTechnique.Passes[0].Apply();
            Utils.Utils.DrawFullScreenQuad(device, gridSize);

            device.SetRenderTarget(null);

            // Swap fuel targets to make the result current
            renderTargetProvider.Swap("fuel");
        }
    }
}
