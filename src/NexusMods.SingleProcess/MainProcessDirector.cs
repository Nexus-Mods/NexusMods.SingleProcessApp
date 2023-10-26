using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NexusMods.SingleProcess;

/// <summary>
/// This is the main entry class for the single process application, it is responsible for
/// starting the main process, and piping CLI calls to the process, and transferring data
/// between CLI processes and the main process.
/// </summary>
public class MainProcessDirector : IAsyncDisposable
{
    private readonly SingleProcessSettings _settings;
    private ISharedArray? _sharedArray;
    private TcpListener? _tcpListener;
    private readonly ILogger<MainProcessDirector> _logger;
    private Task? _listenerTask;
    private List<Task> _runningClients = new();
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
    {
        _logger = logger;
        _settings = singleProcessSettings;
        _cancellationTokenSource = new();
        _cancellationToken = _cancellationTokenSource.Token;
        _lastConnection = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns true if the director is listening for connections
    /// </summary>
    public bool IsListening => _listenerTask is { IsCompleted: false };

    /// <summary>
    /// Returns true if this process is the main process
    /// </summary>
    public bool IsMainProcess => _sharedArray is not null && GetSyncInfo().Process is not null;

    /// <summary>
    /// Attempt to start the main process, if it's already running, this will return false and the app
    /// should attempt to connect via a ClientProcessDirector.
    /// </summary>
    /// <returns></returns>
    public async Task<bool> TryStartMain()
    {
        if (_cancellationToken.IsCancellationRequested)
            return false;

        // Connect to the shared array
        ConnectSharedArray();

        // Look for an existing main process
        var (process, _) = GetSyncInfo();
        if (process != null)
            return false;


        await StartTcpListener();

        // If someone else has already started the main process, then we should stop
        if (MainIsAlreadyRunning())
            return false;

        var id = GetThisSyncValue();

        while (true)
        {
            // Get the current value
            var current = _sharedArray!.Get(0);

            // If the current value hasn't changed, then we are the main process
            if (_sharedArray.CompareAndSwap(0, current, id))
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

    private async Task StartTcpListener()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            var port = Random.Shared.Next(_settings.PortMin, _settings.PortMax);
            try
            {
                _tcpListener = new TcpListener(IPAddress.Loopback, port);
                _tcpListener.Start();
                _listenerTask = Task.Run(StartListening, _cancellationToken);
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

    private async Task StartListening()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (ShouldExit())
                {
                    _logger.LogInformation("No connections after {Seconds} seconds, exiting", _settings.StayRunningTimeout.TotalSeconds);
                    return;
                }

                // Create a timeout token, and combine it with the main cancellation token
                var timeout = new CancellationTokenSource();
                timeout.CancelAfter(_settings.ListenTimeout);
                var combined = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, timeout.Token);

                var found = await _tcpListener!.AcceptTcpClientAsync(combined.Token);

                _runningClients.Add(Task.Run(() => HandleClient(found), _cancellationToken));

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
        return _runningClients.Count == 0 && DateTime.UtcNow - _lastConnection > _settings.StayRunningTimeout;
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
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private Task HandleClient(TcpClient client)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Connect to the shared array
    /// </summary>
    private void ConnectSharedArray()
    {
        _sharedArray = new MultiProcessSharedArray(_settings.SyncFile, (int)(_settings.SyncFileSize.Value / 8));
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
        if (_sharedArray is null)
            throw new InvalidOperationException("The shared array is not initialized");

        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt32BigEndian(buffer, Environment.ProcessId);
        BinaryPrimitives.WriteInt32BigEndian(buffer[4..], ((IPEndPoint)_tcpListener.LocalEndpoint).Port);

        return BinaryPrimitives.ReadUInt64BigEndian(buffer);
    }

    /// <summary>
    /// Returns the current process and port that's stored in the sync file
    /// </summary>
    /// <returns></returns>
    private (Process? Process, int Port) GetSyncInfo()
    {
        var val = _sharedArray!.Get(0);
        var pid = (int)(val >> 32);
        var port = (int)(val & 0xFFFFFFFF);

        if (pid == 0)
            return (null, port);

        try
        {
            return (Process.GetProcessById(pid), port);
        }
        catch (ArgumentException)
        {
            return (null, port);
        }
    }

    /// <summary>
    /// Dispose of the director, this will stop the TCP listener, and dispose of the shared array, and make sure internal
    /// tasks are cancelled and finished running.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing of the main process director");
        // Cancel the token first, so everything starts shutting down
        _cancellationTokenSource.Cancel();

        // Wait for the listener to stop
        if (_listenerTask != null)
            await _listenerTask.WaitAsync(CancellationToken.None);

        // Dispose of everything
        if (_sharedArray != null) await CastAndDispose(_sharedArray);
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
}
