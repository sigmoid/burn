namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;
using Microsoft.Xna.Framework;

public class ComputePressureStep : IFluidSimulationStep
{
    private int iterations;

    private string pressureTarget;
    private string divergenceTarget;

    public ComputePressureStep(string pressureTarget, string divergenceTarget, int iterations)
    {
        this.pressureTarget = pressureTarget;
        this.divergenceTarget = divergenceTarget;
        this.iterations = iterations;
    }

    public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        var divergence = renderTargetProvider.GetCurrent(divergenceTarget);
        effect.Parameters["divergenceTexture"].SetValue(divergence);

        var renderTarget = renderTargetProvider.GetTemp(pressureTarget);
        device.SetRenderTarget(renderTarget);
        device.Clear(Color.Black);

        var tempRenderTarget = renderTargetProvider.GetTemp(pressureTarget);
        device.SetRenderTarget(tempRenderTarget);
        device.Clear(Color.Black);

        device.SetRenderTarget(null);

        for (int i = 0; i < iterations; i++)
        {
            var read = renderTargetProvider.GetCurrent(pressureTarget);
            var write = renderTargetProvider.GetTemp(pressureTarget);
            device.SetRenderTarget(write);
            effect.Parameters["sourceTexture"].SetValue(read);
            effect.CurrentTechnique = effect.Techniques["JacobiPressure"];
            effect.CurrentTechnique.Passes[0].Apply();
            Utils.DrawFullScreenQuad(device, gridSize);

            renderTargetProvider.Swap(pressureTarget);
        }

        if (iterations % 2 != 0)
        {
            renderTargetProvider.Swap(pressureTarget);
        }
    }
}