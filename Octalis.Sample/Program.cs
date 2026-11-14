using Octalis.Vulkan.Core;
using Silk.NET.Windowing;

WindowOptions options = WindowOptions.DefaultVulkan;
options.Title = "sample";
using VulkanWindow window = new(options);

window.OnLoad   += Onload;
window.OnUpdate += OnUpdate;
window.OnRender += OnRender;
window.OnClose  += OnClose;

window.Window.Run();

static void Onload() { 
    Console.WriteLine("meow");
    using var instance = new VulkanInstance();
}
static void OnUpdate(double dt) { }
static void OnRender(double dt) { }
static void OnClose() { Console.WriteLine("mrew");}