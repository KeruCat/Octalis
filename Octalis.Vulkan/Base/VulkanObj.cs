using System.Diagnostics;
using Octalis.Vulkan.Logging;

namespace Octalis.Vulkan.Base;

internal abstract class VulkanObj : IDisposable
{
    private int _disposed;

    internal virtual void DisposeCore() { }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            DisposeCore();
        }
        catch (Exception ex)
        {
            Logger.Fatal("VulkanObj", $"Dispose {GetType().Name} fall: {ex}");
            if (Debugger.IsAttached)
                Debugger.Break();
            throw;
        }
        finally
        {
            GC.SuppressFinalize(this);
        }
    }

    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);
    }

#if DEBUG
    ~VulkanObj()
    {
        if (_disposed != 0) return;
        try
        {
            Logger.Warning("VulkanObj", $"{GetType().Name} Dispose() was not called — native resources leaked.");
        }
        catch { }
    }
#endif
}