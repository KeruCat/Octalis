using System.Runtime.InteropServices;
using Octalis.Vulkan.Base;
using Octalis.Vulkan.Core;
using Octalis.Vulkan.Logging;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Octalis.Vulkan.Validation;

internal static class VulkanInstancePreflight
{
    private const string DebugUtilsExt = "VK_EXT_debug_utils";
    private const int EnumerationAttempts = 3;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate Result EnumerateInstanceVersionDelegate(out uint apiVersion);

    internal static void Validate(VulkanInstanceOptions options, Vk vk)
    {
        if (options is null)
            throw new VulkanException("Options cannot be null.", "VulkanInstanceOptions");

        if (vk is null)
            throw new VulkanException("Vk API cannot be null.", "VulkanInstancePreflight");
        CheckApiVersion(options.Version);
        CheckDebug(options);

        uint supportedVersion = GetSupportedApiVersion(vk);
        uint requestedVersion = options.Version;

        Logger.Information(
            "VulkanInstancePreflight",
            $"Vulkan loader: {FormatVersion(supportedVersion)}, requested: {FormatVersion(requestedVersion)}.");

        if (requestedVersion > supportedVersion)
        {
            throw new VulkanException(
                $"Requested Vulkan API version {FormatVersion(requestedVersion)} " +
                $"is not supported by the loader. Maximum supported version is {FormatVersion(supportedVersion)}.",
                "VulkanInstanceOptions");
        }
        HashSet<string> availableLayers = LayerNames(vk);

        string[] missingLayers = [.. options.Layers.Where(name => !availableLayers.Contains(name))];

        if (missingLayers.Length > 0)
        {
            throw new VulkanException(
                $"Requested instance layers are not available: {string.Join(", ", missingLayers)}.",
                "vkEnumerateInstanceLayerProperties");
        }
        HashSet<string> availableExtensions = ExtensionNames(vk);

        foreach (string layer in options.Layers)
            availableExtensions.UnionWith(ExtensionNames(vk, layer));

        string[] missingExtensions = [.. options.Extensions.Where(name => !availableExtensions.Contains(name))];

        if (missingExtensions.Length > 0)
        {
            throw new VulkanException(
                $"Requested instance extensions are not available: {string.Join(", ", missingExtensions)}.",
                "vkEnumerateInstanceExtensionProperties");
        }

        Logger.Information(
            "VulkanInstancePreflight",
            $"Preflight passed. Layers: {options.Layers.Length}, extensions: {options.Extensions.Length}.");
    }

    private static void CheckApiVersion(Version32 version)
    {
        uint value = version;

        uint variant = (value >> 29) & 0x7;
        uint major = (value >> 22) & 0x7F;

        if (major < 1)
            throw new VulkanException($"Invalid Vulkan API version: {FormatVersion(value)}. Major must be >= 1.", "VulkanInstanceOptions");

        if (variant != 0)
            throw new VulkanException($"Vulkan API version variant must be 0 (received {variant}).", "VulkanInstanceOptions");
    }

    private static void CheckDebug(VulkanInstanceOptions options)
    {
        if (options.DebugCreateInfo is not { } dbg)
            return;

        if (dbg.SType != StructureType.DebugUtilsMessengerCreateInfoExt)
            throw new VulkanException($"DebugCreateInfo.SType must be DebugUtilsMessengerCreateInfoExt (received {dbg.SType}).", "VulkanInstanceOptions");

        if (dbg.PfnUserCallback == 0)
            throw new VulkanException("DebugCreateInfo.PfnUserCallback cannot be null.", "VulkanInstanceOptions");


        if (dbg.MessageSeverity == 0)
            Logger.Warning("VulkanInstanceOptions", "DebugCreateInfo.MessageSeverity = 0. No messages will be delivered.");


        if (dbg.MessageType == 0)
            Logger.Warning("VulkanInstanceOptions", "DebugCreateInfo.MessageType = 0. No messages will be delivered.");

        if (!options.Extensions.Contains(DebugUtilsExt, StringComparer.Ordinal))
            throw new VulkanException($"DebugCreateInfo requires {DebugUtilsExt} in Extensions.", "VulkanInstanceOptions");

    }

    private static unsafe uint GetSupportedApiVersion(Vk vk)
    {
        nint functionName = SilkMarshal.StringToPtr("vkEnumerateInstanceVersion");

        try
        {
            PfnVoidFunction function = vk.GetInstanceProcAddr(default, (byte*)functionName);

            nint functionAddress = (nint)function.Handle;

            if (functionAddress == 0)
            {
                uint vulkan10 = Vk.MakeVersion(1, 0, 0);
                return vulkan10;
            }

            var enumerate = Marshal.GetDelegateForFunctionPointer<EnumerateInstanceVersionDelegate>(functionAddress);

            VkCheck.Ok(enumerate(out uint version), "vkEnumerateInstanceVersion");

            return version;
        }
        finally
        {
            SilkMarshal.Free(functionName);
        }
    }

    private static unsafe HashSet<string> LayerNames(Vk vk)
    {
        const string operation = "vkEnumerateInstanceLayerProperties";

        for (int attempt = 0; attempt < EnumerationAttempts; attempt++)
        {
            uint count = 0;
            VkCheck.Ok(vk.EnumerateInstanceLayerProperties(&count, null), operation);

            if (count == 0)
                return new HashSet<string>(StringComparer.Ordinal);

            var properties = new LayerProperties[count];

            fixed (LayerProperties* pProperties = properties)
            {
                Result result = vk.EnumerateInstanceLayerProperties(&count, pProperties);

                if (result == Result.Incomplete)
                    continue;

                VkCheck.Ok(result, operation);

                var names = new HashSet<string>(StringComparer.Ordinal);

                for (uint i = 0; i < count; i++)
                {
                    string name = SilkMarshal.PtrToString((nint)pProperties[i].LayerName) ?? string.Empty;
                    names.Add(name);
                }
                return names;
            }
        }

        throw new VulkanException("The available instance layer list kept changing during enumeration.", operation);
    }

    private static unsafe HashSet<string> ExtensionNames(Vk vk, string? layerName = null)
    {
        const string operation = "vkEnumerateInstanceExtensionProperties";
        nint layerNamePtr = 0;

        try
        {
            byte* pLayerName = null;

            if (layerName is not null)
            {
                layerNamePtr = SilkMarshal.StringToPtr(layerName);
                pLayerName = (byte*)layerNamePtr;
            }
            for (int attempt = 0; attempt < EnumerationAttempts; attempt++)
            {
                uint count = 0;

                VkCheck.Ok(vk.EnumerateInstanceExtensionProperties(pLayerName, &count, null), operation);
                if (count == 0)
                    return new HashSet<string>(StringComparer.Ordinal);

                var properties = new ExtensionProperties[count];

                fixed (ExtensionProperties* pProperties = properties)
                {
                    Result result = vk.EnumerateInstanceExtensionProperties(pLayerName, &count, pProperties);

                    if (result == Result.Incomplete)
                        continue;
                    VkCheck.Ok(result, operation);
                    var names = new HashSet<string>(StringComparer.Ordinal);

                    for (uint i = 0; i < count; i++)
                    {
                        string name = SilkMarshal.PtrToString((nint)pProperties[i].ExtensionName) ?? string.Empty;
                        names.Add(name);
                    }
                    return names;
                }
            }

            throw new VulkanException(
                $"The available instance extension list kept changing during enumeration" +
                (layerName is null ? "." : $" for layer '{layerName}'."),
                operation);
        }
        finally
        {
            if (layerNamePtr != 0)
                SilkMarshal.Free(layerNamePtr);
        }
    }

    private static string FormatVersion(uint version)
    {
        uint variant = (version >> 29) & 0x7;
        uint major = (version >> 22) & 0x7F;
        uint minor = (version >> 12) & 0x3FF;
        uint patch = version & 0xFFF;

        return variant == 0
            ? $"{major}.{minor}.{patch}"
            : $"{variant}:{major}.{minor}.{patch}";
    }
}