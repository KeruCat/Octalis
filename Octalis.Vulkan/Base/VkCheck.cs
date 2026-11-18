using Silk.NET.Vulkan;

namespace Octalis.Vulkan.Base;

internal static class VkCheck
{
    internal static void Ok(Result result, string operation) => Ok(result, operation, acceptable: null);
    internal static void Ok(Result result, string operation, params Result[]? acceptable)
    {
        if (result == Result.Success)
            return;

        if (acceptable is not null)
            foreach (var r in acceptable)
                if (r == result)
                    return;

        throw new VulkanException(Format(result, operation), operation, (int)result);
    }
    private static string Format(Result result, string operation)
    {
        string hint = result switch
        {
            Result.ErrorLayerNotPresent => " (requested layer is not available)",
            Result.ErrorExtensionNotPresent => " (requested extension is not available)",
            Result.ErrorIncompatibleDriver => " (the driver does not support the requested version of Vulkan)",
            Result.ErrorOutOfHostMemory => " (host memory allocation failed)",
            Result.ErrorOutOfDeviceMemory => " (not enough video memory)",
            Result.ErrorInitializationFailed => " (device memory allocation failed)",
            _ => string.Empty
        };
        return $"{operation} failed: {result}{hint}";
    }
}