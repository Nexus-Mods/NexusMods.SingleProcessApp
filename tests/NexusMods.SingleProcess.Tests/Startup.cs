using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using NexusMods.ProxyConsole;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.SingleProcess.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddSingleProcess((_, s) => s)
            .AddDefaultRenderers()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .AddLogging(builder => builder.AddXunitOutput());

    }
}
