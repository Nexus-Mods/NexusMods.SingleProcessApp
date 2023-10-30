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

    public async Task StartClient(ProxiedConsole proxy)
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

    private async Task RunTillClose(ProxiedConsole proxy)
    {
        try
        {
            await using var multiplexer = await MultiplexingStream.CreateAsync(_stream!);

            var argsChannel = await multiplexer.AcceptChannelAsync("args");
            var stdInChannel = await multiplexer.AcceptChannelAsync("stdin");
            var stdOutChannel = await multiplexer.AcceptChannelAsync("stdout");
            var stdErrChannel = await multiplexer.AcceptChannelAsync("stderr");

            {
                await using var argsStream = argsChannel.AsStream();
                await using var bw = new BinaryWriter(argsStream, Encoding.UTF8, true);

                bw.Write(proxy.Args.Length);
                foreach (var arg in proxy.Args)
                {
                    bw.Write(arg);
                }
                bw.Flush();
            }

            {
                await using var stdInStream = stdInChannel.AsStream();
                await using var stdOutStream = stdOutChannel.AsStream();
                await using var stdErrStream = stdErrChannel.AsStream();

                var stdInTask = proxy.StdIn.CopyToAsync(stdInStream);
                var stdOutTask = stdOutStream.CopyToAsync(proxy.StdOut);
                var stdErrTask = stdErrStream.CopyToAsync(proxy.StdErr);

                await Task.WhenAll(stdInTask, stdOutTask, stdErrTask);
            }
        }
        catch (IOException ex)
        {
            _logger.LogDebug(ex, "Client disconnected");
        }
    }

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
