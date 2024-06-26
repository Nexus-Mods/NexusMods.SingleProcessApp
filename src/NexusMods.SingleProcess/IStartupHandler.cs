using System.Threading;
using System.Threading.Tasks;
using NexusMods.ProxyConsole.Abstractions;

namespace NexusMods.SingleProcess;

/// <summary>
/// Contains the user-defined startup logic for the application.
/// </summary>
public interface IStartupHandler
{
    /// <summary>
    /// Called from the main process to handle the CLI arguments passed in from a client process.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="renderer"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> HandleCliCommandAsync(string[] args, IRenderer renderer, CancellationToken token = default);

    /// <summary>
    /// Called from the main process to handle the creation of a new UI window. The returned task should only complete
    /// when the UI window is closed.
    /// </summary>
    /// <returns></returns>
    public Task<int> StartUiWindowAsync();

    /// <summary>
    /// Creates a new process that will run the main code. This is called when the current process is not the main process
    /// and the current process wants to run a CLI command.
    /// </summary>
    /// <returns></returns>
    public Task StartMainProcess();

    /// <summary>
    /// When the application is called with this single CLI argument, it will start the main process.
    /// </summary>
    public string MainProcessArgument { get; }
}
