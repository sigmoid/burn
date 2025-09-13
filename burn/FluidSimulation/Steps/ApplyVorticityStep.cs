namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;
using Microsoft.Xna.Framework;

public class ApplyVorticityStep : IFluidSimulationStep
{
    private readonly string _velocityName;
    private readonly string _vorticityName;
    private readonly float _vorticityScale;

    public ApplyVorticityStep(string vorticityName, string velocityName, float vorticityScale)
    {
        _velocityName = velocityName;
        _vorticityName = vorticityName;
        _vorticityScale = vorticityScale;
    }

    public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        var vorticityRT = renderTargetProvider.GetCurrent("vorticity");
        var velocityRT = renderTargetProvider.GetCurrent("velocity");
        var tempVelocityRT = renderTargetProvider.GetTemp("velocity");

        device.SetRenderTarget(tempVelocityRT);
        effect.Parameters["velocityTexture"].SetValue(velocityRT);
        effect.Parameters["vorticityTexture"].SetValue(vorticityRT);
        effect.Parameters["vorticityScale"].SetValue(_vorticityScale);
        effect.Parameters["timeStep"].SetValue(deltaTime);
        effect.Parameters["texelSize"].SetValue(new Vector2(1f / gridSize, 1f / gridSize));
        effect.CurrentTechnique = effect.Techniques["VorticityConfinement"];
        effect.CurrentTechnique.Passes[0].Apply();
        Utils.DrawFullScreenQuad(device, gridSize);
        device.SetRenderTarget(null);

        renderTargetProvider.Swap("velocity");
    }
}