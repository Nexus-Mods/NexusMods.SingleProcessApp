using System.IO;

namespace NexusMods.SingleProcess;

public class ProxiedConsole
{
    /// <summary>
    /// The Console's standard input stream (stdin)
    /// </summary>
    public required Stream StdIn { get; init; }

    /// <summary>
    /// The Console's standard output stream (stdout)
    /// </summary>
    public required Stream StdOut { get; init; }

    /// <summary>
    /// The Console's standard error stream (stderr)
    /// </summary>
    public required Stream StdErr { get; init; }

    /// <summary>
    /// The arguments passed to the application on startup
    /// </summary>
    public required string[] Args { get; init; }
}
