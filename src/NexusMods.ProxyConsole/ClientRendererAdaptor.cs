using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MemoryPack;
using MemoryPack.Streaming;
using Nerdbank.Streams;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Messages;
using Spectre.Console;

namespace NexusMods.ProxyConsole;

/// <summary>
/// Forwards commands from the duplexStream to the console. .Start() must be called to start forwarding.
/// </summary>
public class ClientRendererAdaptor
{
    private readonly Stream _stream;
    private readonly Task _task;
    private readonly IRenderer _renderer;
    private readonly Serializer _serializer;
    private readonly string[] _args;

    public ClientRendererAdaptor(Stream duplexStream, IRenderer renderer, string[]? args = null)
    {
        _stream = duplexStream;
        _renderer = renderer;
        _serializer = new Serializer(duplexStream);
        _task = Task.Run(ForwardCommands);
        _args = args ?? Array.Empty<string>();
    }

    public Task RunningTask => _task;

    private async Task ForwardCommands()
    {
        while(true)
        {
            try
            {
                var msg = await _serializer.ReceiveAsync();

                switch (msg)
                {
                    case Render renderable:
                        await _renderer.RenderAsync(renderable.Renderable);
                        await _serializer.AcknowledgeAsync();
                        break;
                    case ProgramArgumentsRequest:
                        await _serializer.SendAsync(new ProgramArgumentsResponse()
                        {
                            Arguments = _args
                        });
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(msg));

                }
            }
            catch (EndOfStreamException)
            {
                break;
            }
        }
    }

    private void SendResponse(IMessage message)
    {
        Span<byte> sizeBuff = stackalloc byte[4];
        var buff = MemoryPackSerializer.Serialize(message);
        BitConverter.TryWriteBytes(sizeBuff, buff.Length);
        _stream.Write(sizeBuff);
        _stream.Write(buff);
    }

    public bool IsRunning { get; set; }

}
