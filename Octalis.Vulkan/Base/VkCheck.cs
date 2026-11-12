using Silk.NET.Vulkan;

namespace Octalis.Vulkan.Base;
internal static class VkCheck
{
    internal static void Ok(Result result, string operation)
    {
        if (result != Result.Success)
            throw new VulkanException($"{operation} failed: {result}", operation, (int)result);
    }
}
