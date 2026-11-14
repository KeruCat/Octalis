using Octalis.Vulkan.Base;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Octalis.Vulkan.Core;
internal class VulkanInstance : VulkanObj
{ 
    private static readonly string[] _validationLayers = ["VK_LAYER_KHRONOS_validation", "VK_LAYER_LUNARG_monitor"];
    private readonly Vk _vk;
    private readonly Instance _instance;
    internal Vk Vk { get => _vk;}
    internal Instance Instance { get => _instance; }
    internal unsafe VulkanInstance(string[] extensions)
    {
        _vk = Vk.GetApi();
#if DEBUG
        HashSet<string> validationNames = LayerNames();
        List<string> validationlayers = СompatibilityСheck(validationNames, _validationLayers);
#else
        List<string> validationlayers = [];
#endif
        HashSet<string> extensionNames = ExtensionNames();
        List<string> extension = СompatibilityСheck(extensionNames, extensions);

        ApplicationInfo applicationInfo = new()
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)SilkMarshal.StringToPtr("Octalis"),
            ApplicationVersion = Vk.MakeVersion(0, 0, 1),
            PEngineName = (byte*)SilkMarshal.StringToPtr("Octalis.Vulkan"),
            EngineVersion = Vk.MakeVersion(0, 0, 1),
            ApiVersion = Vk.MakeVersion(1,2,0),
        };
        InstanceCreateInfo createInfo = new()
        {
            SType = StructureType.InstanceCreateInfo,
            PNext = null,
            PApplicationInfo = &applicationInfo,
            EnabledExtensionCount   = (uint)extension.Count,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extension),
            EnabledLayerCount   = (uint)validationlayers.Count,
            PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationlayers),
        };
        try{
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
        Console.WriteLine("mrew mrew");
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
                names.Add(SilkMarshal.PtrToString((nint)pProperties[i].LayerName)?? string.Empty);
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
                names.Add(SilkMarshal.PtrToString((nint)pProperties[i].ExtensionName)?? string.Empty);
        }
        return names;
    }
    internal static List<string> СompatibilityСheck(in HashSet<string> avaible, in string[] request)
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
    internal override unsafe void DisposeRes()
    {
        _vk.DestroyInstance(_instance, null);
        _vk.Dispose();
    }
}