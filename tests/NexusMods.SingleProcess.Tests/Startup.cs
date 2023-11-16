using Microsoft.Extensions.Configuration;
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
            .AddSingleProcess((_, s) => s)
            .AddSingleton<CommandLineConfigurator>()
            .AddDefaultRenderers()
            .AddDefaultParsers()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .AddLogging(builder => builder.AddXunitOutput());

        CliParserTests.AddCliParserTestVerbs(container);

    }
}
