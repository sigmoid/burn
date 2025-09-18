namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;
using Peridot;
using Microsoft.Xna.Framework;

public class ComputeDivergenceStep : IFluidSimulationStep
{
    private Effect _effect;
    private string shaderPath = "shaders/fluid-simulation/compute-divergence";

    public ComputeDivergenceStep()
    {
        _effect = Core.Content.Load<Effect>(shaderPath);
    }
    public void Execute(GraphicsDevice device, int gridSize, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        var velocityRT = renderTargetProvider.GetCurrent("velocity");
        var divergenceRT = renderTargetProvider.GetCurrent("divergence");

        device.SetRenderTarget(divergenceRT);

        _effect.Parameters["renderTargetSize"].SetValue(new Vector2(gridSize, gridSize));
        _effect.Parameters["texelSize"].SetValue(new Vector2(1f / gridSize, 1f / gridSize));
        _effect.Parameters["velocityTexture"].SetValue(velocityRT);
        _effect.CurrentTechnique = _effect.Techniques["ComputeDivergence"];
        _effect.CurrentTechnique.Passes[0].Apply();

        Utils.DrawFullScreenQuad(device, gridSize);
        device.SetRenderTarget(null);
    }
}
