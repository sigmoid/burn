namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;
using Microsoft.Xna.Framework;

public class IgnitionStep : IFluidSimulationStep
{
    private float _ignitionTemperature;
    private float _fuelBurnTemperature;
    public IgnitionStep(float fuelBurnTemperature, float ignitionTemperature)
    {
        _fuelBurnTemperature = fuelBurnTemperature;
        _ignitionTemperature = ignitionTemperature;
    }   

    public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        var temperatureRT = renderTargetProvider.GetCurrent("temperature");
        var tempTemperatureRT = renderTargetProvider.GetTemp("temperature");
        var fuelRT = renderTargetProvider.GetCurrent("fuel");

        device.SetRenderTarget(tempTemperatureRT);
        device.Clear(Color.Transparent);

        effect.CurrentTechnique = effect.Techniques["Ignition"];
        effect.Parameters["fuelTexture"].SetValue(fuelRT);
        effect.Parameters["temperatureTexture"].SetValue(temperatureRT);
        effect.Parameters["ignitionTemperature"].SetValue(_ignitionTemperature);
        effect.Parameters["fuelBurnTemperature"].SetValue(_fuelBurnTemperature);
        effect.Parameters["timeStep"].SetValue(deltaTime);  
        effect.CurrentTechnique.Passes[0].Apply();

        Utils.DrawFullScreenQuad(device, gridSize);

        device.SetRenderTarget(null);

        renderTargetProvider.Swap("temperature");
    }
}