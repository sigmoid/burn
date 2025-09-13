namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;
using Microsoft.Xna.Framework;

public class ClampStep : IFluidSimulationStep
{
    private readonly string _targetName;

    public ClampStep(string targetName)
    {
        _targetName = targetName;
    }

    public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        var source = renderTargetProvider.GetCurrent(_targetName);
        var destination = renderTargetProvider.GetTemp(_targetName);

        device.SetRenderTarget(null);
        device.SetRenderTarget(destination);

        device.Clear(Color.Transparent);

        effect.Parameters["sourceTexture"].SetValue(source);
        effect.CurrentTechnique = effect.Techniques["Clamp"];
        effect.CurrentTechnique.Passes[0].Apply();
        
        Utils.DrawFullScreenQuad(device, gridSize);

        device.SetRenderTarget(null);
        
        renderTargetProvider.Swap(_targetName);
    }
}