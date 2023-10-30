using System.Threading;
using System.Threading.Tasks;

namespace NexusMods.SingleProcess;

public interface IMainProcessHandler
{
    public Task Handle(ProxiedConsole console, CancellationToken token);
}
