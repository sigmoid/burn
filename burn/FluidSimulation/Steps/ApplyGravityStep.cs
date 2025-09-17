namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;
using Microsoft.Xna.Framework;

public class ApplyGravityStep : IFluidSimulationStep
{
    private readonly string _velocityName;
    private readonly float _gravity;

    public ApplyGravityStep(string velocityName, float gravity)
    {
        _velocityName = velocityName;
        _gravity = gravity;
    }

    public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        var source = renderTargetProvider.GetCurrent(_velocityName);
        var destination = renderTargetProvider.GetTemp(_velocityName);
        device.SetRenderTarget(destination);

        var currentVelocity = renderTargetProvider.GetCurrent("velocity");
        effect.Parameters["velocityTexture"].SetValue(source);
        effect.Parameters["gravity"].SetValue(_gravity);

        effect.CurrentTechnique = effect.Techniques["ApplyGravity"];

        effect.CurrentTechnique.Passes[0].Apply();
        Utils.DrawFullScreenQuad(device, gridSize);

        device.SetRenderTarget(null);
        renderTargetProvider.Swap(_velocityName);
    }
}