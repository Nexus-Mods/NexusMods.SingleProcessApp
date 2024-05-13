using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using NexusMods.ProxyConsole;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.Implementations;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;
using NexusMods.SingleProcess;
using Text = NexusMods.ProxyConsole.Abstractions.Implementations.Text;


var host = Host.CreateDefaultBuilder()
    .ConfigureServices(s =>
    {
        s.AddLogging();
        s.AddFileSystem();
        s.AddSingleProcess();
        s.AddDefaultRenderers();

        s.AddSingleton<IStartupHandler, Handler>();

        s.AddSingleton<SingleProcessSettings>(services =>
        {
            var fileSystem = services.GetRequiredService<IFileSystem>();
            return new SingleProcessSettings
            {
                SyncFile = fileSystem.GetKnownPath(KnownPath.EntryDirectory).Combine("tests.sync")
            };
        });
        s.AddDefaultParsers();

        s.AddVerb(() => Verbs.HelloWorld);
        s.AddVerb(() => Verbs.GuidTable);
        s.AddSingleton<CommandLineConfigurator>();

    }).Build();

Console.OutputEncoding = Encoding.UTF8;

var startupDirector = host.Services.GetRequiredService<StartupDirector>();
return await startupDirector.Start(args);


static class Verbs
{

    [Verb("hello-world", "Prints 'Hello, {name}!'")]
    public static async Task<int> HelloWorld([Injected] IRenderer renderer,
        [Option("n", "name", "The name to greet")]
        string name)
    {
        await renderer.RenderAsync(new Text { Template = $"Hello, [bold]{name}[/]!" });
        return 0;
    }


    [Verb("guid-table", "Prints a table of guids to test the table renderer")]
    public static async Task<int> GuidTable([Injected] IRenderer renderer,
        [Option("c", "count", "The name to greet")]
        int count)
    {
        count = count == 0 ? 10 : count;
        var rows = new List<IRenderable[]>();
        for (var i = 0; i < count; i++)
        {
            rows.Add([new Text { Template = i.ToString() }, new Text { Template = Guid.NewGuid().ToString() }]);
        }

        await renderer.RenderAsync(new Table
        {
            Columns =
            [
                new Text { Template = "Index" },
                new Text { Template = "Guid" }
            ],
            Rows = rows.ToArray()
        });
        return 0;
    }
}

class Handler(ILogger<Handler> logger, IFileSystem fileSystem, IServiceProvider provider) :
    AStartupHandler(logger, fileSystem)
{
    public override async Task<int> HandleCliCommandAsync(string[] args, IRenderer renderer, CancellationToken token)
    {
        try
        {
            logger.LogInformation("Running command: {Arguments}", string.Join(' ', args));
            var configurator = provider.GetRequiredService<CommandLineConfigurator>();
            return await configurator.RunAsync(args, renderer, token);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error running command");
            return -1;
        }
    }

    public override Task<int> StartUiWindowAsync()
    {
        logger.LogInformation("Starting UI window");
        return Task.FromResult(0);
    }
    public override string MainProcessArgument => "main-process";
}
