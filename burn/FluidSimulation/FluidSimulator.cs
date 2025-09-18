using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using burn.FluidSimulation.Steps;
using Peridot.Graphics;

namespace burn.FluidSimulation
{
    public class FluidSimulator
    {
        private GraphicsDevice _graphicsDevice;
        private Effect _fluidEffect;
        private Texture2D _flameGradientTexture;
        private SpriteBatch _spriteBatch;

        #region Render Targets

        private RenderTarget2D _velocityRT;
        private RenderTarget2D _fuelRT;
        private RenderTarget2D _pressureRT;
        private RenderTarget2D _pressureRT2;
        private RenderTarget2D _tempFuelRT;
        private RenderTarget2D _tempVelocityRT;
        private RenderTarget2D _divergenceRT;
        private RenderTarget2D _tempDivergenceRT;
        private RenderTarget2D _temperatureRT;
        private RenderTarget2D _tempTemperatureRT;
        private RenderTarget2D _vorticityRT;
        private RenderTarget2D _obstacleRT;
        private RenderTarget2D _tempObstacleRT;
        private RenderTarget2D _smokeRT;
        private RenderTarget2D _tempSmokeRT;
        private RenderTarget2D _spriteObstacleRT;
        private RenderTarget2D _tempSpriteObstacleRT;

        #endregion

        #region Fluid Sim Properties

        private int _gridSize;
        private float _diffusion = 0.0001f;
        private float _forceStrength = 1.0f;
        private float _sourceStrength = 1.0f;
        private float _vorticityScale = 0.125f;

        private int diffuseIterations = 20;
        private int pressureIterations = 20;

        private float ignitionTemperature = 0.3f;
        private float fuelBurnTemperature = 20.0f;
        private float fuelConsumptionRate = 32.0f;
        private float minFuelThreshold = 0.01f;

        private float combustionPressure = -75.0f;

        private int temperatureDiffuseIterations = 20;

        private int spreadFireIterations = 30;

        private float buoyancyConstant = 80.0f;
        private float gravity = -9.81f;

        float ambientTemperature = 0;
        float maxTemperature = 1.0f;
        float coolingRate = 125.0f / 2.0f;

        float smokeEmissionRate = 256.0f;

        float velocityDampingCoefficient = 0.75f;

        #endregion

        private DrawSpritesToObstacleStep _spriteObstacleStep;

        private List<IFluidSimulationStep> _simulationSteps;
        private RenderTargetProvider _renderTargetProvider;

        public FluidSimulator(GraphicsDevice graphicsDevice, int gridSize)
        {
            _graphicsDevice = graphicsDevice;
            _gridSize = gridSize;
            _spriteBatch = new SpriteBatch(graphicsDevice);

            CreateRenderTargets();
            InitializeRenderTargets();
            PopulateRenderTargetProvider();
            CreateSimulationSteps();
        }

        private void InitializeRenderTargets()
        {
            _graphicsDevice.SetRenderTarget(_velocityRT);
            _graphicsDevice.Clear(Color.Transparent);
            _graphicsDevice.SetRenderTarget(_fuelRT);
            _graphicsDevice.Clear(Color.Transparent);
            _graphicsDevice.SetRenderTarget(_temperatureRT);
            _graphicsDevice.Clear(Color.Transparent);
            _graphicsDevice.SetRenderTarget(_pressureRT);
            _graphicsDevice.Clear(Color.Transparent);
            _graphicsDevice.SetRenderTarget(_pressureRT2);
            _graphicsDevice.Clear(Color.Transparent);
            _graphicsDevice.SetRenderTarget(_vorticityRT);
            _graphicsDevice.Clear(Color.Transparent);
            _graphicsDevice.SetRenderTarget(_obstacleRT);
            _graphicsDevice.Clear(Color.Transparent);
            _graphicsDevice.SetRenderTarget(_spriteObstacleRT);
            _graphicsDevice.Clear(Color.Transparent);
            _graphicsDevice.SetRenderTarget(null);

        }

        private void PopulateRenderTargetProvider()
        {
            _renderTargetProvider = new RenderTargetProvider();
            _renderTargetProvider.RegisterRenderTargetPair("fuel", _fuelRT, _tempFuelRT);
            _renderTargetProvider.RegisterRenderTargetPair("temperature", _temperatureRT, _tempTemperatureRT);
            _renderTargetProvider.RegisterRenderTargetPair("velocity", _velocityRT, _tempVelocityRT);
            _renderTargetProvider.RegisterRenderTargetPair("divergence", _divergenceRT, _tempDivergenceRT);
            _renderTargetProvider.RegisterRenderTargetPair("pressure", _pressureRT, _pressureRT2);
            _renderTargetProvider.RegisterRenderTargetPair("vorticity", _vorticityRT, _vorticityRT);
            _renderTargetProvider.RegisterRenderTargetPair("obstacle", _obstacleRT, _tempObstacleRT);
            _renderTargetProvider.RegisterRenderTargetPair("smoke", _smokeRT, _tempSmokeRT);
            _renderTargetProvider.RegisterRenderTargetPair("spriteObstacle", _spriteObstacleRT, _tempSpriteObstacleRT);
        }

        private void CreateSimulationSteps()
        {
            _spriteObstacleStep = new DrawSpritesToObstacleStep("spriteObstacle", true, null, true, 20);

            _simulationSteps = new List<IFluidSimulationStep>
            {
                _spriteObstacleStep,
                new ClampStep("fuel"),
                new ClampStep("temperature"),

                new ApplyGravityStep("velocity", gravity),

                // Step 1: ADVECTION - Transport quantities along velocity field
                new AdvectFieldStep("velocity", "fuel"),
                new AdvectFieldStep("velocity", "temperature"),
                new AdvectFieldStep("velocity", "smoke"),
                
                // Step 2: DIFFUSION - Viscous and thermal diffusion
                new DiffuseStep("velocity", diffuseIterations),
                new DiffuseStep("fuel", 10),
                new GaussianBlurStep("temperature", 1, 16),
                new DiffuseStep("smoke", diffuseIterations),

                // Step 3: EXTERNAL FORCES - Applied via AddForce() calls

                // Step 4: PROJECTION - Make velocity field divergence-free
                new ComputeDivergenceStep(),
                //new CombustionDivergenceStep("temperature", "divergence", "fuel", combustionPressure, ignitionTemperature),
                new ComputePressureStep("pressure", "divergence", pressureIterations),
                new BoundaryStep("pressure", BoundaryStep.BoundaryType.Pressure),
                new ProjectStep("velocity", "pressure"),
                new BoundaryStep("velocity", BoundaryStep.BoundaryType.Velocity),

                // Step 5: ADVECT VELOCITY
                new AdvectFieldStep("velocity", "velocity"),
                new BoundaryStep("velocity", BoundaryStep.BoundaryType.Velocity),

                // ========== ADDITIONAL FLUID EFFECTS ==========
                // new ComputeVorticityStep("vorticity", "velocity"),
                // new ApplyVorticityStep("vorticity", "velocity", _vorticityScale),
                
                // Combustion effects
                new IgnitionStep(fuelBurnTemperature, ignitionTemperature, minFuelThreshold),
                new SpreadFireStep(spreadFireIterations, ignitionTemperature, minFuelThreshold, "temperature", "fuel"),
                new ConsumeFuelState("fuel", "temperature", ignitionTemperature, fuelConsumptionRate),

                new AddSmokeStep("temperature", "fuel", "smoke", smokeEmissionRate, minFuelThreshold, ignitionTemperature),
                new ClampStep("smoke"),

                new RadianceStep("temperature", ambientTemperature, maxTemperature, coolingRate),
                new BuoyancyStep("temperature", "velocity", ambientTemperature, buoyancyConstant, gravity),

                new VelocityDampingStep("velocity", velocityDampingCoefficient),
                new ClampVelocityStep("velocity"),

                // Example sprite-based obstacle rendering:
                // var spriteObstacleStep = new DrawSpritesToObstacleStep("spriteObstacle", true);
                // spriteObstacleStep.AddSprite(mySprite, new Vector2(100, 100), 1.0f);
                // Add spriteObstacleStep to simulation steps to render sprites as obstacles
            };
        }

        public void SetRenderTarget(RenderTarget2D target)
        {
            _graphicsDevice.SetRenderTarget(target);
        }

        public void AddSprite(Sprite sprite, Vector2 position)
        {
            _spriteObstacleStep.AddSprite(new ObstacleSpriteData(sprite, position, 1.0f));
        }

        public void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content)
        {
            _fluidEffect = content.Load<Effect>("FluidEffect");
            _flameGradientTexture = content.Load<Texture2D>("images/flameGradient");
        }

        public void Update(GameTime gameTime)
        {
            _fluidEffect.Parameters["timeStep"].SetValue((float)gameTime.ElapsedGameTime.TotalSeconds);
            _fluidEffect.Parameters["diffusion"].SetValue(_diffusion);
            _fluidEffect.Parameters["texelSize"].SetValue(new Vector2(1.0f / _gridSize, 1.0f / _gridSize));
            _fluidEffect.Parameters["sourceStrength"].SetValue(_sourceStrength);

            // Set obstacle texture before simulation steps
            _fluidEffect.Parameters["obstacleTexture"].SetValue(_renderTargetProvider.GetCurrent("obstacle"));

            foreach (var step in _simulationSteps)
            {
                step.Execute(_graphicsDevice, _gridSize, _fluidEffect, _renderTargetProvider, (float)gameTime.ElapsedGameTime.TotalSeconds);
            }

            _fluidEffect.Parameters["temperatureTexture"].SetValue(_renderTargetProvider.GetCurrent("temperature"));
        }

        public void AddForce(Vector2 position, Vector2 force, float radius)
        {
            var scaledAmount = force * _forceStrength;

            var velocityRT = _renderTargetProvider.GetCurrent("velocity");
            var tempVelocityRT = _renderTargetProvider.GetTemp("velocity");

            _fluidEffect.Parameters["sourceTexture"].SetValue(velocityRT);
            _fluidEffect.Parameters["cursorPosition"].SetValue(position);
            _fluidEffect.Parameters["cursorValue"].SetValue(scaledAmount);
            _fluidEffect.Parameters["radius"].SetValue(radius);

            _graphicsDevice.SetRenderTarget(tempVelocityRT);

            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["AddValue"];

            _fluidEffect.CurrentTechnique.Passes[0].Apply();
            Utils.Utils.DrawFullScreenQuad(_graphicsDevice, _gridSize);


            _graphicsDevice.SetRenderTarget(null);

            _renderTargetProvider.Swap("velocity");
        }

        public void SetForce(Vector2 position, Vector2 force, float radius)
        {
            var scaledAmount = force * _forceStrength;

            _fluidEffect.Parameters["sourceTexture"].SetValue(_velocityRT);
            _fluidEffect.Parameters["cursorPosition"].SetValue(position);
            _fluidEffect.Parameters["cursorValue"].SetValue(scaledAmount);
            _fluidEffect.Parameters["radius"].SetValue(radius);

            _graphicsDevice.SetRenderTarget(_renderTargetProvider.GetTemp("velocity"));

            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["SetValue"];

            _fluidEffect.CurrentTechnique.Passes[0].Apply();
            Utils.Utils.DrawFullScreenQuad(_graphicsDevice, _gridSize);


            _graphicsDevice.SetRenderTarget(null);

            _renderTargetProvider.Swap("velocity");
        }

        public void SetObstacle(Vector2 position, float radius)
        {
            _graphicsDevice.SetRenderTarget(_renderTargetProvider.GetTemp("obstacle"));
            _fluidEffect.Parameters["sourceTexture"].SetValue(_renderTargetProvider.GetCurrent("obstacle"));
            _fluidEffect.Parameters["cursorPosition"].SetValue(position);
            _fluidEffect.Parameters["cursorValue"].SetValue(100.0f);
            _fluidEffect.Parameters["radius"].SetValue(radius);

            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["SetValue"];

            _fluidEffect.CurrentTechnique.Passes[0].Apply();
            Utils.Utils.DrawFullScreenQuad(_graphicsDevice, _gridSize);

            _graphicsDevice.SetRenderTarget(null);

            _renderTargetProvider.Swap("obstacle");

            _fluidEffect.Parameters["obstacleTexture"].SetValue(_renderTargetProvider.GetCurrent("obstacle"));
        }

        public void AddFuel(Vector2 position, float amount, float radius)
        {
            float scaledAmount = amount * _sourceStrength;

            var fuelRT = _renderTargetProvider.GetCurrent("fuel");
            var tempFuelRT = _renderTargetProvider.GetTemp("fuel");

            _fluidEffect.Parameters["sourceTexture"].SetValue(fuelRT);
            _fluidEffect.Parameters["cursorPosition"].SetValue(position);
            _fluidEffect.Parameters["cursorValue"].SetValue(new Vector2(scaledAmount, 0));
            _fluidEffect.Parameters["radius"].SetValue(radius);

            _graphicsDevice.SetRenderTarget(tempFuelRT);
            _graphicsDevice.Clear(Color.Black);

            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["AddValue"];

            _fluidEffect.CurrentTechnique.Passes[0].Apply();
            Utils.Utils.DrawFullScreenQuad(_graphicsDevice, _gridSize);

            _graphicsDevice.SetRenderTarget(null);

            _renderTargetProvider.Swap("fuel");
        }

        public void AddTemperature(Vector2 position, float amount, float radius)
        {
            float scaledAmount = amount * _sourceStrength;

            var temperatureRT = _renderTargetProvider.GetCurrent("temperature");
            var tempTemperatureRT = _renderTargetProvider.GetTemp("temperature");

            _fluidEffect.Parameters["sourceTexture"].SetValue(temperatureRT);
            _fluidEffect.Parameters["cursorPosition"].SetValue(position);
            _fluidEffect.Parameters["cursorValue"].SetValue(new Vector2(scaledAmount, 0));
            _fluidEffect.Parameters["radius"].SetValue(radius);

            _graphicsDevice.SetRenderTarget(tempTemperatureRT);
            _graphicsDevice.Clear(Color.Black);

            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["AddValue"];

            _fluidEffect.CurrentTechnique.Passes[0].Apply();
            Utils.Utils.DrawFullScreenQuad(_graphicsDevice, _gridSize);

            _graphicsDevice.SetRenderTarget(null);

            _renderTargetProvider.Swap("temperature");
        }

        public void Draw(RenderTarget2D renderTarget)
        {
            _graphicsDevice.SetRenderTarget(renderTarget);

            if (_fluidEffect == null)
                throw new InvalidOperationException("FluidEffect must be loaded before drawing.");

            _fluidEffect.Parameters["renderTargetSize"].SetValue(new Vector2(_gridSize, _gridSize));

            if (_fluidEffect.Parameters["fuelTexture"] != null)
                _fluidEffect.Parameters["fuelTexture"].SetValue(_renderTargetProvider.GetCurrent("fuel"));

            if (_fluidEffect.Parameters["temperatureTexture"] != null)
                _fluidEffect.Parameters["temperatureTexture"].SetValue(_renderTargetProvider.GetCurrent("temperature"));

            if (_fluidEffect.Parameters["pressureTexture"] != null)
                _fluidEffect.Parameters["pressureTexture"].SetValue(_renderTargetProvider.GetCurrent("pressure"));

            if (_fluidEffect.Parameters["spriteObstacleTexture"] != null)
                _fluidEffect.Parameters["spriteObstacleTexture"].SetValue(_renderTargetProvider.GetCurrent("spriteObstacle"));

            if (_fluidEffect.Parameters["smokeTexture"] != null)
                _fluidEffect.Parameters["smokeTexture"].SetValue(_renderTargetProvider.GetCurrent("smoke"));

            if (_fluidEffect.Parameters["flameGradientTexture"] != null)
                _fluidEffect.Parameters["flameGradientTexture"].SetValue(_flameGradientTexture);

            _fluidEffect.Parameters["ignitionTemperature"].SetValue(ignitionTemperature);
            _fluidEffect.Parameters["maxTemperature"].SetValue(maxTemperature);

            _fluidEffect.CurrentTechnique = _fluidEffect.Techniques["Visualize"];
            _fluidEffect.CurrentTechnique.Passes[0].Apply();

            Utils.Utils.DrawFullScreenQuad(_graphicsDevice, _gridSize);

            _graphicsDevice.SetRenderTarget(null);
        }


        /// <summary>
        /// Gets the render target provider for accessing any render target by name.
        /// </summary>
        public IRenderTargetProvider RenderTargetProvider => _renderTargetProvider;

        public void Dispose()
        {
            _velocityRT?.Dispose();
            _fuelRT?.Dispose();
            _pressureRT?.Dispose();
            _pressureRT2?.Dispose();
            _tempFuelRT?.Dispose();
            _tempVelocityRT?.Dispose();
            _temperatureRT?.Dispose();
            _tempTemperatureRT?.Dispose();
            _divergenceRT?.Dispose();
            _vorticityRT?.Dispose();
            _smokeRT?.Dispose();
            _tempSmokeRT?.Dispose();
            _obstacleRT?.Dispose();
            _tempObstacleRT?.Dispose();
            _spriteObstacleRT?.Dispose();
            _tempSpriteObstacleRT?.Dispose();
            _spriteBatch?.Dispose();
        }

        private void CreateRenderTargets()
        {
            _velocityRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Vector2, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _tempVelocityRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Vector2, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _fuelRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _pressureRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _pressureRT2 = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _tempFuelRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _divergenceRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _tempDivergenceRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _temperatureRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _tempTemperatureRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _vorticityRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Vector2, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _obstacleRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _tempObstacleRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _smokeRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _tempSmokeRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _spriteObstacleRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _tempSpriteObstacleRT = new RenderTarget2D(_graphicsDevice, _gridSize, _gridSize, false,
                SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }
    }
}
