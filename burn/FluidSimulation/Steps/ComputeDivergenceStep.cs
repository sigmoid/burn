namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;

public class ComputeDivergenceStep : IFluidSimulationStep
{
    public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        var velocityRT = renderTargetProvider.GetCurrent("velocity");
        var divergenceRT = renderTargetProvider.GetCurrent("divergence");

        device.SetRenderTarget(null);
        device.SetRenderTarget(divergenceRT);

        effect.Parameters["velocityTexture"].SetValue(velocityRT);
        effect.CurrentTechnique = effect.Techniques["ComputeDivergence"];
        effect.CurrentTechnique.Passes[0].Apply();

        Utils.DrawFullScreenQuad(device, gridSize);
        device.SetRenderTarget(null);
    }
}
