using Silk.NET.Core;
using Silk.NET.Vulkan;

namespace Octalis.Vulkan.Core;

internal enum GpuPreference
{
    Discrete,
    Integrated,
    Any
}

internal sealed record PhysicalDeviceFeatureRequirements
{
    internal bool GeometryShader { get; init; }
    internal bool TessellationShader { get; init; }
    internal bool SamplerAnisotropy { get; init; }
    internal bool MultiViewport { get; init; }
}

internal sealed record VulkanPhysicalDeviceOptions
{
    internal GpuPreference PreferredGpu { get; init; } = GpuPreference.Discrete;
    internal bool AllowFallbackToOtherType { get; init; } = true;

    internal Version32 MinimumApiVersion { get; init; } = Vk.MakeVersion(1, 0, 0);
    internal ulong MinimumDeviceLocalMemoryBytes { get; init; }

    internal string[] RequiredExtensions { get; init; } = [];
    internal PhysicalDeviceFeatureRequirements RequiredFeatures { get; init; } = new();
}