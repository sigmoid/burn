namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;

public class ClampVelocityStep : IFluidSimulationStep
{
    private string _velocityName;
    public ClampVelocityStep(string velocityTextureName)
    {
        _velocityName = velocityTextureName;
    }
    public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        var velocityRT = renderTargetProvider.GetCurrent(_velocityName);
        var velocityTempRT = renderTargetProvider.GetTemp(_velocityName);

        device.SetRenderTarget(velocityTempRT);

        effect.Parameters["velocityTexture"].SetValue(velocityRT);
        effect.Parameters["timeStep"].SetValue(deltaTime);
        effect.CurrentTechnique = effect.Techniques["ClampVelocity"];
        effect.CurrentTechnique.Passes[0].Apply();

        Utils.DrawFullScreenQuad(device, gridSize);
        device.SetRenderTarget(null);
    }
}
