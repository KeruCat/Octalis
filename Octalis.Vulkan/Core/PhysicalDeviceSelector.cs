using System.Runtime.InteropServices;
using Octalis.Vulkan.Base;
using Octalis.Vulkan.Logging;
using Octalis.Vulkan.Validation;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Octalis.Vulkan.Core;

internal static class PhysicalDeviceSelector
{
    private const string Tag = nameof(PhysicalDeviceSelector);
    private const string SwapchainExtension = "VK_KHR_swapchain";

    internal static PhysicalDeviceSelection Select(
        VulkanPhysicalDeviceOptions options, Instance instance, Vk vk, VulkanSurface? surface = null)
    {
        VulkanPhysicalDevicePreflight.Validate(options, instance, vk, surface);

        PhysicalDeviceSelection? best = null;
        long bestScore = long.MinValue;
        var rejected = new List<string>();

        foreach (PhysicalDevice device in vk.GetPhysicalDevices(instance))
        {
            if (TryEvaluate(device, options, vk, surface, out var selection, out long score, out string error))
            {
                if (score > bestScore)
                {
                    best = selection;
                    bestScore = score;
                }
            }
            else
            {
                rejected.Add(error);
                Logger.Warning(Tag, $"Skipping {error}");
            }
        }

        if (best is null)
        {
            string details = rejected.Count == 0
                ? "No Vulkan physical devices were found."
                : string.Join(Environment.NewLine, rejected);

            throw new VulkanException(
                $"No compatible Vulkan physical device was found.{Environment.NewLine}{details}",
                "vkEnumeratePhysicalDevices");
        }

        LogSelection(best, options);
        return best;
    }

    // ---------------------------------------------------------------- evaluation
    private static bool TryEvaluate(
        PhysicalDevice device,
        VulkanPhysicalDeviceOptions options,
        Vk vk,
        VulkanSurface? surface,
        out PhysicalDeviceSelection selection,
        out long score,
        out string error)
    {
        try
        {
            return EvaluateCore(device, options, vk, surface, out selection, out score, out error);
        }
        catch (VulkanException ex)
        {
            vk.GetPhysicalDeviceProperties(device, out PhysicalDeviceProperties props);
            selection = null!;
            score = 0;
            error = $"{GetDeviceName(props)}: {ex.Message}";
            return false;
        }
    }

    private static bool EvaluateCore(
        PhysicalDevice device,
        VulkanPhysicalDeviceOptions options,
        Vk vk,
        VulkanSurface? surface,
        out PhysicalDeviceSelection selection,
        out long score,
        out string error)
    {
        selection = null!;
        score = 0;
        error = string.Empty;

        vk.GetPhysicalDeviceProperties(device, out PhysicalDeviceProperties props);
        vk.GetPhysicalDeviceFeatures(device, out PhysicalDeviceFeatures features);
        vk.GetPhysicalDeviceMemoryProperties(device, out PhysicalDeviceMemoryProperties memory);
        ulong vram = GetDeviceLocalMemoryBytes(memory);

        var failures = new List<string>();

        uint minApi = options.MinimumApiVersion;
        if (props.ApiVersion < minApi)
            failures.Add($"Vulkan {props.ApiVersion} is below required {minApi}");

        if (!options.AllowFallbackToOtherType && !MatchesPreference(props.DeviceType, options.PreferredGpu))
            failures.Add($"GPU type {props.DeviceType} is not allowed");

        if (vram < options.MinimumDeviceLocalMemoryBytes)
            failures.Add($"device-local memory {vram:N0} bytes is below required {options.MinimumDeviceLocalMemoryBytes:N0}");

        string[] missingFeatures = GetMissingFeatures(options.RequiredFeatures, features);
        if (missingFeatures.Length > 0)
            failures.Add($"missing features: {string.Join(", ", missingFeatures)}");

        HashSet<string> supported = GetDeviceExtensions(vk, device);
        IEnumerable<string> required = surface is null
            ? options.RequiredExtensions
            : options.RequiredExtensions.Append(SwapchainExtension);
        string[] missingExtensions = [.. required.Distinct().Where(x => !supported.Contains(x))];
        if (missingExtensions.Length > 0)
            failures.Add($"missing extensions: {string.Join(", ", missingExtensions)}");

        if (!TryPickQueues(vk, device, surface, out QueueFamilies queues, out string queueError))
            failures.Add(queueError);

        if (failures.Count > 0)
        {
            error = $"{GetDeviceName(props)}: {string.Join("; ", failures)}";
            return false;
        }

        selection = new PhysicalDeviceSelection(device, props, features, queues);
        score = Score(props, vram, options.PreferredGpu);
        return true;
    }

    private static bool MatchesPreference(PhysicalDeviceType type, GpuPreference preference) => preference switch
    {
        GpuPreference.Discrete => type == PhysicalDeviceType.DiscreteGpu,
        GpuPreference.Integrated => type == PhysicalDeviceType.IntegratedGpu,
        _ => true
    };
    private static long Score(PhysicalDeviceProperties props, ulong vram, GpuPreference preference)
    {
        long score = props.DeviceType switch
        {
            PhysicalDeviceType.DiscreteGpu => 1_000,
            PhysicalDeviceType.IntegratedGpu => 500,
            PhysicalDeviceType.VirtualGpu => 100,
            _ => 0
        };

        if (preference != GpuPreference.Any && MatchesPreference(props.DeviceType, preference))
            score += 1_000_000;

        return score + (long)(vram >> 20); // MiB
    }

    private static string[] GetMissingFeatures(PhysicalDeviceFeatureRequirements r, PhysicalDeviceFeatures f)
    {
        (string Name, bool Required, bool Supported)[] checks =
        [
            (nameof(f.GeometryShader), r.GeometryShader, (bool)f.GeometryShader),
            (nameof(f.TessellationShader), r.TessellationShader, (bool)f.TessellationShader),
            (nameof(f.SamplerAnisotropy), r.SamplerAnisotropy, (bool)f.SamplerAnisotropy),
            (nameof(f.MultiViewport), r.MultiViewport, (bool)f.MultiViewport),
        ];

        return [.. checks.Where(c => c.Required && !c.Supported).Select(c => c.Name)];
    }

    private static ulong GetDeviceLocalMemoryBytes(PhysicalDeviceMemoryProperties memory)
    {
        ulong total = 0;
        for (int i = 0; i < memory.MemoryHeapCount; i++)
        {
            MemoryHeap heap = memory.MemoryHeaps[i];
            if ((heap.Flags & MemoryHeapFlags.DeviceLocalBit) != 0)
                total += heap.Size;
        }

        return total;
    }

    // ---------------------------------------------------------------- queues

    private static unsafe bool TryPickQueues(
        Vk vk, PhysicalDevice device, VulkanSurface? surface, out QueueFamilies queues, out string error)
    {
        queues = default;
        error = string.Empty;

        uint count = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(device, &count, null);
        if (count == 0)
        {
            error = "no queue families";
            return false;
        }

        var families = new QueueFamilyProperties[count];
        fixed (QueueFamilyProperties* pFamilies = families)
            vk.GetPhysicalDeviceQueueFamilyProperties(device, &count, pFamilies);

        var canPresent = new bool[families.Length];
        if (surface is not null)
        {
            for (int i = 0; i < families.Length; i++)
            {
                VkCheck.Ok(
                    surface.KhrSurface.GetPhysicalDeviceSurfaceSupport(device, (uint)i, surface.Surface, out Bool32 ok),
                    "vkGetPhysicalDeviceSurfaceSupportKHR");
                canPresent[i] = (bool)ok;
            }
        }

        bool Has(int i, QueueFlags flag) => (families[i].QueueFlags & flag) != 0;

        int First(Func<int, bool> match)
        {
            for (int i = 0; i < families.Length; i++)
            {
                if (families[i].QueueCount > 0 && match(i))
                    return i;
            }
            return -1;
        }

        int graphics = First(i => Has(i, QueueFlags.GraphicsBit) && canPresent[i]);
        if (graphics < 0)
            graphics = First(i => Has(i, QueueFlags.GraphicsBit));
        if (graphics < 0)
        {
            error = "no graphics-capable queue family";
            return false;
        }

        uint? present = null;
        if (surface is not null)
        {
            int p = canPresent[graphics] ? graphics : First(i => canPresent[i]);
            if (p < 0)
            {
                error = "no queue family supports presentation to the surface";
                return false;
            }
            present = (uint)p;
        }

        int compute = First(i => Has(i, QueueFlags.ComputeBit) && !Has(i, QueueFlags.GraphicsBit));
        if (compute < 0)
            compute = graphics;

        int transfer = First(i => Has(i, QueueFlags.TransferBit)
                                  && !Has(i, QueueFlags.GraphicsBit)
                                  && !Has(i, QueueFlags.ComputeBit));
        if (transfer < 0)
            transfer = compute;

        queues = new QueueFamilies((uint)graphics, (uint)compute, (uint)transfer, present);
        return true;
    }

    // ---------------------------------------------------------------- vulkan helpers

    private static unsafe HashSet<string> GetDeviceExtensions(Vk vk, PhysicalDevice device)
    {
        uint count = 0;
        VkCheck.Ok(
            vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &count, null),
            "vkEnumerateDeviceExtensionProperties");

        if (count == 0)
            return [];

        var items = new ExtensionProperties[checked((int)count)];
        fixed (ExtensionProperties* buffer = items)
        {
            VkCheck.Ok(
                vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &count, buffer),
                "vkEnumerateDeviceExtensionProperties");
        }

        return [.. items.Select(ExtensionName)];
    }

    private static unsafe string ExtensionName(ExtensionProperties properties) =>
        SilkMarshal.PtrToString((nint)properties.ExtensionName) ?? string.Empty;

    private static unsafe string GetDeviceName(PhysicalDeviceProperties properties) =>
        Marshal.PtrToStringUTF8((nint)properties.DeviceName) ?? "Unknown Vulkan device";

    // ---------------------------------------------------------------- logging

    private static void LogSelection(PhysicalDeviceSelection selected, VulkanPhysicalDeviceOptions options)
    {
        PhysicalDeviceProperties props = selected.Properties;
        QueueFamilies q = selected.Queues;

        Logger.Information(
            Tag,
            $"Selected {GetDeviceName(props)} ({props.DeviceType}), API {props.ApiVersion}. " +
            $"Queue families: graphics {q.Graphics}, compute {q.Compute}, transfer {q.Transfer}, " +
            $"present {(q.Present is { } p ? p.ToString() : "-")}.");

        if (!MatchesPreference(props.DeviceType, options.PreferredGpu))
            Logger.Warning(Tag, $"Selected a fallback GPU type: {props.DeviceType}.");

        if (q.Present is { } present && present != q.Graphics)
        {
            Logger.Warning(
                Tag,
                "Present uses a different queue family from Graphics. Swapchain image ownership transfers " +
                "or concurrent sharing may be required.");
        }
    }
}