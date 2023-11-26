using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Messages;

namespace NexusMods.ProxyConsole;

public class ProxiedRenderer : IRenderer
{
    private readonly Serializer _serializer;

    private ProxiedRenderer(Serializer serializer)
    {
        _serializer = serializer;
    }

    /// <summary>
    /// Creates a new <see cref="ProxiedRenderer"/> instance from the given duplex capable stream.
    /// </summary>
    /// <param name="duplexStream"></param>
    /// <returns></returns>
    public static async Task<(string[] Arguments, IRenderer Renderer)> Create(IServiceProvider provider, Stream duplexStream)
    {
        var renderer = new ProxiedRenderer(new Serializer(duplexStream, provider.GetRequiredService<IEnumerable<IRenderableDefinition>>()));

        var arguments = await renderer._serializer.SendAndReceiveAsync<ProgramArgumentsResponse, ProgramArgumentsRequest>
            (new ProgramArgumentsRequest());

        return (arguments.Arguments, renderer);
    }

    public async ValueTask RenderAsync(IRenderable renderable)
    {
        await _serializer.SendAndAckAsync(new Render {Renderable = renderable});
    }

    public async ValueTask ClearAsync()
    {
        await _serializer.SendAndAckAsync(new Clear());
    }
}
