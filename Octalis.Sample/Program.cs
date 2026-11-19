using Octalis.Vulkan.Core;
using Octalis.Vulkan.Debug;
using Octalis.Vulkan.Logging;
using Octalis.Vulkan.Validation;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
unsafe
{
    WindowOptions options = WindowOptions.DefaultVulkan;
    options.Title = "sample";
    using VulkanWindow window = new(options);
    using Vk vk = Vk.GetApi();
    window.OnLoad += Onload;
    window.OnUpdate += OnUpdate;
    window.OnRender += OnRender;
    window.OnClose += OnClose;

    var bootOptions = new VulkanDebugOptions() { };
    var boot = new VulkanDebugBootstrap(bootOptions);
    var instanceoptions = new VulkanInstanceOptions()
    {
        ApplicationName = "Octalis.Sample",
        ApplicationVersion = Vk.MakeVersion(0,1,0),
        EngineName = "Octalis",
        EngineVersion = Vk.MakeVersion(0,1,0),
        Extensions = ["VK_EXT_debug_utils"],
        Layers = ["VK_LAYER_KHRONOS_validation", "VK_LAYER_LUNARG_monitor"],
        DebugCreateInfo = boot.CreateInfo,
        Version = Vk.MakeVersion(1,1,0)
    };
    VulkanInstancePreflight.Validate(instanceoptions, vk);
    using var instance = new VulkanInstance(instanceoptions, vk);
    window.Window.Run();

    static void Onload() { Logger.Information("onLoad","Meow"); }
    static void OnUpdate(double dt) { }
    static void OnRender(double dt) { }
    static void OnClose() { Logger.Information("onClose","Meow"); }
}