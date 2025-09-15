using Microsoft.Xna.Framework.Graphics;
using burn.FluidSimulation.Utils;

namespace burn.FluidSimulation.Steps
{
    /// <summary>
    /// Applies a separated Gaussian blur to a render target using variable kernel sizes.
    /// Performs horizontal blur followed by vertical blur for efficiency.
    /// </summary>
    public class GaussianBlurStep : IFluidSimulationStep
    {
        private readonly string _targetName;
        private readonly float _blurRadius;
        private readonly int _kernelSize;

        /// <summary>
        /// Creates a new Gaussian blur step.
        /// </summary>
        /// <param name="targetName">Name of the render target to blur</param>
        /// <param name="blurRadius">Blur radius multiplier (default: 1.0)</param>
        /// <param name="kernelSize">Kernel size (3-32, default: 9). Larger values = higher quality but slower performance</param>
        public GaussianBlurStep(string targetName, float blurRadius = 1.0f, int kernelSize = 9)
        {
            _targetName = targetName;
            _blurRadius = blurRadius;
            _kernelSize = kernelSize;
        }

        public void Execute(GraphicsDevice device, int gridSize, Effect effect, IRenderTargetProvider renderTargetProvider, float deltaTime)
        {
            // Set blur parameters
            effect.Parameters["blurRadius"].SetValue(_blurRadius);
            effect.Parameters["blurKernelSize"].SetValue(_kernelSize);

            // First pass: Horizontal blur
            var source = renderTargetProvider.GetCurrent(_targetName);
            var temp = renderTargetProvider.GetTemp(_targetName);

            device.SetRenderTarget(temp);
            effect.Parameters["sourceTexture"].SetValue(source);
            effect.CurrentTechnique = effect.Techniques["GaussianBlurHorizontal"];
            effect.CurrentTechnique.Passes[0].Apply();
            Utils.Utils.DrawFullScreenQuad(device, gridSize);
            device.SetRenderTarget(null);

            // Swap to make temp the new current
            renderTargetProvider.Swap(_targetName);

            // Second pass: Vertical blur
            var horizontalResult = renderTargetProvider.GetCurrent(_targetName);
            var finalResult = renderTargetProvider.GetTemp(_targetName);

            device.SetRenderTarget(finalResult);
            effect.Parameters["sourceTexture"].SetValue(horizontalResult);
            effect.CurrentTechnique = effect.Techniques["GaussianBlurVertical"];
            effect.CurrentTechnique.Passes[0].Apply();
            Utils.Utils.DrawFullScreenQuad(device, gridSize);
            device.SetRenderTarget(null);

            // Swap to make final result the current
            renderTargetProvider.Swap(_targetName);
        }
    }
}
