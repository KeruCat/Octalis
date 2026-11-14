using Octalis.Vulkan.Core;
using Silk.NET.Core.Native;
using Silk.NET.Windowing;
unsafe{
    WindowOptions options = WindowOptions.DefaultVulkan;
    options.Title = "sample";
    using VulkanWindow window = new(options);

    window.OnLoad   += Onload;
    window.OnUpdate += OnUpdate;
    window.OnRender += OnRender;
    window.OnClose  += OnClose;

    window.Window.Run();

    void Onload() { 
        Console.WriteLine("meow");
        uint count = 0;
        var names = window.Window.VkSurface?.GetRequiredExtensions(out count);
        List<string> list = [];
        for (var i = 0; i < count; i++)
            list.Add(SilkMarshal.PtrToString((nint)names[i])?? string.Empty);

        string[] extensions = [.. list];
        foreach(string name in extensions) Console.WriteLine(name);
        using var instance = new VulkanInstance(extensions);
        using var surface = new VulkanSurface(instance.Instance, window.Window ,instance.Vk);
    }
    static void OnUpdate(double dt) { }
    static void OnRender(double dt) { }
    static void OnClose() { Console.WriteLine("mrew");}
}