using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace burn.FluidSimulation
{
    /// <summary>
    /// GPU-based fluid simulation using a grid-based approach.
    /// This implements a simplified version of Jos Stam's "Stable Fluids" algorithm.
    /// </summary>
    public class FluidSimulator
    {
        private GraphicsDevice _graphicsDevice;
        private Effect _fluidEffect;
        private RenderTarget2D _velocityRT;
        private RenderTarget2D _densityRT;
        private RenderTarget2D _pressureRT;
        private RenderTarget2D _tempRT;
        
        private int _gridSize;
        private float _timeStep = 0.033f;
        private float _diffusion = 0.0001f;
        private float _viscosity = 0.0001f;
        private float _forceStrength = 5.0f;
        private float _sourceStrength = 100.0f;
        
        private VertexPositionTexture[] _fullScreenVertices;
        private int[] _fullScreenIndices;

        /// <summary>
        /// Creates a new fluid simulator with the specified grid size.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device to use for rendering.</param>
        /// <param name="gridSize">The size of the simulation grid (gridSize x gridSize).</param>
        public FluidSimulator(GraphicsDevice graphicsDevice, int gridSize)
        {
            _graphicsDevice = graphicsDevice;
            _gridSize = gridSize;
            
            // Create render targets for velocity, density, and pressure fields
            _velocityRT = new RenderTarget2D(_graphicsDevice, gridSize, gridSize, false, 
                SurfaceFormat.Vector2, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _densityRT = new RenderTarget2D(_graphicsDevice, gridSize, gridSize, false, 
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _pressureRT = new RenderTarget2D(_graphicsDevice, gridSize, gridSize, false, 
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _tempRT = new RenderTarget2D(_graphicsDevice, gridSize, gridSize, false, 
                SurfaceFormat.Vector4, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            
            // Create full-screen quad for rendering
            CreateFullScreenQuad();
        }

        /// <summary>
        /// Loads the necessary shader effects for the fluid simulation.
        /// </summary>
        /// <param name="content">The content manager to use for loading.</param>
        public void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content)
        {
            _fluidEffect = content.Load<Effect>("FluidEffect");
        }

        /// <summary>
        /// Updates the fluid simulation.
        /// </summary>
        /// <param name="gameTime">The current game time.</param>
        public void Update(GameTime gameTime)
        {
            // Update time step based on game time
            _timeStep = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // Set common shader parameters
            _fluidEffect.Parameters["timeStep"].SetValue(_timeStep);
            _fluidEffect.Parameters["diffusion"].SetValue(_diffusion);
            _fluidEffect.Parameters["viscosity"].SetValue(_viscosity);
            _fluidEffect.Parameters["texelSize"].SetValue(new Vector2(1.0f / _gridSize, 1.0f / _gridSize));
            
            // 1. Diffuse velocity
            Diffuse(_velocityRT, _tempRT);
            
            // Swap render targets
            RenderTarget2D temp = _velocityRT;
            _velocityRT = _tempRT;
            _tempRT = temp;
            
            // 2. Calculate pressure
            ComputePressure();
            
            // 3. Project velocity field to be mass-conserving
            Project();
            
            // 4. Advect velocity
            Advect(_velocityRT, _tempRT);
            
            // Swap render targets again
            temp = _velocityRT;
            _velocityRT = _tempRT;
            _tempRT = temp;
            
            // 5. Project again for stability
            Project();
            
            // 6. Advect density
            Advect(_densityRT, _tempRT);
            
            // Swap render targets for density
            temp = _densityRT;
            _densityRT = _tempRT;
            _tempRT = temp;
            
            // Reset the render target
            _graphicsDevice.SetRenderTarget(null);
        }

        /// <summary>
        /// Adds force to the fluid at the specified position.
        /// </summary>
        /// <param name="position">The position to add force at (normalized 0-1 range).</param>
        /// <param name="force">The force vector to add.</param>
        public void AddForce(Vector2 position, Vector2 force)
        {
            // Create a temporary render target for the force
            RenderTarget2D forceRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize);
            
            // Set the render target
            _graphicsDevice.SetRenderTarget(forceRT);
            
            // Clear the render target
            _graphicsDevice.Clear(Color.Transparent);
            
            // Draw a point at the specified position with the force as color
            SpriteBatch spriteBatch = Peridot.Core.SpriteBatch;
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            
            // Create a 1x1 pixel texture with the force as color
            Texture2D pixel = new Texture2D(_graphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            
            // Scale the force by the strength
            force *= _forceStrength;
            
            // Convert the force to a color (x = red, y = green)
            Color forceColor = new Color(force.X + 0.5f, force.Y + 0.5f, 0.5f, 1.0f);
            
            // Draw the force at the specified position
            float radius = _gridSize * 0.05f; // 5% of grid size
            spriteBatch.Draw(
                pixel,
                new Rectangle(
                    (int)(position.X * _gridSize - radius / 2),
                    (int)(position.Y * _gridSize - radius / 2),
                    (int)radius,
                    (int)radius),
                forceColor);
            
            spriteBatch.End();
            
            // Add the force to the velocity field
            _graphicsDevice.SetRenderTarget(_velocityRT);
            
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            spriteBatch.Draw(forceRT, Vector2.Zero, Color.White);
            spriteBatch.End();
            
            // Clean up
            forceRT.Dispose();
            pixel.Dispose();
            
            // Reset the render target
            _graphicsDevice.SetRenderTarget(null);
        }

        /// <summary>
        /// Adds density to the fluid at the specified position.
        /// </summary>
        /// <param name="position">The position to add density at (normalized 0-1 range).</param>
        /// <param name="amount">The amount of density to add.</param>
        public void AddDensity(Vector2 position, float amount)
        {
            // Create a temporary render target for the density
            RenderTarget2D densitySourceRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize);
            
            // Set the render target
            _graphicsDevice.SetRenderTarget(densitySourceRT);
            
            // Clear the render target
            _graphicsDevice.Clear(Color.Transparent);
            
            // Draw a point at the specified position with the density as color
            SpriteBatch spriteBatch = Peridot.Core.SpriteBatch;
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            
            // Create a 1x1 pixel texture with the density as color
            Texture2D pixel = new Texture2D(_graphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            
            // Scale the amount by the strength
            amount *= _sourceStrength;
            
            // Convert the amount to a color
            Color densityColor = new Color(amount, amount, amount, 1.0f);
            
            // Draw the density at the specified position
            float radius = _gridSize * 0.05f; // 5% of grid size
            spriteBatch.Draw(
                pixel,
                new Rectangle(
                    (int)(position.X * _gridSize - radius / 2),
                    (int)(position.Y * _gridSize - radius / 2),
                    (int)radius,
                    (int)radius),
                densityColor);
            
            spriteBatch.End();
            
            // Add the density to the density field
            _graphicsDevice.SetRenderTarget(_densityRT);
            
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
            spriteBatch.Draw(densitySourceRT, Vector2.Zero, Color.White);
            spriteBatch.End();
            
            // Clean up
            densitySourceRT.Dispose();
            pixel.Dispose();
            
            // Reset the render target
            _graphicsDevice.SetRenderTarget(null);
        }

        /// <summary>
        /// Draws the fluid simulation to the specified render target.
        /// </summary>
        /// <param name="renderTarget">The render target to draw to.</param>
        public void Draw(RenderTarget2D renderTarget)
        {
            // Set the render target
            _graphicsDevice.SetRenderTarget(renderTarget);
            
            // Set shader parameters
            _fluidEffect.Parameters["velocityTexture"].SetValue(_velocityRT);
            _fluidEffect.Parameters["densityTexture"].SetValue(_densityRT);
            
            // Apply the visualization technique
            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["Visualize"];
            
            // Draw a full-screen quad
            _fluidEffect.CurrentTechnique.Passes[0].Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _fullScreenVertices,
                0,
                4,
                _fullScreenIndices,
                0,
                2);
            
            // Reset the render target
            _graphicsDevice.SetRenderTarget(null);
        }

        /// <summary>
        /// Gets the density field as a texture.
        /// </summary>
        public Texture2D DensityField => _densityRT;

        /// <summary>
        /// Gets the velocity field as a texture.
        /// </summary>
        public Texture2D VelocityField => _velocityRT;

        /// <summary>
        /// Creates a full-screen quad for rendering.
        /// </summary>
        private void CreateFullScreenQuad()
        {
            _fullScreenVertices = new VertexPositionTexture[4];
            _fullScreenVertices[0] = new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, 1));
            _fullScreenVertices[1] = new VertexPositionTexture(new Vector3(1, -1, 0), new Vector2(1, 1));
            _fullScreenVertices[2] = new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 0));
            _fullScreenVertices[3] = new VertexPositionTexture(new Vector3(1, 1, 0), new Vector2(1, 0));

            _fullScreenIndices = new int[] { 0, 1, 2, 2, 1, 3 };
        }

        /// <summary>
        /// Advects a field through the velocity field.
        /// </summary>
        /// <param name="source">The source field to advect.</param>
        /// <param name="destination">The destination field to advect to.</param>
        private void Advect(RenderTarget2D source, RenderTarget2D destination)
        {
            // Set the render target
            _graphicsDevice.SetRenderTarget(destination);
            
            // Set shader parameters
            _fluidEffect.Parameters["velocityTexture"].SetValue(_velocityRT);
            _fluidEffect.Parameters["sourceTexture"].SetValue(source);
            
            // Apply the advection technique
            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["Advect"];
            
            // Draw a full-screen quad
            _fluidEffect.CurrentTechnique.Passes[0].Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _fullScreenVertices,
                0,
                4,
                _fullScreenIndices,
                0,
                2);
        }
        
        /// <summary>
        /// Diffuses a field.
        /// </summary>
        /// <param name="source">The source field to diffuse.</param>
        /// <param name="destination">The destination field to diffuse to.</param>
        private void Diffuse(RenderTarget2D source, RenderTarget2D destination)
        {
            // Set the render target
            _graphicsDevice.SetRenderTarget(destination);
            
            // Set shader parameters
            _fluidEffect.Parameters["sourceTexture"].SetValue(source);
            
            // Apply the diffusion technique
            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["Diffuse"];
            
            // Draw a full-screen quad
            _fluidEffect.CurrentTechnique.Passes[0].Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _fullScreenVertices,
                0,
                4,
                _fullScreenIndices,
                0,
                2);
        }
        
        /// <summary>
        /// Computes the pressure field from the velocity field.
        /// </summary>
        private void ComputePressure()
        {
            // Set the render target
            _graphicsDevice.SetRenderTarget(_pressureRT);
            
            // Set shader parameters
            _fluidEffect.Parameters["velocityTexture"].SetValue(_velocityRT);
            
            // Apply the pressure computation technique
            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["ComputePressure"];
            
            // Draw a full-screen quad
            _fluidEffect.CurrentTechnique.Passes[0].Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _fullScreenVertices,
                0,
                4,
                _fullScreenIndices,
                0,
                2);
        }
        
        /// <summary>
        /// Projects the velocity field to be mass-conserving.
        /// </summary>
        private void Project()
        {
            // Set the render target
            _graphicsDevice.SetRenderTarget(_velocityRT);
            
            // Set shader parameters
            _fluidEffect.Parameters["velocityTexture"].SetValue(_velocityRT);
            _fluidEffect.Parameters["pressureTexture"].SetValue(_pressureRT);
            
            // Apply the projection technique
            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["Project"];
            
            // Draw a full-screen quad
            _fluidEffect.CurrentTechnique.Passes[0].Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _fullScreenVertices,
                0,
                4,
                _fullScreenIndices,
                0,
                2);
        }

        /// <summary>
        /// Disposes of resources used by the fluid simulator.
        /// </summary>
        public void Dispose()
        {
            _velocityRT?.Dispose();
            _densityRT?.Dispose();
            _pressureRT?.Dispose();
            _tempRT?.Dispose();
        }
    }
}
