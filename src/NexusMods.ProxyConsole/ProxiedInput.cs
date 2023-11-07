using System;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace NexusMods.ProxyConsole;

public class ProxiedInput : IAnsiConsoleInput
{
    private readonly Serializer _rpc;

    public ProxiedInput(Serializer rpc)
    {
        _rpc = rpc;
    }

    public bool IsKeyAvailable()
    {
        throw new NotImplementedException();
    }

    public ConsoleKeyInfo? ReadKey(bool intercept)
    {
        throw new NotImplementedException();
    }

    public Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
