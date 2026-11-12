namespace Octalis.Vulkan.Base;
public sealed class VulkanException(string message, string? operation = null, int? vkResultCode = null, Exception? inner = null) 
    : Exception(message, inner)
{
    public string? Operation { get; } = operation;
    public int? VkResultCode { get; } = vkResultCode;
}