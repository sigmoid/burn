namespace burn.FluidSimulation.Steps;

using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;

public class ProjectStep : IFluidSimulationStep
{
    private readonly string _velocityName;
    private readonly string _pressureName;

    public ProjectStep(string velocityName, string pressureName)
    {
        _velocityName = velocityName;
        _pressureName = pressureName;
    }

    public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
    {
        var velocityRT = renderTargetProvider.GetCurrent(_velocityName);
        var pressureRT = renderTargetProvider.GetCurrent(_pressureName);

        device.SetRenderTarget(null);
        device.SetRenderTarget(velocityRT);

        effect.Parameters["velocityTexture"].SetValue(velocityRT);
        effect.Parameters["pressureTexture"].SetValue(pressureRT);

        effect.CurrentTechnique = effect.Techniques["Project"]; 

        effect.CurrentTechnique.Passes[0].Apply();
        Utils.DrawFullScreenQuad(device, gridSize);
        
        device.SetRenderTarget(null);
    }
}