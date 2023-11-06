using System;
using System.Threading.Tasks;
using Spectre.Console;
using Abstractions = NexusMods.ProxyConsole.Abstractions;
using Impl = NexusMods.ProxyConsole.Implementations;
using Render = Spectre.Console.Rendering;

namespace NexusMods.ProxyConsole;


public class SpectreRenderer : Abstractions.IRenderer
{
    private readonly IAnsiConsole _console;

    public SpectreRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    private async ValueTask<Render.IRenderable> ToSpectre(Abstractions.IRenderable renderable)
    {
        return renderable switch
        {
            Implementations.Text text => new Text(text.Template),
            _ => throw new NotImplementedException()
        };
    }

    public async ValueTask RenderAsync(Abstractions.IRenderable renderable)
    {
        var spectre = await ToSpectre(renderable);
        _console.Write(spectre);
    }

    public ValueTask ClearAsync()
    {
        _console.Clear();
        return ValueTask.CompletedTask;
    }
}
