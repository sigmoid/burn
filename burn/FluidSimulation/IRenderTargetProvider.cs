using Microsoft.Xna.Framework.Graphics;

namespace burn.FluidSimulation
{
    public interface IRenderTargetProvider
    {
        RenderTarget2D GetCurrent(string name);
        RenderTarget2D GetTemp(string name);
        void Swap(string name);
    }
}
