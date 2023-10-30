namespace NexusMods.SingleProcess;

public enum Channel
{
    /// <summary>
    /// The main process will echo anything it gets on this channel, back on the same channel
    /// </summary>
    Echo,
    /// <summary>
    /// The Console StdIn from Client -> Main
    /// </summary>
    StdIn,
    /// <summary>
    /// The Console StdOut from Main -> Client
    /// </summary>
    StdOut,

    /// <summary>
    /// The Console StdErr from Main -> Client
    /// </summary>
    StdErr,

}
