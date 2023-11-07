﻿using System;
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

    /// <summary>
    /// Constructs and adapter and starts forwarding commands from the duplexStream to the console. Arguments are
    /// to be passed to the main process, they can be provided via the args parameter.
    /// </summary>
    /// <param name="duplexStream"></param>
    /// <param name="renderer"></param>
    /// <param name="args"></param>
    public ClientRendererAdaptor(Stream duplexStream, IRenderer renderer, string[]? args = null)
    {
        _stream = duplexStream;
        _renderer = renderer;
        _serializer = new Serializer(duplexStream);
        _task = Task.Run(ForwardCommands);
        _args = args ?? Array.Empty<string>();
    }

    /// <summary>
    /// The task that is forwarding commands from the duplexStream to the console.
    /// </summary>
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
                    case Clear:
                        await _renderer.ClearAsync();
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
}
