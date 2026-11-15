using Octalis.Vulkan.Base;
using Silk.NET.Vulkan;

namespace Octalis.Vulkan.Debug;

internal sealed class VulkanDebugBootstrap : VulkanObj
{
    internal DebugUtilsMessengerCreateInfoEXT CreateInfo { get; }

    internal VulkanDebugBootstrap(VulkanDebugOptions options)
    {
        CreateInfo = new DebugUtilsMessengerCreateInfoEXT
        {
            SType = StructureType.DebugUtilsMessengerCreateInfoExt,
            MessageSeverity = options.Severity,
            MessageType = options.Types,
            PfnUserCallback = VulkanLogger.Callback
        };
    }
}
internal sealed record class VulkanDebugOptions
{
    public DebugUtilsMessageSeverityFlagsEXT Severity { get; init; }
        = DebugUtilsMessageSeverityFlagsEXT.WarningBitExt
        | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;

    public DebugUtilsMessageTypeFlagsEXT Types { get; init; }
        = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt
        | DebugUtilsMessageTypeFlagsEXT.ValidationBitExt
        | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt;
}