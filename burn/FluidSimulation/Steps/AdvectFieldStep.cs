namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;
using Microsoft.Xna.Framework;

public class AdvectFieldStep : IFluidSimulationStep
{
    private readonly string _velocityName;
    private readonly string _sourceName;

    public AdvectFieldStep(string velocityName, string sourceName)
    {
        _velocityName = velocityName;
        _sourceName = sourceName;
    }

    public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        var source = renderTargetProvider.GetCurrent(_sourceName);
        var destination = renderTargetProvider.GetTemp(_sourceName);
        device.SetRenderTarget(destination);

        var currentVelocity = renderTargetProvider.GetCurrent("velocity");
        effect.Parameters["velocityTexture"].SetValue(currentVelocity);
        effect.Parameters["sourceTexture"].SetValue(source);

        effect.CurrentTechnique = effect.Techniques["Advect"];

        effect.CurrentTechnique.Passes[0].Apply();
        Utils.DrawFullScreenQuad(device, gridSize);

        device.SetRenderTarget(null);
        renderTargetProvider.Swap(_sourceName);
    }
}