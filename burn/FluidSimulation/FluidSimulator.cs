using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.Devices.Sensors;
using System;
using System.Runtime.CompilerServices;

namespace burn.FluidSimulation
{
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
        private float _diffusion = 0.0001f;
        private float _forceStrength = 1.0f;
        private float _sourceStrength = 1.0f;

        private int diffuseIterations = 10;
        private int pressureIterations = 20;
        private VertexPositionTexture[] _fullScreenVertices;
        private int[] _fullScreenIndices;

        public FluidSimulator(GraphicsDevice graphicsDevice, int gridSize)
        {
            _graphicsDevice = graphicsDevice;
            _gridSize = gridSize;

            CreateRenderTargets();

            CreateFullScreenQuad();
        }

        public void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content)
        {
            _fluidEffect = content.Load<Effect>("FluidEffect");
        }

        public void Update(GameTime gameTime)
        {
            _fluidEffect.Parameters["timeStep"].SetValue((float)gameTime.ElapsedGameTime.TotalSeconds);
            _fluidEffect.Parameters["diffusion"].SetValue(_diffusion);
            _fluidEffect.Parameters["texelSize"].SetValue(new Vector2(1.0f / _gridSize, 1.0f / _gridSize));

            Clamp(_densityRT, _tempDensityRT);
            SwapRenderTargets(ref _densityRT, ref _tempDensityRT);

            Diffuse(_densityRT, _tempDensityRT, diffuseIterations);
            SwapRenderTargets(ref _densityRT, ref _tempDensityRT);

            ComputeDivergence();
            ComputePressure(_pressureRT, _pressureRT2, pressureIterations);
            Project();

            Advect(_velocityRT, _tempVelocityRT);
            SwapRenderTargets(ref _velocityRT, ref _tempVelocityRT);

            Advect(_densityRT, _tempDensityRT);
            SwapRenderTargets(ref _densityRT, ref _tempDensityRT);
        }

        public void AddForce(Vector2 position, Vector2 force, float radius)
        {
            var scaledAmount = force * _forceStrength;

            _fluidEffect.Parameters["sourceTexture"].SetValue(_velocityRT);
            _fluidEffect.Parameters["cursorPosition"].SetValue(position); 
            _fluidEffect.Parameters["cursorValue"].SetValue(scaledAmount); 
            _fluidEffect.Parameters["radius"].SetValue(radius); 

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

            SwapRenderTargets(ref _velocityRT, ref _tempVelocityRT);
        }

        public void AddDensity(Vector2 position, float amount, float radius)
        {
            float scaledAmount = amount * _sourceStrength;

            _fluidEffect.Parameters["sourceTexture"].SetValue(_densityRT);
            _fluidEffect.Parameters["cursorPosition"].SetValue(position);
            _fluidEffect.Parameters["cursorValue"].SetValue(new Vector2(scaledAmount, 0));
            _fluidEffect.Parameters["radius"].SetValue(radius);

            _graphicsDevice.SetRenderTarget(_tempDensityRT);
            _graphicsDevice.Clear(Color.Black);

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

            SwapRenderTargets(ref _densityRT, ref _tempDensityRT);
        }

        public void Draw(RenderTarget2D renderTarget)
        {
            _graphicsDevice.SetRenderTarget(renderTarget);

            if (_fluidEffect == null)
                throw new InvalidOperationException("FluidEffect must be loaded before drawing.");

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

        public void Dispose()
        {
            _velocityRT?.Dispose();
            _densityRT?.Dispose();
            _pressureRT?.Dispose();
            _pressureRT2?.Dispose();
            _tempDensityRT?.Dispose();
            _tempVelocityRT?.Dispose();
        }

        private void CreateRenderTargets()
        {
            _velocityRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Vector2, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _tempVelocityRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Vector2, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _densityRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _pressureRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _pressureRT2 = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _tempDensityRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _divergenceRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }

        private void Clamp(RenderTarget2D source, RenderTarget2D destination)
        {
            _graphicsDevice.SetRenderTarget(destination);

            _graphicsDevice.Clear(Color.Transparent);

            _fluidEffect.Parameters["sourceTexture"].SetValue(source);

            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["Clamp"];

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

        private void CreateFullScreenQuad()
        {
            float width = _gridSize;
            float height = _gridSize;
            _fullScreenVertices = new VertexPositionTexture[4];
            _fullScreenVertices[0] = new VertexPositionTexture(new Vector3(0, height, 0), new Vector2(0, 1)); // Bottom-left
            _fullScreenVertices[1] = new VertexPositionTexture(new Vector3(width, height, 0), new Vector2(1, 1)); // Bottom-right
            _fullScreenVertices[2] = new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(0, 0)); // Top-left
            _fullScreenVertices[3] = new VertexPositionTexture(new Vector3(width, 0, 0), new Vector2(1, 0)); // Top-right


            _fullScreenIndices = new int[] { 0, 1, 2, 2, 1, 3 };
        }

        private void Advect(RenderTarget2D source, RenderTarget2D destination)
        {
            _graphicsDevice.SetRenderTarget(destination);

            _fluidEffect.Parameters["velocityTexture"].SetValue(_velocityRT);
            _fluidEffect.Parameters["sourceTexture"].SetValue(source);

            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["Advect"];

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

        private void Diffuse(RenderTarget2D source, RenderTarget2D destination, int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                _graphicsDevice.SetRenderTarget(destination);

                _fluidEffect.Parameters["sourceTexture"].SetValue(source);

                _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["Diffuse"];

                _fluidEffect.CurrentTechnique.Passes[0].Apply();
                _graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    _fullScreenVertices,
                    0,
                    4,
                    _fullScreenIndices,
                    0,
                    2);

                SwapRenderTargets(ref source, ref destination);

                _graphicsDevice.SetRenderTarget(null);
            }
        }

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

        private void Project()
        {
            _graphicsDevice.SetRenderTarget(_velocityRT);

            _fluidEffect.Parameters["velocityTexture"].SetValue(_velocityRT);
            _fluidEffect.Parameters["pressureTexture"].SetValue(_pressureRT);

            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["Project"];

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

                SwapRenderTargets(ref read, ref write);
            }

            if (iterations % 2 != 0)
            {
                SwapRenderTargets(ref read, ref write);
            }
        }
        
        private void SwapRenderTargets(ref RenderTarget2D rt1, ref RenderTarget2D rt2)
        {
            var temp = rt1;
            rt1 = rt2;
            rt2 = temp;
        }
    }
}
