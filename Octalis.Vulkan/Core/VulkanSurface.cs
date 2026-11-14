using Octalis.Vulkan.Base;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;

namespace Octalis.Vulkan.Core;
internal class VulkanSurface : VulkanObj
{
    private readonly KhrSurface _khrSurface;
    private readonly SurfaceKHR _surfacekhr;
    private readonly Instance _instance;
    internal KhrSurface KhrSurface { get => _khrSurface; }
    internal SurfaceKHR Surface    { get => _surfacekhr; }

    internal unsafe VulkanSurface(Instance instance, IWindow window, Vk vk)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (!vk.TryGetInstanceExtension(instance, out KhrSurface khrSurface))
            throw new VulkanException("failed to create khrSurface extension", "VulkanWindow.Surface.KHRSurface");
        var vkSurface = window.VkSurface 
         ?? throw new VulkanException("window created without Vulkan support", "VulkanWindow.Surface.SurfaceKHR");
        _surfacekhr = vkSurface.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();

        _khrSurface = khrSurface;
        _instance   = instance;
    }
    internal override unsafe void DisposeRes()
    {
        try {
            _khrSurface.DestroySurface(_instance, _surfacekhr, null);
        }
        finally {
            _khrSurface.Dispose();
        }
    }
}