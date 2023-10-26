using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.SingleProcess.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddSingleton<MainProcessDirector>()
            .AddSingleton<SingleProcessSettings>(_ => new SingleProcessSettings
            {
                MainApplication = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory),
                MainApplicationArgs = new [] {"main-process"},
            })
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .AddLogging(builder => builder.AddXunitOutput());

    }
}
