using NexusMods.ProxyConsole.Abstractions;

namespace NexusMods.SingleProcess.Tests;

public class LoggingRenderer : IRenderer
{
    public List<IRenderable> Log { get; } = new();

    public ValueTask RenderAsync(IRenderable renderable)
    {
        Log.Add(renderable);
        return ValueTask.CompletedTask;
    }
    public ValueTask ClearAsync()
    {
        Log.Clear();
        return ValueTask.CompletedTask;
    }
}
