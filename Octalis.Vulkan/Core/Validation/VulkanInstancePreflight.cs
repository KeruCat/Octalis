using Octalis.Vulkan.Core;
using Octalis.Vulkan.Logging;
using Silk.NET.Core;
using Silk.NET.Vulkan;

namespace Octalis.Vulkan.Base;

internal static class VkOptionsCheck
{
    private const string DebugUtilsExt = "VK_EXT_debug_utils";

    internal static void Validate(VulkanInstanceOptions options)
    {
        if (options is null)
            throw new VulkanException("Options cannot be null.", "VulkanInstanceOptions");

        CheckName(options.ApplicationName, nameof(options.ApplicationName));
        CheckName(options.EngineName, nameof(options.EngineName));
        CheckApiVersion(options.Version);
        CheckNames(options.Extensions, nameof(options.Extensions));
        CheckNames(options.Layers, nameof(options.Layers));
        CheckDebug(options);
    }

    private static void CheckName(string? name, string field)
    {
        if (name is null)
            throw new VulkanException($"{field} cannot be null.", "VulkanInstanceOptions");

        if (string.IsNullOrWhiteSpace(name))
            Logger.Warning("VulkanInstanceOptions", $"{field} is empty - Vulkan allows this, but the name will be omitted.");

        if (name.Contains('\0'))
            Logger.Warning("VulkanInstanceOptions", $"{field} contains '\\0' - the string will be truncated during marshaling.");
    }

    private static void CheckApiVersion(Version32 version)
    {
        uint v = version;
        uint variant = (v >> 29) & 0x7;
        uint major = (v >> 22) & 0x7F;

        if (major < 1)
            throw new VulkanException(
                $"Invalid Vulkan API version: {version}. Major must be >= 1.", "VulkanInstanceOptions");

        if (variant != 0)
            throw new VulkanException(
                $"ApiVersion must have variant = 0 (received {variant}).", "VulkanInstanceOptions");
    }

    private static void CheckNames(string[]? names, string field)
    {
        if (names is null)
            throw new VulkanException($"{field} cannot be null.", "VulkanInstanceOptions");

        var seen = new HashSet<string>();

        foreach (string? name in names)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new VulkanException($"{field} contains a null or empty name.", "VulkanInstanceOptions");

            if (name.Contains('\0'))
                throw new VulkanException($"{field}: name '{name}' contains '\\0'.", "VulkanInstanceOptions");

            if (name != name.Trim())
                Logger.Warning("VulkanInstanceOptions", $"{field}: name '{name}' has surrounding whitespace and may not match when searched.");

            if (name.Length >= 256)
                Logger.Warning("VulkanInstanceOptions", $"{field}: name '{name}' is longer than 255 characters and will not be found.");

            if (!seen.Add(name))
                Logger.Warning("VulkanInstanceOptions", $"{field}: duplicate '{name}'.");
        }
    }

    private static void CheckDebug(VulkanInstanceOptions options)
    {
        if (options.DebugCreateInfo is null)
            return;

        var dbg = options.DebugCreateInfo.Value;

        if (dbg.SType != StructureType.DebugUtilsMessengerCreateInfoExt)
            throw new VulkanException(
                $"DebugCreateInfo.SType must be DebugUtilsMessengerCreateInfoExt (received {dbg.SType}).", "VulkanInstanceOptions");

        if (dbg.PfnUserCallback == 0)
            throw new VulkanException("DebugCreateInfo.PfnUserCallback cannot be null.", "VulkanInstanceOptions");

        if (dbg.MessageSeverity == 0)
            Logger.Warning("VulkanInstanceOptions", "DebugCreateInfo.MessageSeverity = 0 - no messages will be delivered.");

        if (dbg.MessageType == 0)
            Logger.Warning("VulkanInstanceOptions", "DebugCreateInfo.MessageType = 0 - no messages will be delivered.");

        if (Array.IndexOf(options.Extensions, DebugUtilsExt) < 0)
            Logger.Warning("VulkanInstanceOptions", $"DebugCreateInfo is set without {DebugUtilsExt}.");
    }
}