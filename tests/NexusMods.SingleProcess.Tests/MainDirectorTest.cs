using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace NexusMods.SingleProcess.Tests;

public class MainDirectorTest
{
    /*
    private readonly MainProcessDirector _main;
    private readonly SingleProcessSettings _settings;
    private readonly ClientProcessDirector _client;

    public MainDirectorTest(ILogger<MainProcessDirector> mainProcessLogger, ClientProcessDirector client,  SingleProcessSettings settings)
    {
        _settings = settings;
        _main = new MainProcessDirector(mainProcessLogger, settings);
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
        _main.IsListening.Should().BeTrue("the director is listening");
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

    public void Dispose()
    {
        // Strange doing it this way but for some reason the DisposeAsync method is not being called by xUnit :(
        Task.Run(async () =>
        {
            await _main.DisposeAsync();
        }).Wait();
    }
    */
}
