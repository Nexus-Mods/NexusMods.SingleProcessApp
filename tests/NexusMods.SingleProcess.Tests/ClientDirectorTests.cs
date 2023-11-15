using Microsoft.Extensions.Logging;
using NexusMods.ProxyConsole;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.Implementations;
using Spectre.Console.Testing;

namespace NexusMods.SingleProcess.Tests;

public class ClientDirectorTests(ILogger<ClientProcessDirector> logger, IServiceProvider serviceProvider)
{
    [Fact]
    public async Task CanRoundtripData()
    {
        await using var main = MainProcessDirector.Create(serviceProvider);
        await using var client = ClientProcessDirector.Create(serviceProvider);


        var handler = new EchoArgsHandler(logger);
        await main.TryStartMainAsync(handler);

        var testConsole = new TestConsole();


        await client.StartClientAsync(new ConsoleSettings
        {
            Arguments = new[] {"Some", "Args", "Here"},
            Renderer = new SpectreRenderer(testConsole)
        });

        (await handler.Handled).Should().BeEquivalentTo("Some", "Args", "Here");

        testConsole.Output.Should().Be("Hello World! - Some|Args|Here");
    }

    private class EchoArgsHandler(ILogger logger) : IMainProcessHandler
    {
        private readonly TaskCompletionSource<string[]> _handled = new();

        public Task<string[]> Handled => _handled.Task;

        public async Task HandleAsync(string[] arguments, IRenderer console, CancellationToken token)
        {
            logger.LogInformation("Received {Count} arguments", arguments.Length);
            await console.RenderAsync(new Text { Template = $"Hello World! - {string.Join('|', arguments)}" });
            _handled.SetResult(arguments);
        }
    }

}
