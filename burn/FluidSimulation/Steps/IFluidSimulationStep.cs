using Microsoft.Xna.Framework.Graphics;
namespace burn.FluidSimulation.Steps;

public interface IFluidSimulationStep
{
    void Execute(GraphicsDevice device, int gridSize, IRenderTargetProvider renderTargetProvider, float deltaTime);
}