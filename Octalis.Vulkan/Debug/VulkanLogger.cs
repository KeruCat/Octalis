using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Octalis.Vulkan.Base;
using Octalis.Vulkan.Logging;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Octalis.Vulkan.Debug;

internal static unsafe class VulkanLogger
{
    internal static readonly PfnDebugUtilsMessengerCallbackEXT Callback = new(&OnMessage);
    internal static bool BreakOnError = false;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static Bool32 OnMessage(
        DebugUtilsMessageSeverityFlagsEXT severity,
        DebugUtilsMessageTypeFlagsEXT type,
        DebugUtilsMessengerCallbackDataEXT* data,
        void* userData)
    {
        try
        {   
            string text = SilkMarshal.PtrToString((nint)data->PMessage) ?? string.Empty;
            Log(severity, type, text);
        }
        catch{}
        return false;
    }

    private static void Log(
        DebugUtilsMessageSeverityFlagsEXT severity,
        DebugUtilsMessageTypeFlagsEXT type,
        string text)
    {
        string message = $"[{type}] {text}";

        if ((severity & DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt) != 0)
        {
            Logger.Error("Vulkan", message);

            if (Debugger.IsAttached)
                Debugger.Break();
            return;
        }
        if ((severity & DebugUtilsMessageSeverityFlagsEXT.WarningBitExt) != 0)
        {
            Logger.Warning("Vulkan", message);
            return;
        }
        if ((severity & DebugUtilsMessageSeverityFlagsEXT.InfoBitExt) != 0)
        {
            Logger.Information("Vulkan", message);
            return;
        }
        Logger.Debug("Vulkan", message);
    }
}