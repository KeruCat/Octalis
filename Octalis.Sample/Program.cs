using Octalis.Vulkan.Core;
using Octalis.Vulkan.Debug;
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

    var bootOptions = new VulkanDebugOptions() {};
    using var boot = new VulkanDebugBootstrap(bootOptions);
    var instanceoptions = new VulkanInstanceOptions()
    {
      ApplicationName = "Octalis.Sample",
      EngineName = "Octalis",
      Extensions =  [],
      Layers = [],
      DebugCreateInfo = boot.CreateInfo
    };
    using var instance = new VulkanInstance(instanceoptions);
    window.Window.Run();

    void Onload(){}
    static void OnUpdate(double dt) { }
    static void OnRender(double dt) { }
    static void OnClose() { Console.WriteLine("mrew");}
}