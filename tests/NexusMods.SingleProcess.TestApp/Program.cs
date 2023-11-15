using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using NexusMods.ProxyConsole;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.SingleProcess;
using NexusMods.SingleProcess.TestApp.Commands;
using Spectre.Console;


var host = Host.CreateDefaultBuilder()
    .ConfigureServices(s =>
    {
        s.AddLogging();
        s.AddFileSystem();
        s.AddSingleProcess((_, s) => s);
        s.AddDefaultRenderers();

        s.AddSingleton<IStartupHandler, Handler>();

        s.AddSingleton<SingleProcessSettings>();

        var rootCommand = new RootCommand();

        var helloWorld = new Command("hello-world");
        var option = new Option<string>("--name");
        helloWorld.Add(option);
        helloWorld.SetHandler(async (renderer, arg) => await HelloWorld.ExecuteAsync(renderer, arg), new RendererBinder(), option);
        rootCommand.AddCommand(helloWorld);

        var guidTable = new Command("guid-table");
        var countOption = new Option<int>("--count");
        guidTable.Add(countOption);
        guidTable.SetHandler(async (renderer, arg) => await GuidTable.ExecuteAsync(renderer, arg), new RendererBinder(), countOption);
        rootCommand.AddCommand(guidTable);

        s.AddSingleton<RootCommand>(_ => rootCommand);

    }).Build();

Console.OutputEncoding = Encoding.UTF8;

var startupDirector = host.Services.GetRequiredService<StartupDirector>();
return await startupDirector.Start(args);

class RendererBinder : BinderBase<IRenderer>
{
    protected override IRenderer GetBoundValue(BindingContext bindingContext)
    {
        return bindingContext.GetRequiredService<IRenderer>();
    }
}

class Handler(ILogger<Handler> logger, IFileSystem fileSystem, IServiceProvider provider) : AStartupHandler(logger, fileSystem)
{
    public override async Task<int> HandleCliCommandAsync(string[] args, IRenderer renderer, CancellationToken token)
    {
        try
        {
            logger.LogInformation("Running command: {Arguments}", string.Join(' ', args));
            var app = provider.GetRequiredService<RootCommand>();
            var builder = new CommandLineBuilder(app);
            builder.AddMiddleware(async (ctx, next) =>
            {
                ctx.BindingContext.AddService(typeof(IRenderer), _ => renderer);
                await next(ctx);
            });
            return await builder.Build().InvokeAsync(args);
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
