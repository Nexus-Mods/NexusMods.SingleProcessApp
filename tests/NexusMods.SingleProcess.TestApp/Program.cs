using System.CommandLine;
using System.CommandLine.Binding;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        s.AddSingleton<MainProcessDirector>();
        s.AddSingleton<ClientProcessDirector>();

        s.AddSingleton<SingleProcessSettings>(s =>
        {
            return new SingleProcessSettings
            {
                MainApplication = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory),
                MainApplicationArgs = new[] { "server-mode" }
            };
        });

        s.AddSingleton<ServerMode>();

        var rootCommand = new RootCommand();
        var helloWorld = new Command("hello-world");
        var option = new Option<string>("--name");
        helloWorld.Add(option);
        helloWorld.SetHandler(async (renderer, arg) => await HelloWorld.ExecuteAsync(renderer, arg), new RendererBinder(), option);
        rootCommand.AddCommand(helloWorld);

        s.AddSingleton<RootCommand>(_ => rootCommand);

    }).Build();

Console.OutputEncoding = Encoding.UTF8;

if (args[0] == "server-mode")
{
    using var scope = host.Services.CreateScope();
    var app = scope.ServiceProvider.GetRequiredService<ServerMode>();
    await app.ExecuteAsync();
}
else
{
    var client = host.Services.GetRequiredService<ClientProcessDirector>();
    await client.StartClient(new ConsoleSettings
    {
        Arguments = args,
        Renderer = new SpectreRenderer(AnsiConsole.Console)
    });
}


class RendererBinder : BinderBase<IRenderer>
{
    protected override IRenderer GetBoundValue(BindingContext bindingContext)
    {
        return bindingContext.GetRequiredService<IRenderer>();
    }
}
