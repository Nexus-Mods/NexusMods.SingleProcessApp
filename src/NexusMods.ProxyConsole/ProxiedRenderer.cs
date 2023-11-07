using System.IO;
using System.Threading.Tasks;
using Nerdbank.Streams;
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
    public static async Task<(string[] Arguments, IRenderer Renderer)> Create(Stream duplexStream)
    {
        var renderer = new ProxiedRenderer(new Serializer(duplexStream));

        var arguments = await renderer._serializer.SendAndReceiveAsync<ProgramArgumentsResponse, ProgramArgumentsRequest>
            (new ProgramArgumentsRequest());

        return (arguments.Arguments, renderer);
    }

    public async ValueTask RenderAsync(IRenderable renderable)
    {
        await _serializer.SendAndAckAsync(new Render {Renderable = renderable});
    }

    public ValueTask ClearAsync()
    {
        throw new System.NotImplementedException();
    }
}
