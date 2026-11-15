using Octalis.Vulkan.Base;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;

namespace Octalis.Vulkan.Debug;

internal sealed unsafe class DebugMessenger : VulkanObj
{
    private readonly ExtDebugUtils _debugUtils;
    private readonly Instance _instance;
    private readonly DebugUtilsMessengerEXT _handle;
    private readonly DebugUtilsMessengerCreateInfoEXT _createInfo;

    internal DebugMessenger(DebugMessangerOptions options)
    {
        _debugUtils = options.DebugUtils;
        _instance = options.Instance;
        _createInfo = options.CreateInfo;
        DebugUtilsMessengerEXT handle;
        fixed (DebugUtilsMessengerCreateInfoEXT* pCreateInfo = &_createInfo)
            VkCheck.Ok(_debugUtils.CreateDebugUtilsMessenger(_instance, pCreateInfo, null, &handle), "vkCreateDebugUtilsMessengerEXT");
        _handle = handle;
    }

    internal override void DisposeRes()
    {
        if (_handle.Handle != 0)
        {
            _debugUtils.DestroyDebugUtilsMessenger(_instance, _handle, null);
        }
        _debugUtils.Dispose();
    }
}
internal sealed record class DebugMessangerOptions
{
    internal required ExtDebugUtils DebugUtils { get; init; }
    internal required Instance Instance { get; init; }
    internal required DebugUtilsMessengerCreateInfoEXT CreateInfo { get; init; }
}