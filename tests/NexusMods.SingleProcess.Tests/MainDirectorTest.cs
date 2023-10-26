namespace NexusMods.SingleProcess.Tests;

public class MainDirectorTest : IAsyncDisposable
{
    private readonly MainProcessDirector _main;

    public MainDirectorTest(MainProcessDirector main)
    {
        _main = main;
    }

    [Fact]
    public async Task CanStartAsMain()
    {
        (await _main.TryStartMain()).Should().BeTrue();
    }

    public async ValueTask DisposeAsync()
    {
        await _main.DisposeAsync();
    }
}
