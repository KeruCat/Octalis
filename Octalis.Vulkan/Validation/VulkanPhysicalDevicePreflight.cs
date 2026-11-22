using Octalis.Vulkan.Base;
using Octalis.Vulkan.Core;
using Octalis.Vulkan.Logging;
using Silk.NET.Core;
using Silk.NET.Vulkan;

namespace Octalis.Vulkan.Validation;

internal static class VulkanPhysicalDevicePreflight
{
    private const string Tag = nameof(VulkanPhysicalDevicePreflight);
    private const string OptionsName = nameof(VulkanPhysicalDeviceOptions);

    internal static void Validate(
        VulkanPhysicalDeviceOptions options, Instance instance, Vk vk, VulkanSurface? surface)
    {
        if (options is null)
            throw new VulkanException("Options cannot be null.", OptionsName);

        if (vk is null)
            throw new VulkanException("Vk API cannot be null.", Tag);

        if (instance.Handle == 0)
            throw new VulkanException(
                "Instance handle is null. Was the instance created, and not already disposed?",
                "vkEnumeratePhysicalDevices");

        CheckPreference(options);
        CheckFeatures(options);
        CheckExtensions(options);
        CheckSurface(surface);

        Logger.Information(
            Tag,
            $"Preflight passed. Preferred GPU: {options.PreferredGpu}, fallback: {options.AllowFallbackToOtherType}, " +
            $"minimum API: {(Version)options.MinimumApiVersion}, " +
            $"required extensions: {options.RequiredExtensions.Length}, present: {surface is not null}.");
    }

    private static void CheckPreference(VulkanPhysicalDeviceOptions options)
    {
        if (!Enum.IsDefined(options.PreferredGpu))
            throw new VulkanException($"Unknown GPU preference: {options.PreferredGpu}.", OptionsName);

        if (options.PreferredGpu == GpuPreference.Any && !options.AllowFallbackToOtherType)
            Logger.Warning(Tag, "AllowFallbackToOtherType has no effect when PreferredGpu is Any.");
    }

    private static void CheckFeatures(VulkanPhysicalDeviceOptions options)
    {
        if (options.RequiredFeatures is null)
            throw new VulkanException("RequiredFeatures cannot be null.", OptionsName);
    }

    private static void CheckExtensions(VulkanPhysicalDeviceOptions options)
    {
        if (options.RequiredExtensions is null)
            throw new VulkanException("RequiredExtensions cannot be null.", OptionsName);

        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (string name in options.RequiredExtensions)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new VulkanException("RequiredExtensions contains an empty name.", OptionsName);

            if (!name.StartsWith("VK_", StringComparison.Ordinal))
                throw new VulkanException(
                    $"'{name}' is not a valid Vulkan extension name (expected a VK_ prefix).", OptionsName);

            if (!seen.Add(name))
                Logger.Warning(Tag, $"RequiredExtensions lists '{name}' more than once.");
        }
    }

    private static void CheckSurface(VulkanSurface? surface)
    {
        if (surface is null)
            return;

        if (surface.Surface.Handle == 0)
            throw new VulkanException(
                "Surface handle is null. Was the surface created, and not already disposed?",
                "vkGetPhysicalDeviceSurfaceSupportKHR");
    }
}