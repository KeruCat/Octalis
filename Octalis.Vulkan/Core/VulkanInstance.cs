using Octalis.Vulkan.Base;
using Silk.NET.Vulkan;

namespace Octalis.Vulkan.Core;
internal class VulkanInstance : VulkanObj
{
    private readonly Vk vk;
    internal Vk Vk { get => vk;}
    internal VulkanInstance()
    {
        vk = Vk.GetApi();
    }
}