using Octalis.Vulkan.Base;
using Silk.NET.Windowing;

namespace Octalis.Vulkan.Core;
internal class VulkanWindow : VulkanObj
{
    private readonly IWindow _window;
    internal IWindow Window { get => _window; }
    internal VulkanWindow(WindowOptions options )
    {
        _window = Silk.NET.Windowing.Window.Create(options);
    }
    internal override void DisposeRes()
    {
        _window.Dispose();
    }
}