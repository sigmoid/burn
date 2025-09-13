namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;

public class DiffuseStep : IFluidSimulationStep
{
    private readonly string _targetName;
    private readonly int _iterations;

    public DiffuseStep(string targetName, int iterations)
    {
        _targetName = targetName;
        _iterations = iterations;
    }

    public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        for (int i = 0; i < _iterations; i++)
        {
            var source = renderTargetProvider.GetCurrent(_targetName);
            var destination = renderTargetProvider.GetTemp(_targetName);
            
            device.SetRenderTarget(destination);

            effect.Parameters["sourceTexture"].SetValue(source);
            effect.CurrentTechnique = effect.Techniques["Diffuse"];
            effect.CurrentTechnique.Passes[0].Apply();

            Utils.DrawFullScreenQuad(device, gridSize);
            device.SetRenderTarget(null);
            
            renderTargetProvider.Swap(_targetName);
        }
    }
}
