namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;

public class RadianceStep : IFluidSimulationStep
{
    private readonly string _temperatureName;
    private readonly float _ambientTemperature;
    private readonly float _maxTemperature;
    private readonly float _coolingRate;

    public RadianceStep(string temperatureName, float ambientTemperature, float maxTemperature, float coolingRate)
    {
        _temperatureName = temperatureName;
        _ambientTemperature = ambientTemperature;
        _maxTemperature = maxTemperature;
        _coolingRate = coolingRate;
    }

    public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        var temperatureRT = renderTargetProvider.GetCurrent(_temperatureName);
        var temperatureTempRT = renderTargetProvider.GetTemp(_temperatureName);

        device.SetRenderTarget(temperatureTempRT);
        device.Clear(Color.Transparent);
        effect.Parameters["temperatureTexture"].SetValue(temperatureRT);
        effect.Parameters["ambientTemperature"].SetValue(_ambientTemperature);
        effect.Parameters["maxTemperature"].SetValue(_maxTemperature);
        effect.Parameters["timeStep"].SetValue(deltaTime);
        effect.Parameters["coolingRate"].SetValue(_coolingRate);
        effect.CurrentTechnique = effect.Techniques["Radiance"];
        effect.CurrentTechnique.Passes[0].Apply();
        Utils.DrawFullScreenQuad(device, gridSize);
        device.SetRenderTarget(null);

        renderTargetProvider.Swap(_temperatureName);
    }
}