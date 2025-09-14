namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;
using Microsoft.Xna.Framework;

public class SpreadFireStep : IFluidSimulationStep
{
    private readonly int _iterations;
    private readonly float _ignitionTemperature;
    private readonly float _minFuelThreshold;
    private readonly string _temperatureName;
    private readonly string _fuelName;

    public SpreadFireStep(int iterations, float ignitionTemperature, float minFuelThreshold, string temperatureName, string fuelName)
    {
        _iterations = iterations;
        _ignitionTemperature = ignitionTemperature;
        _minFuelThreshold = minFuelThreshold;
        _temperatureName = temperatureName;
        _fuelName = fuelName;
    }

    public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        for(int i = 0; i < _iterations; i++)
        {
            var temperatureRT = renderTargetProvider.GetCurrent("temperature");
            var tempTemperatureRT = renderTargetProvider.GetTemp("temperature");
            var fuelRT = renderTargetProvider.GetCurrent("fuel");

            device.SetRenderTarget(tempTemperatureRT);

            effect.Parameters["fuelTexture"].SetValue(fuelRT);
            effect.Parameters["temperatureTexture"].SetValue(temperatureRT);
            effect.Parameters["ignitionTemperature"].SetValue(_ignitionTemperature);
            effect.Parameters["minFuelThreshold"].SetValue(_minFuelThreshold);
            effect.Parameters["timeStep"].SetValue(deltaTime);
            effect.CurrentTechnique = effect.Techniques["SpreadFire"];
            effect.CurrentTechnique.Passes[0].Apply();

            Utils.DrawFullScreenQuad(device, gridSize);

            device.SetRenderTarget(null);

            renderTargetProvider.Swap("temperature");
        }

    }
}