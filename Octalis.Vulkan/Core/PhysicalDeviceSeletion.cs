using Silk.NET.Vulkan;

namespace Octalis.Vulkan.Core;

internal readonly record struct QueueFamilies(uint Graphics, uint Compute, uint Transfer, uint? Present)
{
    internal bool HasDedicatedCompute => Compute != Graphics;
    internal bool HasDedicatedTransfer => Transfer != Graphics && Transfer != Compute;

    internal uint[] Distinct()
    {
        uint[] all = Present is { } present
            ? [Graphics, Compute, Transfer, present]
            : [Graphics, Compute, Transfer];

        return [.. all.Distinct().Order()];
    }
}

internal sealed record PhysicalDeviceSelection(
    PhysicalDevice Device,
    PhysicalDeviceProperties Properties,
    PhysicalDeviceFeatures Features,
    QueueFamilies Queues);