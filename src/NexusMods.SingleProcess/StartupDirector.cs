using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.ProxyConsole;
using NexusMods.ProxyConsole.Abstractions;
using Spectre.Console;

namespace NexusMods.SingleProcess;

/// <summary>
/// A class that will either create a new UI instance or call the main process passing in CLI arguments,
/// depending on the state of the system.
/// </summary>
public class StartupDirector
{
    private readonly ILogger<StartupDirector> _logger;
    private readonly SingleProcessSettings _settings;
    private readonly IStartupHandler _handler;
    private readonly IServiceProvider _provider;
    private MainProcessDirector? _mainProcessDirector = null;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="settings"></param>
    public StartupDirector(ILogger<StartupDirector> logger, SingleProcessSettings settings, IStartupHandler startupHandler, IServiceProvider provider)
    {
        _logger = logger;
        _settings = settings;
        _handler = startupHandler;
        _provider = provider;
    }

    /// <summary>
    /// Starts the application. If arguments are passed this will attempt to start the main process, and funnel them to it,
    /// unless the main process is already running, in which case it will forward the command to the existing main process.
    /// If debug mode is enabled, the main process will never be started, and the code will run directly in this process,
    /// to allow for easier use with a debugger.
    ///
    /// If a IAnsiConsole is passed in, it will be used to render the CLI commands, otherwise AnsiConsole.Console will be used
    /// </summary>
    /// <param name="args"></param>
    /// <param name="debugMode"></param>
    /// <param name="console"></param>
    /// <returns></returns>
    public async Task<int> Start(string[] args, bool debugMode = false, IAnsiConsole? console = null)
    {
        if (debugMode)
        {
            if (args.Length == 0)
            {
                return await _handler.StartUiWindowAsync();
            }
            else if (args[1] == _handler.MainProcessArgument)
            {
                return await StartMainProcessDirector();
            }
            else
            {
                return await _handler.HandleCliCommandAsync(args, new SpectreRenderer(console ?? AnsiConsole.Console));
            }
        }
        else
        {
            _logger.LogInformation("Starting application with args: {Arguments}", string.Join(' ', args));
            if (args.Length == 0)
            {
                await _handler.StartMainProcessAsync();
                return await SendCommandAsync(args, console);
            }
            else if (args[0] == _handler.MainProcessArgument)
            {
                return await StartMainProcessDirector();
            }
            else
            {
                await _handler.StartMainProcessAsync();
                return await SendCommandAsync(args, console);
            }
        }

    }

    /// <summary>
    /// Connects to the main process and sends the CLI arguments to it.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="console"></param>
    /// <returns></returns>
    private async Task<int> SendCommandAsync(string[] args, IAnsiConsole? console)
    {
        var sw = Stopwatch.StartNew();
        Exception? lastException = null;
        while (sw.Elapsed < _settings.ClientConnectTimeout)
        {
            var client = _provider.GetRequiredService<ClientProcessDirector>();
            try
            {
                await client.StartClientAsync(new ConsoleSettings
                {
                    Arguments = args,
                    Renderer = new SpectreRenderer(console ?? AnsiConsole.Console)
                });
                return 0;
            }
            catch (Exception ex)
            {
                //_logger.LogWarning(ex, "Failed to connect to main process");
                lastException = ex;
                await Task.Delay(100);
                continue;
            }
        }
        _logger.LogError(lastException, "Failed to connect to main process");
        return -1;
    }

    /// <summary>
    /// Starts the main process director locally
    /// </summary>
    /// <returns></returns>
    private async Task<int> StartMainProcessDirector()
    {
        _mainProcessDirector = _provider.GetRequiredService<MainProcessDirector>();
        await _mainProcessDirector.TryStartMainAsync(new Handler(this));
        while (_mainProcessDirector.IsListening)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
        return 0;
    }

    private class Handler(StartupDirector director) : IMainProcessHandler
    {
        public async Task HandleAsync(string[] arguments, IRenderer console, CancellationToken token)
        {
            if (arguments.Length == 0)
            {
                using var _ = director._mainProcessDirector!.MakeKeepAliveToken();
                await director._handler.StartUiWindowAsync();
                return;
            }
            await director._handler.HandleCliCommandAsync(arguments, console, token);
        }
    }



}