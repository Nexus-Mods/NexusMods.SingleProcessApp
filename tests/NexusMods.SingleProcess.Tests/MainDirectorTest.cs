namespace NexusMods.SingleProcess.Tests;

public class MainDirectorTest : IAsyncDisposable
{
    private readonly MainProcessDirector _main;
    private readonly SingleProcessSettings _settings;

    public MainDirectorTest(MainProcessDirector main, SingleProcessSettings settings)
    {
        _settings = settings;
        _main = main;
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

    public async ValueTask DisposeAsync()
    {
        await _main.DisposeAsync();
    }
}
