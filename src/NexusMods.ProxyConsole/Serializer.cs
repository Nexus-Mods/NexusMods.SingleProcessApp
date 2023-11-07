﻿using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MemoryPack;
using MemoryPack.Streaming;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Exceptions;
using NexusMods.ProxyConsole.Messages;

namespace NexusMods.ProxyConsole;


/// <summary>
/// Serializes and deserializes messages to and from the server, and manages the underlying
/// duplex stream.
/// </summary>
public class Serializer
{
    private readonly Stream _stream;
    private readonly BinaryWriter _binaryWriter;
    private readonly MemoryPool<byte> _memoryPool;
    private readonly BinaryReader _binaryReader;

    /// <summary>
    /// Primary constructor, takes a duplex capable stream
    /// </summary>
    /// <param name="duplexStream"></param>
    public Serializer(Stream duplexStream)
    {
        _stream = duplexStream;
        _binaryWriter = new BinaryWriter(_stream, Encoding.UTF8, true);
        _binaryReader = new BinaryReader(_stream, Encoding.UTF8, true);
        _memoryPool = MemoryPool<byte>.Shared;
    }


    /// <summary>
    /// Sends the given message to the server and waits for an acknowledgement.
    /// </summary>
    /// <param name="msg"></param>
    /// <typeparam name="TMessage"></typeparam>
    public async Task SendAndAckAsync<TMessage>(TMessage msg)
    where TMessage : IMessage
    {
        await SendAsync(msg);
        await ReceiveExactlyAsync<Acknowledge>();
    }

    /// <summary>
    /// Sends the given message to the server and waits for a response of the given type.
    /// </summary>
    /// <param name="msg"></param>
    /// <typeparam name="TResponse"></typeparam>
    /// <typeparam name="TRequest"></typeparam>
    /// <returns></returns>
    public async Task<TResponse> SendAndReceiveAsync<TResponse, TRequest>(TRequest msg)
        where TResponse : class, IMessage
        where TRequest : IMessage
    {
        await SendAsync(msg);
        return await ReceiveExactlyAsync<TResponse>();
    }

    /// <summary>
    /// Sends the given message to the server and does not wait for a response.
    /// </summary>
    /// <param name="msg"></param>
    /// <typeparam name="TMessage"></typeparam>
    public async Task SendAsync<TMessage>(TMessage msg) where TMessage : IMessage
    {
        var serialized = MemoryPackSerializer.Serialize<IMessage>(msg);
        _binaryWriter.Write(serialized.Length);
        await _stream.WriteAsync(serialized);
    }

    /// <summary>
    /// Sends an acknowledgement to the server.
    /// </summary>
    public async Task AcknowledgeAsync()
    {
        await SendAsync(new Acknowledge());
    }

    /// <summary>
    /// Receives a message of the given type from the server, if the message is not of the given type an
    /// exception is thrown.
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private async Task<TMessage> ReceiveExactlyAsync<TMessage>() where TMessage : class, IMessage
    {
        var deserialized = await ReceiveAsync();

        if (deserialized is not TMessage)
            UnexpectedMessageException.Throw(typeof(TMessage), deserialized?.GetType() ?? typeof(object));
        return (TMessage)deserialized!;
    }

    /// <summary>
    /// Receives the next message from the server.
    /// </summary>
    /// <returns></returns>
    public async Task<IMessage?> ReceiveAsync()
    {
        var size = _binaryReader.ReadUInt32();
        using var buffer = _memoryPool.Rent((int)size);
        var sized = buffer.Memory[..(int)size];
        await _stream.ReadExactlyAsync(sized);
        var deserialized = MemoryPackSerializer.Deserialize<IMessage>(sized.Span);
        return deserialized;
    }
}
