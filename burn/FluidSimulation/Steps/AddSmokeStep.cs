namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;
using Microsoft.Xna.Framework;

public class AddSmokeStep : IFluidSimulationStep
{
    private string _temperatureName;
    private string _fuelName;
    private string _smokeName;
    private float _smokeEmissionRate;
    private float _minFuelThreshold;
    private float _ignitionTemperature;

    public AddSmokeStep(string temperatureName, string fuelName, string smokeName, float smokeEmissionRate, float minFuelThreshold, float ignitionTemperature)
    {
        _temperatureName = temperatureName;
        _fuelName = fuelName;
        _smokeName = smokeName;
        _smokeEmissionRate = smokeEmissionRate;
        _minFuelThreshold = minFuelThreshold;
        _ignitionTemperature = ignitionTemperature;
    }

    public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        var temperatureRT = renderTargetProvider.GetCurrent(_temperatureName);
        var fuelRT = renderTargetProvider.GetCurrent(_fuelName);
        var smokeRT = renderTargetProvider.GetCurrent(_smokeName);
        var tempSmokeRT = renderTargetProvider.GetTemp(_smokeName);

        device.SetRenderTarget(tempSmokeRT);

        effect.Parameters["temperatureTexture"].SetValue(temperatureRT);
        effect.Parameters["fuelTexture"].SetValue(fuelRT);
        effect.Parameters["smokeTexture"].SetValue(smokeRT);
        effect.Parameters["smokeEmissionRate"].SetValue(_smokeEmissionRate);
        effect.Parameters["timeStep"].SetValue(deltaTime);
        effect.Parameters["minFuelThreshold"].SetValue(_minFuelThreshold);
        effect.Parameters["ignitionTemperature"].SetValue(_ignitionTemperature);
        effect.CurrentTechnique = effect.Techniques["AddSmoke"];
        effect.CurrentTechnique.Passes[0].Apply();

        Utils.DrawFullScreenQuad(device, gridSize);

        device.SetRenderTarget(null);

        renderTargetProvider.Swap(_smokeName);
    }
}