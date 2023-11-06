using System.Text;
using Microsoft.Extensions.Logging;
using NexusMods.ProxyConsole;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Implementations;
using Spectre.Console.Testing;

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


        var handler = new EchoArgsHandler(_logger);
        await main.TryStartMain(handler);

        var testConsole = new TestConsole();


        await client.StartClient(new ConsoleSettings
        {
            Arguments = new[] {"Some", "Args", "Here"},
            Renderer = new SpectreRenderer(testConsole)
        });

        (await handler.Handled).Should().BeEquivalentTo("Some", "Args", "Here");

        testConsole.Output.Should().Be("Hello World! - Some|Args|Here");
    }

    public class EchoArgsHandler : IMainProcessHandler
    {
        private readonly ILogger _handlerLogger;
        private readonly TaskCompletionSource<string[]> _handled;

        public Task<string[]> Handled => _handled.Task;

        public EchoArgsHandler(ILogger logger)
        {
            _handlerLogger = logger;
            _handled = new TaskCompletionSource<string[]>();
        }
        public async Task HandleAsync(string[] arguments, IRenderer console, CancellationToken token)
        {
            _handlerLogger.LogInformation("Received {Count} arguments", arguments.Length);
            await console.RenderAsync(new Text { Template = $"Hello World! - {string.Join('|', arguments)}" });
            _handled.SetResult(arguments);
        }
    }

}
