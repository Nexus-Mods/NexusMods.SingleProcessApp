using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using NexusMods.ProxyConsole;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.SingleProcess.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddSingleProcess(_ => new SingleProcessSettings
            {
                SyncFile = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("tests.sync")
            })
            .AddSingleton<CommandLineConfigurator>()
            .AddDefaultRenderers()
            .AddDefaultParsers()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .AddLogging(builder => builder.AddXunitOutput());

        CliParserTests.AddCliParserTestVerbs(container);

    }
}
