using Octalis.Vulkan.Base;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Octalis.Vulkan.Core;

internal sealed class VulkanInstance : VulkanObj
{
    private readonly Vk _vk;
    private readonly Instance _instance;
    internal Instance Instance { get => _instance; }
    internal unsafe VulkanInstance(VulkanInstanceOptions options , Vk vk)
    {
        _vk = vk;

        ApplicationInfo applicationInfo = new()
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)SilkMarshal.StringToPtr(options.ApplicationName),
            ApplicationVersion = options.ApplicationVersion,
            PEngineName = (byte*)SilkMarshal.StringToPtr(options.EngineName),
            EngineVersion = options.EngineVersion,
            ApiVersion = options.Version,
        };
        DebugUtilsMessengerCreateInfoEXT debugCreateInfo = options.DebugCreateInfo.GetValueOrDefault();
        InstanceCreateInfo createInfo = new()
        {
            SType = StructureType.InstanceCreateInfo,
            PNext = options.DebugCreateInfo.HasValue ? &debugCreateInfo : null,
            PApplicationInfo = &applicationInfo,
            EnabledExtensionCount = (uint)options.Extensions.Length,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(options.Extensions),
            EnabledLayerCount = (uint)options.Layers.Length,
            PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(options.Layers),
        };
        try
        {
            VkCheck.Ok(_vk.CreateInstance(&createInfo, null, out Instance instance), "vkCreateInstance");
            _instance = instance;
        }
        finally
        {
            SilkMarshal.Free((nint)applicationInfo.PApplicationName);
            SilkMarshal.Free((nint)applicationInfo.PEngineName);
            SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }
    }
    internal override unsafe void DisposeCore()
    {
        _vk.DestroyInstance(_instance, null);
    }
}
internal sealed record class VulkanInstanceOptions
{
    internal required string ApplicationName { get; init; }
    internal Version32 ApplicationVersion { get; init; } = Vk.MakeVersion(0, 1, 0);
    internal required string EngineName { get; init; }
    internal Version32 EngineVersion { get; init; } = Vk.MakeVersion(0, 1, 0);
    internal required string[] Extensions { get; init; }
    internal required string[] Layers { get; init; }
    internal Version32 Version { get; init; } = Vk.MakeVersion(1, 0, 0);
    internal DebugUtilsMessengerCreateInfoEXT? DebugCreateInfo { get; init; } = null;

}