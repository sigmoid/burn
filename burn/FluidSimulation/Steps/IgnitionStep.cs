namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;
using Microsoft.Xna.Framework;

public class IgnitionStep : IFluidSimulationStep
{
    public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        var temperatureRT = renderTargetProvider.GetCurrent("temperature");
        var tempTemperatureRT = renderTargetProvider.GetTemp("temperature");
        var fuelRT = renderTargetProvider.GetCurrent("fuel");

        device.SetRenderTarget(null);
        device.SetRenderTarget(tempTemperatureRT);
        device.Clear(Color.Transparent);

        effect.Parameters["fuelTexture"].SetValue(fuelRT);
        effect.Parameters["temperatureTexture"].SetValue(temperatureRT);
        effect.CurrentTechnique = effect.Techniques["Ignition"];
        effect.CurrentTechnique.Passes[0].Apply();

        Utils.DrawFullScreenQuad(device, gridSize);

        device.SetRenderTarget(null);
        
        renderTargetProvider.Swap("temperature");
    }
}