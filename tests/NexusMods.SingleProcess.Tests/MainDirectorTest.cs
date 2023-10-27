using Microsoft.Extensions.Logging.Abstractions;

namespace NexusMods.SingleProcess.Tests;

public class MainDirectorTest : IAsyncDisposable
{
    private readonly MainProcessDirector _main;
    private readonly SingleProcessSettings _settings;
    private readonly ClientProcessDirector _client;

    public MainDirectorTest(MainProcessDirector main, ClientProcessDirector client,  SingleProcessSettings settings)
    {
        _settings = settings;
        _main = main;
        _client = client;
    }

    [Fact]
    public async Task CanStartAsMain()
    {
        (await _main.TryStartMain()).Should().BeTrue();
    }

    [Fact]
    public async Task AutoClosesAfterTimeout()
    {
        (await _main.TryStartMain()).Should().BeTrue();
        _main.IsMainProcess.Should().BeTrue("we just started the main process");
        await Task.Delay(_settings.StayRunningTimeout + TimeSpan.FromSeconds(1));
        _main.IsListening.Should().BeFalse("the director has stopped");
    }

    [Fact]
    public async Task CanConnectAsClient()
    {
        (await _main.TryStartMain()).Should().BeTrue();
        _main.IsMainProcess.Should().BeTrue("we just started the main process");
        await Task.Delay(_settings.StayRunningTimeout + TimeSpan.FromSeconds(1));
        _main.IsListening.Should().BeFalse("the director has stopped");

        (await _client.TryStartClient()).Should().BeTrue();
        _client.IsMainProcess.Should().BeTrue("we are running the client from the main process");
    }

    public async ValueTask DisposeAsync()
    {
        await _main.DisposeAsync();
    }
}
