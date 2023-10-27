using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NexusMods.SingleProcess;

public abstract class ADirector : IAsyncDisposable
{
    protected readonly SingleProcessSettings Settings;
    protected ISharedArray? SharedArray;

    protected ADirector(SingleProcessSettings settings)
    {
        Settings = settings;
    }


    /// <inheritdoc />
    public abstract ValueTask DisposeAsync();


    /// <summary>
    /// Connect to the shared array
    /// </summary>
    protected void ConnectSharedArray()
    {
        SharedArray = new MultiProcessSharedArray(Settings.SyncFile, (int)(Settings.SyncFileSize.Value / 8));
    }

    /// <summary>
    /// Returns true if this process is the main process
    /// </summary>
    public bool IsMainProcess => SharedArray is not null && GetSyncInfo().Process is not null;

    /// <summary>
    /// Returns the current process and port that's stored in the sync file
    /// </summary>
    /// <returns></returns>
    protected (Process? Process, int Port) GetSyncInfo()
    {
        var val = SharedArray!.Get(0);
        var pid = (int)(val >> 32);
        var port = (int)(val & 0xFFFFFFFF);

        if (pid == 0)
            return (null, port);

        try
        {
            return (Process.GetProcessById(pid), port);
        }
        catch (ArgumentException)
        {
            return (null, port);
        }
    }
}
