using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MemoryPack;
using MemoryPack.Streaming;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Messages;

namespace NexusMods.ProxyConsole;

public class ServerSerializer
{
    private readonly Stream _stream;
    private readonly BinaryWriter _binaryWriter;
    private readonly MemoryPool<byte> _memoryPool;
    private readonly BinaryReader _binaryReader;

    public ServerSerializer(Stream duplexStream)
    {
        _stream = duplexStream;
        _binaryWriter = new BinaryWriter(_stream, Encoding.UTF8, true);
        _binaryReader = new BinaryReader(_stream, Encoding.UTF8, true);
        _memoryPool = MemoryPool<byte>.Shared;
    }


    /// <summary>
    /// Sends the given message to the server and does not wait for a response.
    /// </summary>
    /// <param name="msg"></param>
    /// <typeparam name="TMessage"></typeparam>
    public async Task SendAndAckAsync<TMessage>(TMessage msg)
    where TMessage : IMessage
    {
        await Send(msg);
        await ReceiveExactly<Acknowledge>();
    }

    public async Task<TResponse> SendAndReceive<TResponse, TRequest>(TRequest msg)
        where TResponse : class, IMessage
        where TRequest : IMessage
    {
        await Send(msg);
        return await ReceiveExactly<TResponse>();
    }

    public async Task Send<TMessage>(TMessage msg) where TMessage : IMessage
    {
        var serialized = MemoryPackSerializer.Serialize<IMessage>(msg);
        _binaryWriter.Write(serialized.Length);
        await _stream.WriteAsync(serialized);
    }

    public async Task Acknowledge()
    {
        await Send(new Acknowledge());
    }

    private async Task<TMessage> ReceiveExactly<TMessage>() where TMessage : class, IMessage
    {
        var deserialized = await Receive();
        return deserialized as TMessage ?? throw new Exception("Unexpected message type");
    }

    public async Task<IMessage?> Receive()
    {
        var size = _binaryReader.ReadUInt32();
        using var buffer = _memoryPool.Rent((int)size);
        var sized = buffer.Memory[..(int)size];
        await _stream.ReadExactlyAsync(sized);
        var deserialized = MemoryPackSerializer.Deserialize<IMessage>(sized.Span);
        return deserialized;
    }
}
