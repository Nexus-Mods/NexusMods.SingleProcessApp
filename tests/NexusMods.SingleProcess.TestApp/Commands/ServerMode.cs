using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NexusMods.SingleProcess.TestApp.Commands;

public class ServerMode : AsyncCommand<ServerMode.Settings>
{
    private readonly MainProcessDirector _director;
    private readonly IServiceProvider _provider;
    private readonly ILogger<ServerMode> _logger;

    public class Settings : CommandSettings
    {

    }

    public ServerMode(ILogger<ServerMode> logger, MainProcessDirector director, IServiceProvider provider)
    {
        _logger = logger;
        _director = director;
        _provider = provider;

    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        await _director.TryStartMain(new Handler(_provider));

        while (_director.IsListening)
        {
            _logger.LogInformation("Waiting for main process to exit: {IsRunning}", _director.IsListening);
            await Task.Delay(5000);
        }

        return 0;
    }
}



class Handler : IMainProcessHandler
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<Handler> _logger;

    public Handler(IServiceProvider provider)
    {
        _provider = provider;
        _logger = provider.GetRequiredService<ILogger<Handler>>();
    }

    public async Task Handle(ProxiedConsole console, CancellationToken token)
    {
        try
        {
            await using var scope = _provider.CreateAsyncScope();
            var converted = scope.ServiceProvider.GetRequiredService<ScopedConsole>().SetConsole(console);

            var diConsole = scope.ServiceProvider.GetRequiredService<IAnsiConsole>();
            if (converted != diConsole)
            {
                _logger.LogError("Console is not the same instance");
            }

            var app = scope.ServiceProvider.GetRequiredService<CommandApp>();
            await app.RunAsync(console.Args);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error running command");
        }
    }
}
