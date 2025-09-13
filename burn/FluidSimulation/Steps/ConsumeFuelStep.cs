namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;
using Microsoft.Xna.Framework;

public class ConsumeFuelState : IFluidSimulationStep
{
    private readonly string _fuelName;
    private readonly string _temperatureName;
    private readonly float _ignitionTemperature;
    private readonly float _fuelConsumptionRate;

    public ConsumeFuelState(string fuelName, string temperatureName, float ignitionTemperature, float fuelConsumptionRate)
    {
        _fuelName = fuelName;
        _temperatureName = temperatureName;
        _ignitionTemperature = ignitionTemperature;
        _fuelConsumptionRate = fuelConsumptionRate;
    }


    public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        var tempFuelRT = renderTargetProvider.GetTemp(_fuelName);
        var temperatureRT = renderTargetProvider.GetCurrent(_temperatureName);
        var fuelRT = renderTargetProvider.GetCurrent(_fuelName);

        device.SetRenderTarget(tempFuelRT);
        device.Clear(Color.Transparent);
        effect.Parameters["temperatureTexture"].SetValue(temperatureRT);
        effect.Parameters["fuelTexture"].SetValue(fuelRT);
        effect.Parameters["ignitionTemperature"].SetValue(_ignitionTemperature);
        effect.Parameters["fuelConsumptionRate"].SetValue(_fuelConsumptionRate);
        effect.CurrentTechnique = effect.Techniques["ConsumeFuel"];
        effect.CurrentTechnique.Passes[0].Apply();
        Utils.DrawFullScreenQuad(device, gridSize);
        device.SetRenderTarget(null);
        
        renderTargetProvider.Swap(_fuelName);
    }
}