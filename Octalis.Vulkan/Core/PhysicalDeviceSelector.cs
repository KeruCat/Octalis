using System.Runtime.InteropServices;
using Silk.NET.Vulkan;

namespace Octalis.Vulkan.Core;

internal sealed class PhysicalDeviceSelector
{
    internal PhysicalDevice _gpu;
    internal PhysicalDeviceSelector(Instance instance, Vk vk)
    {
        var physicalDevices = vk.GetPhysicalDevices(instance);
        foreach (var device in physicalDevices)
        {
            vk.GetPhysicalDeviceProperties(device, out PhysicalDeviceProperties properties);
            PrintDeviceDetails(vk , device);
        }
    }
    unsafe void PrintDeviceDetails(Vk vk, PhysicalDevice device)
    {
        vk.GetPhysicalDeviceProperties(device, out PhysicalDeviceProperties props);
        string name = Marshal.PtrToStringUTF8((nint)props.DeviceName) ?? "Unknown";
        
        uint major = props.ApiVersion >> 22;
        uint minor = (props.ApiVersion >> 12) & 0x3FF;
        uint patch = props.ApiVersion & 0xFFF;

        Console.WriteLine($"=== {name} ===");
        Console.WriteLine($"Тип: {props.DeviceType}");
        Console.WriteLine($"Vendor ID: 0x{props.VendorID:X}, Device ID: 0x{props.DeviceID:X}");
        Console.WriteLine($"Версия Vulkan API: {major}.{minor}.{patch}");
        Console.WriteLine($"Макс. размер 2D текстуры: {props.Limits.MaxImageDimension2D}");
        Console.WriteLine($"Макс. выделений памяти: {props.Limits.MaxMemoryAllocationCount}");

        vk.GetPhysicalDeviceFeatures(device, out PhysicalDeviceFeatures features);
        Console.WriteLine("\n--- Функции (Features) ---");
        Console.WriteLine($"Geometry Shader: {(bool)features.GeometryShader}");
        Console.WriteLine($"Tessellation Shader: {(bool)features.TessellationShader}");
        Console.WriteLine($"Sampler Anisotropy: {(bool)features.SamplerAnisotropy}");
        Console.WriteLine($"Multi Viewport: {(bool)features.MultiViewport}");

        vk.GetPhysicalDeviceMemoryProperties(device, out PhysicalDeviceMemoryProperties memProps);
        Console.WriteLine("\n--- Кучи памяти (Memory Heaps) ---");
        for (int i = 0; i < memProps.MemoryHeapCount; i++)
        {
            var heap = memProps.MemoryHeaps[i];
            double sizeGb = heap.Size / (1024.0 * 1024.0 * 1024.0);
            bool isVram = heap.Flags.HasFlag(MemoryHeapFlags.DeviceLocalBit);
            Console.WriteLine($"Heap {i}: {sizeGb:F2} GB | VRAM (DeviceLocal): {isVram}");
        }
        uint queueFamilyCount = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, null);
        var queueFamilies = new QueueFamilyProperties[queueFamilyCount];
        fixed (QueueFamilyProperties* ptr = queueFamilies)
        {
            vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, ptr);
        }

        Console.WriteLine("\n--- Семейства очередей (Queue Families) ---");
        for (int i = 0; i < queueFamilies.Length; i++)
        {
            var qf = queueFamilies[i];
            Console.WriteLine($"Семейство {i}: Очередей = {qf.QueueCount} | Флаги = {qf.QueueFlags}");
        }
    }
}