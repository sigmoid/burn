using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Peridot.Graphics;
using burn.FluidSimulation.Utils;

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

        public ObstacleSpriteData(Sprite sprite, Vector2 position, float obstacleValue = 1.0f, 
                                 float rotation = 0f, Vector2? scale = null, Color? color = null)
        {
            Sprite = sprite;
            Position = position;
            Rotation = rotation;
            Scale = scale ?? Vector2.One;
            Color = color ?? Color.White;
            ObstacleValue = obstacleValue;
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
        private readonly List<ObstacleSpriteData> _sprites;
        private readonly bool _clearTarget;
        private readonly BlendState _blendState;
        private readonly bool _convertToFuel;
        private readonly float _fuelConversionRate;

        /// <summary>
        /// Creates a new sprite-to-obstacle drawing step.
        /// </summary>
        /// <param name="targetField">Target render target field name (default: "spriteObstacle")</param>
        /// <param name="clearTarget">Whether to clear the target before drawing</param>
        /// <param name="blendState">Blend state for sprite rendering (default: AlphaBlend)</param>
        /// <param name="convertToFuel">Whether to convert obstacle pixels to fuel after drawing</param>
        /// <param name="fuelConversionRate">Rate at which obstacles are converted to fuel (default: 1.0)</param>
        public DrawSpritesToObstacleStep(string targetField = "spriteObstacle", bool clearTarget = true, 
                                       BlendState blendState = null, bool convertToFuel = true, float fuelConversionRate = 1.0f)
        {
            _targetField = targetField;
            _sprites = new List<ObstacleSpriteData>();
            _clearTarget = clearTarget;
            _blendState = blendState ?? BlendState.AlphaBlend;
            _convertToFuel = convertToFuel;
            _fuelConversionRate = fuelConversionRate;
        }

        /// <summary>
        /// Adds a sprite to be rendered as an obstacle.
        /// </summary>
        public void AddSprite(ObstacleSpriteData spriteData)
        {
            _sprites.Add(spriteData);
        }

        /// <summary>
        /// Adds a sprite to be rendered as an obstacle with simplified parameters.
        /// </summary>
        public void AddSprite(Sprite sprite, Vector2 position, float obstacleValue = 1.0f, 
                            float rotation = 0f, Vector2? scale = null)
        {
            _sprites.Add(new ObstacleSpriteData(sprite, position, obstacleValue, rotation, scale));
        }

        /// <summary>
        /// Clears all sprites from the list.
        /// </summary>
        public void ClearSprites()
        {
            _sprites.Clear();
        }

        /// <summary>
        /// Gets the current number of sprites to be rendered.
        /// </summary>
        public int SpriteCount => _sprites.Count;

        public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
        {
            if (_sprites.Count == 0)
                return;

            var targetRT = renderTargetProvider.GetTemp(_targetField);
            var currentRT = renderTargetProvider.GetCurrent(_targetField);

            // Set render target
            device.SetRenderTarget(targetRT);

            if (_clearTarget)
            {
                device.Clear(Color.Transparent);
            }
            else
            {
                // Copy current content to preserve existing obstacles
                var spriteBatch = new SpriteBatch(device);
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp);
                spriteBatch.Draw(currentRT, Vector2.Zero, Color.White);
                spriteBatch.End();
                spriteBatch.Dispose();
            }

            // Create SpriteBatch for rendering sprites
            var obstacleSpriteBatch = new SpriteBatch(device);

            // Begin sprite batch with custom blend state
            obstacleSpriteBatch.Begin(SpriteSortMode.Deferred, _blendState, SamplerState.LinearClamp);

            // Draw all sprites
            foreach (var spriteData in _sprites)
            {
                if (spriteData.Sprite?.Region?.Texture != null)
                {
                    // Calculate color with obstacle value as alpha
                    Color renderColor = new Color(
                        spriteData.Color.R,
                        spriteData.Color.G, 
                        spriteData.Color.B,
                        (byte)(spriteData.ObstacleValue * 255));

                    // Draw sprite with rotation and scale
                    obstacleSpriteBatch.Draw(
                        spriteData.Sprite.Region.Texture,
                        spriteData.Position,
                        spriteData.Sprite.Region.SourceRectangle,
                        renderColor,
                        spriteData.Rotation,
                        spriteData.Sprite.Origin,
                        spriteData.Scale,
                        SpriteEffects.None,
                        0f);
                }
            }

            obstacleSpriteBatch.End();
            obstacleSpriteBatch.Dispose();

            device.SetRenderTarget(null);

            // Swap render targets
            renderTargetProvider.Swap(_targetField);

            // Convert obstacle pixels to fuel if enabled
            if (_convertToFuel)
            {
                ConvertObstacleToFuel(device, gridSize, effect, renderTargetProvider);
            }
        }

        /// <summary>
        /// Converts obstacle pixels to fuel using a shader effect.
        /// </summary>
        private void ConvertObstacleToFuel(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider)
        {
            var obstacleRT = renderTargetProvider.GetCurrent(_targetField);
            var fuelRT = renderTargetProvider.GetCurrent("fuel");
            var tempFuelRT = renderTargetProvider.GetTemp("fuel");

            // Set shader parameters
            effect.Parameters["sourceTexture"].SetValue(fuelRT);
            effect.Parameters["obstacleTexture"].SetValue(obstacleRT);
            effect.Parameters["sourceStrength"].SetValue(_fuelConversionRate);

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
