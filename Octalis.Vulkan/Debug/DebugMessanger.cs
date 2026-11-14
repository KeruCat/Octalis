using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Octalis.Vulkan.Base;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;

namespace Octalis.Vulkan.Debug;

internal sealed unsafe class DebugMessenger : VulkanObj
{
    private static readonly PfnDebugUtilsMessengerCallbackEXT Callback = new(&OnMessage);
    private ExtDebugUtils? _debugUtils;
    private Instance _instance;
    private DebugUtilsMessengerEXT _handle;
    public DebugMessenger()
    {
    }
    public static DebugUtilsMessengerCreateInfoEXT CreateInfo() => new()
    {
        SType = StructureType.DebugUtilsMessengerCreateInfoExt,
        MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.WarningBitExt
                        | DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt,
        MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt
                    | DebugUtilsMessageTypeFlagsEXT.ValidationBitExt
                    | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt,
        PfnUserCallback = Callback,
    };

    public void Create(ExtDebugUtils debugUtils, Instance instance)
    {
        _debugUtils = debugUtils;
        _instance = instance;

        var createInfo = CreateInfo();
        DebugUtilsMessengerEXT handle = default;
        VkCheck.Ok(debugUtils.CreateDebugUtilsMessenger(instance, &createInfo, null, &handle),
            "vkCreateDebugUtilsMessengerEXT");
        _handle = handle;
    }
    internal override void DisposeRes()
    {
        if (_debugUtils is not null)
        {
            if (_handle.Handle != 0)
                _debugUtils.DestroyDebugUtilsMessenger(_instance, _handle, null);
            _debugUtils.Dispose();
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static Bool32 OnMessage(
        DebugUtilsMessageSeverityFlagsEXT severity,
        DebugUtilsMessageTypeFlagsEXT types,
        DebugUtilsMessengerCallbackDataEXT* data,
        void* userData)
    {
        try
        {
            var text = SilkMarshal.PtrToString((nint)data->PMessage) ?? "(пустое сообщение)";
            Log(severity, types, text);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Vulkan debug callback failed: {exception}");
        }
        return false;
    }

    private static void Log(DebugUtilsMessageSeverityFlagsEXT severity, DebugUtilsMessageTypeFlagsEXT types, string text)
    {
        var level =
            (severity & DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt)   != 0 ? "Error" :
            (severity & DebugUtilsMessageSeverityFlagsEXT.WarningBitExt) != 0 ? "Warning" :
            (severity & DebugUtilsMessageSeverityFlagsEXT.InfoBitExt)    != 0 ? "Information" :
            "Debug";
        if (severity.HasFlag(
            DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt)
            && Debugger.IsAttached)
        {
            Debugger.Break();
        }
        Console.Error.WriteLine($"[{level}]\n\t[Vulkan {types}] {text}");
    }
}
