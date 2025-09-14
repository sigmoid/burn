namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;
using Microsoft.Xna.Framework;

public class BuoyancyStep : IFluidSimulationStep
{
    private string _temperatureName;
    private string _velocityName;
    private float _ambientTemperature;
    private float _heatBuoyancyConstant;
    private float _gravity;
    
    public BuoyancyStep(string temperatureName, string velocityName, float ambientTemperature, float heatBuoyancyConstant, float gravity)
    {
        _temperatureName = temperatureName;
        _velocityName = velocityName;
        _ambientTemperature = ambientTemperature;
        _heatBuoyancyConstant = heatBuoyancyConstant;
        _gravity = gravity;
    }

    public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        var temperatureRT = renderTargetProvider.GetCurrent(_temperatureName);
        var velocityRT = renderTargetProvider.GetCurrent(_velocityName);
        var tempVelocityRT = renderTargetProvider.GetTemp(_velocityName);

        device.SetRenderTarget(tempVelocityRT);

        effect.Parameters["temperatureTexture"].SetValue(temperatureRT);
        effect.Parameters["velocityTexture"].SetValue(velocityRT);
        effect.Parameters["ambientTemperature"].SetValue(_ambientTemperature);
        effect.Parameters["heatBuoyancyConstant"].SetValue(_heatBuoyancyConstant);
        effect.Parameters["gravity"].SetValue(_gravity);
        effect.Parameters["timeStep"].SetValue(deltaTime);
        effect.CurrentTechnique = effect.Techniques["Buoyancy"];
        effect.CurrentTechnique.Passes[0].Apply();

        Utils.DrawFullScreenQuad(device, gridSize);

        device.SetRenderTarget(null);

        renderTargetProvider.Swap(_velocityName);
    }
}