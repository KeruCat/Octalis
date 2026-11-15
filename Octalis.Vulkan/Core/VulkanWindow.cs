using Octalis.Vulkan.Base;
using Silk.NET.Windowing;

namespace Octalis.Vulkan.Core;

internal sealed class VulkanWindow : VulkanObj
{
    private readonly IWindow _window;
    internal IWindow Window { get => _window; }
    internal event Action OnLoad { add => _window.Load += value; remove => _window.Load -= value; }
    internal event Action<double> OnUpdate { add => _window.Update += value; remove => _window.Update -= value; }
    internal event Action<double> OnRender { add => _window.Render += value; remove => _window.Render -= value; }
    internal event Action OnClose { add => _window.Closing += value; remove => _window.Closing -= value; }
    internal VulkanWindow(WindowOptions options)
    {
        _window = Silk.NET.Windowing.Window.Create(options);
    }
    internal override void DisposeRes()
    {
        _window.Dispose();
    }
}