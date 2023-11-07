---
hide:
  - toc
---

<div align="center">
	<h1>NexusMods.SingleProcessApp</h1>
	<img src="./Nexus/Images/Nexus-Icon.png" width="150" align="center" />
	<br/> <br/>
    An opinionated, open source, cross-platform, single process application framework for the Nexus Mods App
    <br/>
</div>

## About

This library allows an application to be run as a single process, with multiple windows (and fully multithreaded) while
still allowing rich communication between the main process and the app's CLI.

## Motivation

It became clear that the Nexus Mods App would benefit significantly from being a single process application, issues such
as synchronisation of data between processes and message lag between windows were becoming a problem. This library attempts
to solve these issues by providing a framework for a single process application, where the UI is run through a "Main" process
and communication with the CLI is done through child processes connecting over IPC.

Non-goals of this library include multiple implementations of the IPC system (everything uses localhost tcp for simplicity),
and multiple CLI rendering/interaction techs. In this app the CLI is abstracted away and a default rendering implementation
is provided for Spectre. This is where the "opinionated" part of the library comes in, it is designed to be used for apps
structured in a similar way to the Nexus Mods App.

## Technical Details

There are two modes the app can run in, the "Client Mode", and the "Main Process" mode. These modes are managed by the
`ClientProcessDirector` and `MainProcessDirector` classes respectively.

When the app is run in `MainProcess` mode, the `MainProcessDirector` first opens the sync file. This sync file is a small
(8 byte) file that is memory mapped into the process space and manipulated via `CAS` (Atomic Compare and Swap) operations.
The first 4 bytes of the file are used to store the process ID of the process that currently owns the lock, and the second
4 bytes stores the TCP port that the process is listening on. The `MainProcessDirector` will attempt to acquire the lock
by seeing if the sync file contains valid data. If the file is not blank, but the process ID in the file does not match
any running processes, the sync file is considered to be orphaned and the `MainProcessDirector` will attempt to overwrite
the sync file with its own process ID (atomically). If the process ID matches a running process, the `MainProcessDirector`
will throw an error.

Once the sync file has been acquired, the `MainProcessDirector` will start listening on the TCP port randomly selected
(settings for this port range are part of the `SingleProcessAppSettings` class). Once the TCP port is listening, the
`MainProcessDirector` will listen for clients, and as they come in, hand them off to the provided `IMainProcessHandler`.
This handler receives CLI arguments, and provides user code with a `IRenderer` which can be used to display information
on the client process's console.

Likewise, in `Client Mode`, the `ClientProcessDirector` will attempt to connect to the sync file. If no valid information
can be found in the sync file, the `ClientProcessDirector` will throw an error. If the process ID in the sync file matches
and the TCP port is listening, the `ClientProcessDirector` will connect to the TCP port and pipe all render responses from the main
process to the provided `IRenderer`. The `ClientProcessDirector` will also pipe all CLI arguments to the main process
(when requested by the main process).

```csharp
// Setup the two Directors
await using var main = MainProcessDirector.Create(_serviceProvider);
await using var client = ClientProcessDirector.Create(_serviceProvider);

// This handler is below, and used to handle connecting clients
var handler = new EchoArgsHandler(_logger);
await main.TryStartMain(handler);

// A test console that the client will send server render requests to
var testConsole = new TestConsole();


// Start the client, and send some arguments and a renderer
await client.StartClient(new ConsoleSettings
{
    Arguments = new[] {"Some", "Args", "Here"},
    // Wrap the the Spectre IAnsiConsole in a IRenderer
    Renderer = new SpectreRenderer(testConsole)
});

// Syncronize the client and server
(await handler.Handled).Should().BeEquivalentTo("Some", "Args", "Here");

// Validate the output
testConsole.Output.Should().Be("Hello World! - Some|Args|Here");

// Handler code here
public class EchoArgsHandler : IMainProcessHandler
{
    private readonly ILogger _handlerLogger;
    private readonly TaskCompletionSource<string[]> _handled;

    public Task<string[]> Handled => _handled.Task;

    public EchoArgsHandler(ILogger logger)
    {
        _handlerLogger = logger;
        _handled = new TaskCompletionSource<string[]>();
    }
    public async Task HandleAsync(string[] arguments, IRenderer console, CancellationToken token)
    {
        // Render the arguments back to the client
        await console.RenderAsync(new Text { Template = $"Hello World! - {string.Join('|', arguments)}" });
        // This is just used for syncronization for testing.
        // Render calls are async but follow a RPC pattern so, once the above call completes, the client will have
        // received the message, and rendered it to the console.
        _handled.SetResult(arguments);
    }
}
```
