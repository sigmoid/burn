namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;
using Microsoft.Xna.Framework;

public class ComputeVorticityStep : IFluidSimulationStep
{
    private readonly string _velocityName;
    private readonly string _vorticityName;

    public ComputeVorticityStep(string vorticityName, string velocityName)
    {
        _velocityName = velocityName;
        _vorticityName = vorticityName;
    }

    public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        var vorticityRT = renderTargetProvider.GetCurrent("vorticity");
        var velocityRT = renderTargetProvider.GetCurrent("velocity");

        device.SetRenderTarget(vorticityRT);
        device.Clear(Color.Transparent);
        effect.Parameters["velocityTexture"].SetValue(velocityRT);
        effect.Parameters["texelSize"].SetValue(new Vector2(1f / gridSize, 1f / gridSize));
        effect.CurrentTechnique = effect.Techniques["ComputeVorticity"];
        effect.CurrentTechnique.Passes[0].Apply();

        Utils.DrawFullScreenQuad(device, gridSize);
        device.SetRenderTarget(null);
    }
}