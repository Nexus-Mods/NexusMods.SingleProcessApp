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
            .AddFileSystem()
            .AddSingleProcess()
            .AddSingleton<CommandLineConfigurator>()
            .AddDefaultRenderers()
            .AddDefaultParsers()
            .AddSingleton(services =>
            {
                var fileSystem = services.GetRequiredService<IFileSystem>();
                return new SingleProcessSettings
                {
                    SyncFile = fileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("tests.sync")
                };
            })
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .AddLogging(builder => builder.AddXunitOutput());

        CliParserTests.AddCliParserTestVerbs(container);

    }
}
