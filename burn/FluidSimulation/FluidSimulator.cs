using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;

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
        private RenderTarget2D _pressureRT2;
        private RenderTarget2D _tempDensityRT;
        private RenderTarget2D _tempVelocityRT;
        private RenderTarget2D _divergenceRT;

        private int _gridSize;
        private float _timeStep = 0.033f;
        private float _diffusion = 0.01f;
        private float _forceStrength = 1.0f;
        private float _sourceStrength = 1.0f;

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
            _tempVelocityRT = new RenderTarget2D(_graphicsDevice, gridSize, gridSize, false,
                SurfaceFormat.Vector2, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _densityRT = new RenderTarget2D(_graphicsDevice, gridSize, gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _pressureRT = new RenderTarget2D(_graphicsDevice, gridSize, gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _pressureRT2 = new RenderTarget2D(_graphicsDevice, gridSize, gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _tempDensityRT = new RenderTarget2D(_graphicsDevice, gridSize, gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _divergenceRT = new RenderTarget2D(_graphicsDevice, gridSize, gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

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
            // // _fluidEffect.Parameters["viscosity"].SetValue(_viscosity);  // not used?
            _fluidEffect.Parameters["texelSize"].SetValue(new Vector2(1.0f / _gridSize, 1.0f / _gridSize));


            Clamp(_densityRT, _tempDensityRT);

            var temp = _densityRT;
            _densityRT = _tempDensityRT;
            _tempDensityRT = temp;

            Diffuse(_densityRT, _tempDensityRT);

            temp = _densityRT;
            _densityRT = _tempDensityRT;
            _tempDensityRT = temp;

            ComputeDivergence();

            ComputePressure(_pressureRT, _pressureRT2, 50);


            _fluidEffect.Parameters["pressureTexture"].SetValue(_pressureRT);

            // 3. Project velocity field to be mass-conserving
            Project();

            // 4. Advect velocity
            Advect(_velocityRT, _tempVelocityRT);

            // Swap render targets again
            var temp2 = _velocityRT;
            _velocityRT = _tempVelocityRT;
            _tempVelocityRT = temp2;

            // //5. Project again for stability
            // Project();

            // 6. Advect density
            Advect(_densityRT, _tempDensityRT);


            // Swap render targets for density
            var temp3 = _densityRT;
            _densityRT = _tempDensityRT;
            _tempDensityRT = temp3;

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
            Console.WriteLine($"Adding force at ({position.X},{position.Y}) with amount {force}");

            var scaledAmount = force * _forceStrength;

            // Set shader parameters
            _fluidEffect.Parameters["sourceTexture"].SetValue(_velocityRT);
            _fluidEffect.Parameters["cursorPosition"].SetValue(position); // reuse this param name or define AddDensityPosition
            _fluidEffect.Parameters["cursorValue"].SetValue(scaledAmount); // encode amount in .x, zero y for density
            _fluidEffect.Parameters["radius"].SetValue(0.05f); // 5% radius

            _graphicsDevice.SetRenderTarget(_tempVelocityRT);

            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["AddValue"];

            _fluidEffect.CurrentTechnique.Passes[0].Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _fullScreenVertices,
                0,
                4,
                _fullScreenIndices,
                0,
                2);


            _graphicsDevice.SetRenderTarget(null);

            var temp = _velocityRT;
            _velocityRT = _tempVelocityRT;
            _tempVelocityRT = temp;
        }

        public void AddDensity(Vector2 position, float amount)
        {
            Console.WriteLine($"Adding density at ({position.X},{position.Y}) with amount {amount}");

            // Scale amount by source strength
            float scaledAmount = amount * _sourceStrength;

            // Set shader parameters
            _fluidEffect.Parameters["sourceTexture"].SetValue(_densityRT);
            _fluidEffect.Parameters["cursorPosition"].SetValue(position); // reuse this param name or define AddDensityPosition
            _fluidEffect.Parameters["cursorValue"].SetValue(new Vector2(scaledAmount, 0)); // encode amount in .x, zero y for density
            _fluidEffect.Parameters["radius"].SetValue(0.05f); // 5% radius

            // Set render target to temp RT for updated density
            _graphicsDevice.SetRenderTarget(_tempDensityRT);

            // Clear target
            _graphicsDevice.Clear(Color.Black);

            // Use AddDensity technique (you'll add this to your shader)
            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["AddValue"];

            _fluidEffect.CurrentTechnique.Passes[0].Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _fullScreenVertices,
                0,
                4,
                _fullScreenIndices,
                0,
                2);


            // Reset render target
            _graphicsDevice.SetRenderTarget(null);

            // Swap density render targets so _densityRT holds updated texture
            var temp = _densityRT;
            _densityRT = _tempDensityRT;
            _tempDensityRT = temp;
        }

        /// <summary>
        /// Draws the fluid simulation to the specified render target.
        /// </summary>
        /// <param name="renderTarget">The render target to draw to.</param>
        public void Draw(RenderTarget2D renderTarget)
        {
            // Set the render target
            _graphicsDevice.SetRenderTarget(renderTarget);

            // Set up a basic effect to draw the density texture
            if (_fluidEffect == null)
                throw new InvalidOperationException("FluidEffect must be loaded before drawing.");

            // Set renderTargetSize for the vertex shader (required for pixel coordinate mapping)
            _fluidEffect.Parameters["renderTargetSize"].SetValue(new Vector2(_gridSize, _gridSize));

            if (_fluidEffect.Parameters["densityTexture"] != null)
                _fluidEffect.Parameters["densityTexture"].SetValue(_densityRT);

            if (_fluidEffect.Parameters["pressureTexture"] != null)
                _fluidEffect.Parameters["pressureTexture"].SetValue(_pressureRT);

            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["Visualize"];
            _fluidEffect.CurrentTechnique.Passes[0].Apply();

            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _fullScreenVertices,
                0,
                4,
                _fullScreenIndices,
                0,
                2);

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

        private void Clamp(RenderTarget2D source, RenderTarget2D destination)
        {
            // Set the render target
            _graphicsDevice.SetRenderTarget(destination);

            _graphicsDevice.Clear(Color.Transparent);

            // Set shader parameters
            _fluidEffect.Parameters["sourceTexture"].SetValue(source);

            // Apply the diffusion technique
            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["Clamp"];

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

            _graphicsDevice.SetRenderTarget(null);

        }

        /// <summary>
        /// Creates a full-screen quad for rendering.
        /// </summary>
        private void CreateFullScreenQuad()
        {
            // Use pixel coordinates for the full-screen quad (0,0)-(width,height)
            float width = _gridSize;
            float height = _gridSize;
            _fullScreenVertices = new VertexPositionTexture[4];
            // VertexPositionTexture(position, texcoord)
            _fullScreenVertices[0] = new VertexPositionTexture(new Vector3(0, height, 0), new Vector2(0, 1)); // Bottom-left
            _fullScreenVertices[1] = new VertexPositionTexture(new Vector3(width, height, 0), new Vector2(1, 1)); // Bottom-right
            _fullScreenVertices[2] = new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(0, 0)); // Top-left
            _fullScreenVertices[3] = new VertexPositionTexture(new Vector3(width, 0, 0), new Vector2(1, 0)); // Top-right


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

            _graphicsDevice.SetRenderTarget(null);
        }

        /// <summary>
        /// Diffuses a field.
        /// </summary>
        /// <param name="source">The source field to diffuse.</param>
        /// <param name="destination">The destination field to diffuse to.</param>
        private void Diffuse(RenderTarget2D source, RenderTarget2D destination)
        {
            var jacobiIterations = 20;

            for (int i = 0; i < jacobiIterations; i++)
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

                // Swap source and destination for next iteration
                var temp = source;
                source = destination;
                destination = temp;

                _graphicsDevice.SetRenderTarget(null);
            }
        }

        /// <summary>
        /// Computes the pressure field from the velocity field using Jacobi iteration.
        /// </summary>
        private void ComputeDivergence()
        {
            _graphicsDevice.SetRenderTarget(_divergenceRT);
            _fluidEffect.Parameters["velocityTexture"].SetValue(_velocityRT);
            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["ComputeDivergence"];
            _fluidEffect.CurrentTechnique.Passes[0].Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _fullScreenVertices,
                0,
                4,
                _fullScreenIndices,
                0,
                2);

            _graphicsDevice.SetRenderTarget(null);
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

        private void ComputePressure(RenderTarget2D read, RenderTarget2D write, int iterations)
        {
            _fluidEffect.Parameters["divergenceTexture"].SetValue(_divergenceRT);

            _graphicsDevice.SetRenderTarget(write);
            _graphicsDevice.Clear(Color.Black);
            _graphicsDevice.SetRenderTarget(read);
            _graphicsDevice.Clear(Color.Black);
            _graphicsDevice.SetRenderTarget(null);

            for (int i = 0; i < iterations; i++)
            {
                _graphicsDevice.SetRenderTarget(write);
                _fluidEffect.Parameters["sourceTexture"].SetValue(read);
                _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["JacobiPressure"];
                _fluidEffect.CurrentTechnique.Passes[0].Apply();
                _graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    _fullScreenVertices,
                    0,
                    4,
                    _fullScreenIndices,
                    0,
                    2);

                // Swap read/write
                var temp = read;
                read = write;
                write = temp;
            }

            // Ensure final result is in `read`
            if (iterations % 2 != 0)
            {
                var temp = read;
                read = write;
                write = temp;
            }
        }

        /// <summary>
        /// Disposes of resources used by the fluid simulator.
        /// </summary>
        public void Dispose()
        {
            _velocityRT?.Dispose();
            _densityRT?.Dispose();
            _pressureRT?.Dispose();
            _pressureRT2?.Dispose();
            _tempDensityRT?.Dispose();
            _tempVelocityRT?.Dispose();
        }
    }
}
