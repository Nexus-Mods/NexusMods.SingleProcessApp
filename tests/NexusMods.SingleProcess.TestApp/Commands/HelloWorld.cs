using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Implementations;

namespace NexusMods.SingleProcess.TestApp.Commands;

public class HelloWorld
{
    public static async Task<int> ExecuteAsync(IRenderer renderer, string name)
    {
        await renderer.RenderAsync(new Text { Template = $"Hello {name}" });
        return 0;
    }
}
