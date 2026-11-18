using Octalis.Vulkan.Base;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Octalis.Vulkan.Core;

internal sealed class VulkanInstance : VulkanObj
{
    private static readonly string[] _validationLayers = ["VK_LAYER_KHRONOS_validation", "VK_LAYER_LUNARG_monitor"];
    private readonly Vk _vk;
    private readonly Instance _instance;
    internal Vk Vk { get => _vk; }
    internal Instance Instance { get => _instance; }
    internal unsafe VulkanInstance(VulkanInstanceOptions options)
    {
        _vk = Vk.GetApi();

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
    internal unsafe HashSet<string> LayerNames()
    {
        uint count = 0;
        VkCheck.Ok(_vk.EnumerateInstanceLayerProperties(&count, null), "vkEnumerateInstanceLayerProperties");

        var properties = new LayerProperties[count];
        var names = new HashSet<string>();
        fixed (LayerProperties* pProperties = properties)
        {
            VkCheck.Ok(_vk.EnumerateInstanceLayerProperties(&count, pProperties),
                "vkEnumerateInstanceLayerProperties");
            for (var i = 0; i < count; i++)
                names.Add(SilkMarshal.PtrToString((nint)pProperties[i].LayerName) ?? string.Empty);
        }
        return names;
    }
    internal unsafe HashSet<string> ExtensionNames()
    {
        uint count = 0;
        VkCheck.Ok(_vk.EnumerateInstanceExtensionProperties((byte*)null, &count, null),
            "vkEnumerateInstanceExtensionProperties");

        var properties = new ExtensionProperties[count];
        var names = new HashSet<string>();
        fixed (ExtensionProperties* pProperties = properties)
        {
            VkCheck.Ok(_vk.EnumerateInstanceExtensionProperties((byte*)null, &count, pProperties),
                "vkEnumerateInstanceExtensionProperties");
            for (var i = 0; i < count; i++)
                names.Add(SilkMarshal.PtrToString((nint)pProperties[i].ExtensionName) ?? string.Empty);
        }
        return names;
    }
    internal static List<string> CompatibilityCheck(in HashSet<string> avaible, in string[] request)
    {
        List<string> output = [];
        foreach (string name in request)
        {
            if (avaible.Contains(name))
                output.Add(name);
            else
                throw new VulkanException($"The resulting {name} is not supported by the system.", "VulkanInstance");
        }
        return output;
    }
    internal override unsafe void DisposeCore()
    {
        _vk.DestroyInstance(_instance, null);
        _vk.Dispose();
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