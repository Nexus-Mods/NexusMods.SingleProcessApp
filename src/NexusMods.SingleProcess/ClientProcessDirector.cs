using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nerdbank.Streams;
using NexusMods.ProxyConsole;

namespace NexusMods.SingleProcess;

public class ClientProcessDirector : ADirector
{
    private readonly ILogger<ClientProcessDirector> _logger;
    private TcpClient? _client;
    private NetworkStream? _stream;

    public ClientProcessDirector(ILogger<ClientProcessDirector> logger, SingleProcessSettings settings) : base(settings)
    {
        _logger = logger;
    }

    public async Task StartClient(ConsoleSettings proxy)
    {
        ConnectSharedArray();

        var (process, port) = GetSyncInfo();
        if (process is null)
        {
            throw new InvalidDataException("No main process is started");
        }

        _logger.LogInformation("Found main process {ProcessId} listening on port {Port}", process.Id, port);

        _client = new TcpClient();
        try
        {
            await _client.ConnectAsync(IPAddress.Loopback, port);
            _stream = _client.GetStream();

            _logger.LogDebug("Connected to main process {ProcessId} on port {Port}", process.Id, port);
            await RunTillClose(proxy);
        }
        catch (SocketException ex)
        {
            _logger.LogWarning("Failed to connect to main process {ProcessId} on port {Port}", process.Id, port);
            throw ex;
        }
    }

    private async Task RunTillClose(ConsoleSettings proxy)
    {
        try
        {
            var adaptor = new ClientRendererAdaptor(_stream!, proxy.Renderer, proxy.Arguments);
            await adaptor.RunningTask;
        }
        catch (IOException ex)
        {
            _logger.LogDebug(ex, "Client disconnected");
        }
    }

    /*
    private async Task SendStartupInfo(ProxiedConsole proxy)
    {
        await using var binaryWriter = new BinaryWriter(_stream!, Encoding.UTF8, true);
        foreach (var arg in proxy.Args)
        {
            binaryWriter.Write(proxy.Args.Length);
            binaryWriter.Write(arg);
        }
        binaryWriter.Flush();
    }
    */


    public override async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        SharedArray?.Dispose();
    }

    public static ClientProcessDirector Create(IServiceProvider serviceProvider)
    {
        return new ClientProcessDirector(serviceProvider.GetRequiredService<ILogger<ClientProcessDirector>>(),
            serviceProvider.GetRequiredService<SingleProcessSettings>());
    }
}
