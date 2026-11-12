namespace Octalis.Vulkan.Base;
internal abstract class VulkanObj : IDisposable
{
    bool disposed = false;
    internal virtual void DisposeRes(){}
    public void Dispose()
    {
        if(disposed) return;
        DisposeRes();
        disposed = true;
        GC.SuppressFinalize(this);
    }
}