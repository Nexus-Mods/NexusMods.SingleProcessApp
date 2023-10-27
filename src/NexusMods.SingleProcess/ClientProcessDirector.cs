using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NexusMods.SingleProcess;

public class ClientProcessDirector : ADirector
{
    private readonly ILogger<ClientProcessDirector> _logger;
    private TcpClient? _client;

    public ClientProcessDirector(ILogger<ClientProcessDirector> logger, SingleProcessSettings settings) : base(settings)
    {
        _logger = logger;
    }

    public async Task<bool> TryStartClient()
    {
        ConnectSharedArray();

        var (process, port) = GetSyncInfo();
        if (process is null)
            return false;

        _logger.LogInformation("Found main process {ProcessId} listening on port {Port}", process.Id, port);

        _client = new TcpClient();
        try
        {
            await _client.ConnectAsync(IPAddress.Loopback, port);
            return true;
        }
        catch (SocketException)
        {
            _logger.LogWarning("Failed to connect to main process {ProcessId} on port {Port}", process.Id, port);
            return false;
        }
    }


    public override async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        SharedArray?.Dispose();
    }
}
