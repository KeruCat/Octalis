using Octalis.Vulkan.Base;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Octalis.Vulkan.Core;
internal class VulkanInstance : VulkanObj
{ 
    private readonly Vk _vk;
    private readonly Instance _instance;
    internal Vk Vk { get => _vk;}
     
    internal unsafe VulkanInstance()
    {
        _vk = Vk.GetApi();

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
            EnabledExtensionCount = 0,
            PpEnabledExtensionNames = null,
            EnabledLayerCount = 0,
            PpEnabledLayerNames = null,
        };
        try{
            VkCheck.Ok(_vk.CreateInstance(&createInfo, null, out Instance instance), "vkCreateInstance");
            _instance = instance;
        }
        finally
        {
            SilkMarshal.Free((nint)applicationInfo.PApplicationName);
            SilkMarshal.Free((nint)applicationInfo.PEngineName);
        }
        Console.WriteLine("mrew mrew");
    }

    internal override unsafe void DisposeRes()
    {
        _vk.DestroyInstance(_instance, null);
        _vk.Dispose();
    }
}