using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation;
using burn.FluidSimulation.Steps;
using Peridot.Graphics;

namespace burn.Examples
{
    /// <summary>
    /// Example demonstrating how to use sprite-based obstacle rendering in fluid simulation
    /// with automatic fuel conversion capabilities.
    /// </summary>
    public static class SpriteObstacleExample
    {
        /// <summary>
        /// Example of how to set up sprite-based obstacles with fuel conversion in the fluid simulator.
        /// </summary>
        /// <param name="fluidSimulator">The fluid simulator instance</param>
        /// <param name="obstacleTexture">Texture to use for obstacles (e.g., a rock or wall texture)</param>
        public static void SetupSpriteObstaclesWithFuel(FluidSimulator fluidSimulator, Texture2D obstacleTexture)
        {
            // Create a sprite obstacle step with fuel conversion enabled
            // Conversion rate of 2.0 means obstacles convert to fuel twice as fast as normal
            var spriteObstacleStep = fluidSimulator.CreateSpriteObstacleStep(0, true, 2.0f);

            // Create texture region for the entire obstacle texture
            var obstacleRegion = new TextureRegion(obstacleTexture, 0, 0, obstacleTexture.Width, obstacleTexture.Height);
            
            // Create sprite from the texture region
            var obstacleSprite = new Sprite(obstacleRegion);
            obstacleSprite.CenterOrigin(); // Center the origin for easier positioning

            // Add fuel-generating obstacles at different positions
            
            // Large fuel source in the center
            spriteObstacleStep.AddSprite(
                obstacleSprite, 
                new Vector2(256, 256), // Position
                1.0f,                  // Full obstacle strength = maximum fuel generation
                0f,                    // No rotation
                Vector2.One * 2.0f     // 2x scale
            );

            // Smaller fuel sources around the edges
            spriteObstacleStep.AddSprite(
                obstacleSprite,
                new Vector2(100, 100),
                0.8f,                  // 80% obstacle strength = 80% fuel generation rate
                MathHelper.ToRadians(45), // 45 degree rotation
                Vector2.One            // Normal scale
            );

            spriteObstacleStep.AddSprite(
                obstacleSprite,
                new Vector2(400, 150),
                0.6f,                  // 60% obstacle strength = 60% fuel generation rate
                MathHelper.ToRadians(-30), // -30 degree rotation
                Vector2.One * 1.5f     // 1.5x scale
            );
        }

        /// <summary>
        /// Example of setting up obstacles without fuel conversion (pure obstacles).
        /// </summary>
        /// <param name="fluidSimulator">The fluid simulator instance</param>
        /// <param name="obstacleTexture">Texture to use for obstacles</param>
        public static void SetupPureObstacles(FluidSimulator fluidSimulator, Texture2D obstacleTexture)
        {
            // Create a sprite obstacle step WITHOUT fuel conversion
            var spriteObstacleStep = fluidSimulator.CreateSpriteObstacleStep(0, false);

            var obstacleRegion = new TextureRegion(obstacleTexture, 0, 0, obstacleTexture.Width, obstacleTexture.Height);
            var obstacleSprite = new Sprite(obstacleRegion);
            obstacleSprite.CenterOrigin();

            // These obstacles will block fluid flow but won't generate fuel
            spriteObstacleStep.AddSprite(
                obstacleSprite,
                new Vector2(200, 200),
                1.0f,
                0f,
                Vector2.One
            );
        }

        /// <summary>
        /// Example of dynamically updating sprite obstacles during runtime.
        /// </summary>
        /// <param name="spriteObstacleStep">The sprite obstacle step to update</param>
        /// <param name="gameTime">Current game time</param>
        /// <param name="obstacleSprite">The obstacle sprite to animate</param>
        public static void UpdateAnimatedObstacles(DrawSpritesToObstacleStep spriteObstacleStep, GameTime gameTime, Sprite obstacleSprite)
        {
            // Clear existing sprites
            spriteObstacleStep.ClearSprites();

            // Create animated obstacle that moves in a circle
            float time = (float)gameTime.TotalGameTime.TotalSeconds;
            float centerX = 256f;
            float centerY = 256f;
            float radius = 100f;

            Vector2 animatedPosition = new Vector2(
                centerX + (float)System.Math.Cos(time) * radius,
                centerY + (float)System.Math.Sin(time) * radius
            );

            spriteObstacleStep.AddSprite(
                obstacleSprite,
                animatedPosition,
                0.9f,                  // 90% obstacle strength
                time,                  // Rotate based on time
                Vector2.One * (1.0f + 0.3f * (float)System.Math.Sin(time * 2)) // Pulsing scale
            );
        }

        /// <summary>
        /// Example of creating a custom blend state for additive obstacle rendering.
        /// </summary>
        /// <returns>Custom blend state for additive obstacle blending</returns>
        public static BlendState CreateAdditiveObstacleBlendState()
        {
            var additiveBlend = new BlendState
            {
                Name = "AdditiveObstacle",
                ColorSourceBlend = Blend.SourceAlpha,
                ColorDestinationBlend = Blend.One,
                ColorBlendFunction = BlendFunction.Add,
                AlphaSourceBlend = Blend.SourceAlpha,
                AlphaDestinationBlend = Blend.One,
                AlphaBlendFunction = BlendFunction.Add
            };

            return additiveBlend;
        }
    }
}
