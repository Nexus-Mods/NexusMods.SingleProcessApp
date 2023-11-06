using System;
using NexusMods.Paths;

namespace NexusMods.SingleProcess;

public class SingleProcessSettings
{
    /// <summary>
    /// The path to the sync file, this file is used to publish the process id of the main process, and the TCP port it's listening on.
    /// </summary>
    public AbsolutePath SyncFile { get; set; } = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("single_process.sync");

    /// <summary>
    /// The size of the sync file, this should be at least the width of two uints. One will be used to store the process
    /// id of the main process, the other will be used to store the TCP port the main process is listening on.
    /// </summary>
    public Size SyncFileSize { get; init; } = Size.FromLong(8);

    /// <summary>
    /// The start of the port range to use when attempting to start a TCP listener. The actual port will be randomly selected
    /// and will be between this and <see cref="PortMax"/>, based on what ports are available on the system.
    /// </summary>
    public int PortMin { get; set; } = 10000;

    /// <summary>
    /// The end of the port range to use when attempting to start a TCP listener. The actual port will be randomly selected
    /// between this and <see cref="PortMin"/>, based on what ports are available on the system.
    /// </summary>
    public int PortMax { get; set; } = 20000;

    /// <summary>
    /// The path to the main application, this is the application that will be started if it's not already running.
    /// </summary>
    public required AbsolutePath MainApplication { get; set; }

    /// <summary>
    /// The arguments to pass to the main application if it's not already running. This
    /// </summary>
    public required string[] MainApplicationArgs { get; set; } = Array.Empty<string>();

    /// <summary>
    /// The amount of time the TCPListener will pause waiting for new connections before checking if it should exit.
    /// </summary>
    public TimeSpan ListenTimeout { get; set; } = TimeSpan.FromSeconds(300);

    /// <summary>
    /// The amount of time the main process will wait for new connections before terminating.
    /// </summary>
    public TimeSpan StayRunningTimeout { get; set; } = TimeSpan.FromSeconds(300);

}
