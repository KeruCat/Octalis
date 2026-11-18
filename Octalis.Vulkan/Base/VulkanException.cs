using Silk.NET.Vulkan;

namespace Octalis.Vulkan.Base;

public sealed class VulkanException(
    string message,
    string? operation = null,
    int? vkResultCode = null,
    Exception? inner = null)
    : Exception(message, inner)
{
    public string? Operation { get; } = operation;
    public int? VkResultCode { get; } = vkResultCode;
    public Result? VkResult => VkResultCode.HasValue ? (Result)VkResultCode.Value : null;
    public override string ToString()
    {
        var s = base.ToString();
        if (Operation is not null) s += $"{Environment.NewLine}Operation: {Operation}";
        if (VkResult is { } r)     s += $"{Environment.NewLine}VkResult: {r} ({VkResultCode})";
        return s;
    }
}