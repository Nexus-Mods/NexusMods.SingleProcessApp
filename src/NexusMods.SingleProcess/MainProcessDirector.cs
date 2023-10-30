﻿using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NexusMods.SingleProcess;

/// <summary>
/// This is the main entry class for the single process application, it is responsible for
/// starting the main process, and piping CLI calls to the process, and transferring data
/// between CLI processes and the main process.
/// </summary>
public class MainProcessDirector : ADirector
{
    private TcpListener? _tcpListener;
    private readonly ILogger<MainProcessDirector> _logger;
    private Task? _listenerTask;
    private readonly List<Task> _runningClients = new();
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly CancellationToken _cancellationToken;
    private readonly DateTime _lastConnection;

    /// <summary>
    /// The director for the main process, this is responsible for starting the main process, and setting up the TCP Listener
    /// and registering the main process in the shared array.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="singleProcessSettings"></param>
    public MainProcessDirector(ILogger<MainProcessDirector> logger, SingleProcessSettings singleProcessSettings)
        : base(singleProcessSettings)
    {
        _logger = logger;
        _cancellationTokenSource = new();
        _cancellationToken = _cancellationTokenSource.Token;
        _lastConnection = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns true if the director is listening for connections
    /// </summary>
    public bool IsListening => _listenerTask is { IsCompleted: false };

    /// <summary>
    /// Attempt to start the main process, if it's already running, this will return false and the app
    /// should attempt to connect via a ClientProcessDirector.
    /// </summary>
    /// <returns></returns>
    public async Task<bool> TryStartMain(IMainProcessHandler handler)
    {
        if (_cancellationToken.IsCancellationRequested)
            return false;

        // Connect to the shared array
        ConnectSharedArray();

        // Look for an existing main process
        var (process, _) = GetSyncInfo();
        if (process != null && process.Id != Environment.ProcessId)
            return false;

        if (process != null && process.Id == Environment.ProcessId)
        {
            _logger.LogInformation("This process ({ProcessId}) is already the main process", Environment.ProcessId);
            return true;
        }

        _logger.LogInformation("No main process found, starting main process ({ProcessId})", Environment.ProcessId);

        await StartTcpListener(handler);

        // If someone else has already started the main process, then we should stop
        if (MainIsAlreadyRunning())
            return false;

        var id = GetThisSyncValue();

        while (true)
        {
            // Get the current value
            var current = SharedArray!.Get(0);

            // If the current value hasn't changed, then we are the main process
            if (SharedArray.CompareAndSwap(0, current, id))
                break;

            // If a cancellation was requested, then we should stop
            if (_cancellationToken.IsCancellationRequested)
                return false;

            // If some other process is now the main process, then we should stop
            if (MainIsAlreadyRunning())
                return false;
        }

        _logger.LogInformation("This process ({ProcessId}) is now the main process", Environment.ProcessId);
        // We are now the main process, and our information is published in the shared array
        return true;
    }

    private bool MainIsAlreadyRunning()
    {
        var syncValue = GetSyncInfo();
        if (syncValue.Process is not null)
        {
            _logger.LogInformation("Main process is already running, this director is now defunct and should be disposed");
            return true;
        }

        return false;
    }

    private async Task StartTcpListener(IMainProcessHandler handler)
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            var port = Random.Shared.Next(Settings.PortMin, Settings.PortMax);
            try
            {
                _tcpListener = new TcpListener(IPAddress.Loopback, port);
                _tcpListener.Start();
                _listenerTask = Task.Run(async () => await StartListening(handler), _cancellationToken);
                _logger.LogInformation("Started TCP listener on port {Port}", port);
                return;
            }
            catch (SocketException exception)
            {
                // That port didn't work, try another one
                _logger.LogInformation(exception, "Failed to start TCP listener on port {Port}, trying a different port", port);
            }

        }
        throw new TaskCanceledException();
    }

    private async Task StartListening(IMainProcessHandler handler)
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (ShouldExit())
                {
                    _logger.LogInformation("No connections after {Seconds} seconds, exiting", Settings.StayRunningTimeout.TotalSeconds);
                    return;
                }

                // Create a timeout token, and combine it with the main cancellation token
                var timeout = new CancellationTokenSource();
                timeout.CancelAfter(Settings.ListenTimeout);
                var combined = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, timeout.Token);

                var found = await _tcpListener!.AcceptTcpClientAsync(combined.Token);

                _runningClients.Add(Task.Run(() => HandleClient(found, handler), _cancellationToken));

                _logger.LogDebug("Accepted TCP connection from {RemoteEndPoint}",
                    ((IPEndPoint)found.Client.RemoteEndPoint!).Port);

                CleanClosedConnections();
            }
            catch (OperationCanceledException)
            {
                // The cancellation could be from the timeout, or the main cancellation token, if it's the
                // timeout, then we should just continue, if it's the main cancellation token, then we should stop
                if (!_cancellationToken.IsCancellationRequested)
                    continue;
                _logger.LogInformation("TCP listener was cancelled, stopping");
                return;
            }

        }
    }

    /// <summary>
    /// Returns true if the application should exit due to there being no connections recently generated
    /// </summary>
    /// <returns></returns>
    private bool ShouldExit()
    {
        return _runningClients.Count == 0 && DateTime.UtcNow - _lastConnection > Settings.StayRunningTimeout;
    }

    /// <summary>
    /// Clears up any closed connections in the <see cref="_runningClients"/> dictionary
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void CleanClosedConnections()
    {
        // Snapshot the dictionary before we modify it
        foreach(var task in _runningClients.ToArray())
            if (task.IsCompleted)
                _runningClients.Remove(task);
    }

    /// <summary>
    /// Handle a client connection
    /// </summary>
    /// <param name="client"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private async Task HandleClient(TcpClient client, IMainProcessHandler handler)
    {
        var stream = client.GetStream();
        using var binaryReader = new BinaryReader(stream, Encoding.UTF8, true);
        var argc = binaryReader.ReadInt32();
        var args = new string[argc];

        for (var i = 0; i < argc; i++)
            args[i] = binaryReader.ReadString();

        var proxiedConsole = new ProxiedConsole
        {
            Args = args,
            StdIn = new StreamReader(stream, Encoding.UTF8, leaveOpen: true),
            StdOut = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true),
            StdErr = StreamWriter.Null
        };

        await handler.Handle(proxiedConsole, _cancellationToken);

        await proxiedConsole.StdErr.FlushAsync();
        await proxiedConsole.StdOut.FlushAsync();
        await stream.FlushAsync(_cancellationToken);
        stream.Close();
        client.Close();
    }



    /// <summary>
    /// Combines the process id and port into a single ulong, this can then be used as a CAS value
    /// to atomically update where the main process is running.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private ulong GetThisSyncValue()
    {
        if (_tcpListener is null)
            throw new InvalidOperationException("The TCP listener is not running");
        if (SharedArray is null)
            throw new InvalidOperationException("The shared array is not initialized");

        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt32BigEndian(buffer, Environment.ProcessId);
        BinaryPrimitives.WriteInt32BigEndian(buffer[4..], ((IPEndPoint)_tcpListener.LocalEndpoint).Port);

        return BinaryPrimitives.ReadUInt64BigEndian(buffer);
    }


    /// <summary>
    /// Dispose of the director, this will stop the TCP listener, and dispose of the shared array, and make sure internal
    /// tasks are cancelled and finished running.
    /// </summary>
    public override async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing of the main process director");

        var ourVal = GetThisSyncValue();
        SharedArray?.CompareAndSwap(0, ourVal, 0);

        // Cancel the token first, so everything starts shutting down
        _cancellationTokenSource.Cancel();

        // Wait for the listener to stop
        if (_listenerTask != null)
            await _listenerTask.WaitAsync(CancellationToken.None);

        // Dispose of everything
        if (SharedArray != null) await CastAndDispose(SharedArray);
        if (_listenerTask != null) await CastAndDispose(_listenerTask);
        await CastAndDispose(_cancellationTokenSource);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }

    public static MainProcessDirector Create(IServiceProvider serviceProvider)
    {
        return new MainProcessDirector(serviceProvider.GetRequiredService<ILogger<MainProcessDirector>>(),
            serviceProvider.GetRequiredService<SingleProcessSettings>());
    }
}
