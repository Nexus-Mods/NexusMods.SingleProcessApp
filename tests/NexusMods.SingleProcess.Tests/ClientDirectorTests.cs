using System.Text;
using Microsoft.Extensions.Logging;

namespace NexusMods.SingleProcess.Tests;

public class ClientDirectorTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ClientProcessDirector> _logger;

    public ClientDirectorTests(ILogger<ClientProcessDirector> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }



    [Fact]
    public async Task CanRoundtripData()
    {
        await using var main = MainProcessDirector.Create(_serviceProvider);
        await using var client = ClientProcessDirector.Create(_serviceProvider);

        await main.TryStartMain(new EchoArgsHandler(_logger));


        var stdOut = new MemoryStream();
        await client.StartClient(new ProxiedConsole
        {
            Args = "Some Args Here".Split(' '),
            StdOut = new StreamWriter(stdOut, Encoding.UTF8, 1024, true),
            StdIn = new StreamReader(Stream.Null),
            StdErr = new StreamWriter(Stream.Null, Encoding.UTF8, 1024, true)
        });

        var stdOutString = Encoding.UTF8.GetString(stdOut.ToArray());
        stdOutString.Should().Contain("Hello World! - Some|Args|Here");
    }

    public class EchoArgsHandler : IMainProcessHandler
    {
        private readonly ILogger _handlerLogger;

        public EchoArgsHandler(ILogger logger)
        {
            _handlerLogger = logger;
        }
        public async Task Handle(ProxiedConsole console, CancellationToken token)
        {
            _handlerLogger.LogInformation("Received {Count} arguments", console.Args.Length);
            await console.StdOut.WriteAsync("Hello World! - " + string.Join("|", console.Args));

            await console.StdOut.FlushAsync();
            await Task.Delay(1000, token);
        }
    }

}
