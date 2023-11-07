using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.ProxyConsole.Abstractions;

namespace NexusMods.SingleProcess.TestApp.Commands;

public class ServerMode
{
    private readonly MainProcessDirector _director;
    private readonly IServiceProvider _provider;
    private readonly ILogger<ServerMode> _logger;

    public ServerMode(ILogger<ServerMode> logger, MainProcessDirector director, IServiceProvider provider)
    {
        _logger = logger;
        _director = director;
        _provider = provider;

    }

    public async Task<int> ExecuteAsync()
    {
        await _director.TryStartMainAsync(new Handler(_provider));

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

    public async Task HandleAsync(string[] arguments, IRenderer console, CancellationToken token)
    {
        try
        {
            _logger.LogInformation("Running command: {Arguments}", string.Join(' ', arguments));
            var app = _provider.GetRequiredService<RootCommand>();
            var builder = new CommandLineBuilder(app);
            builder.AddMiddleware(async (ctx, next) =>
            {
                ctx.BindingContext.AddService(typeof(IRenderer), _ => console);
                await next(ctx);
            });
            await builder.Build().InvokeAsync(arguments);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error running command");
        }
    }
}
