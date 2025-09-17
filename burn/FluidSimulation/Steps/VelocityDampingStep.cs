namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;
using Microsoft.Xna.Framework;

public class VelocityDampingStep : IFluidSimulationStep
{
    private readonly string _velocityName;
    private readonly float _dampingCoefficient;

    public VelocityDampingStep(string velocityName, float dampingCoefficient)
    {
        _velocityName = velocityName;
        _dampingCoefficient = dampingCoefficient;
    }

    public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        var source = renderTargetProvider.GetCurrent(_velocityName);
        var destination = renderTargetProvider.GetTemp(_velocityName);
        device.SetRenderTarget(destination);

        var currentVelocity = renderTargetProvider.GetCurrent("velocity");
        effect.Parameters["velocityTexture"].SetValue(currentVelocity);
        effect.Parameters["velocityDampingCoefficient"].SetValue(_dampingCoefficient);

        effect.CurrentTechnique = effect.Techniques["VelocityDamping"];

        effect.CurrentTechnique.Passes[0].Apply();
        Utils.DrawFullScreenQuad(device, gridSize);

        device.SetRenderTarget(null);
        renderTargetProvider.Swap(_velocityName);
    }
}