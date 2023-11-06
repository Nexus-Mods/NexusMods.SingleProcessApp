using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NexusMods.ProxyConsole;

public static class StreamExtensions
{
    public static async Task CopyToAsync(this TextReader input, TextWriter output)
    {
        var buffer = new char[4096];
        int read;
        while ((read = await input.ReadAsync(buffer, 0, buffer.Length)) != 0)
        {
            await output.WriteAsync(buffer, 0, read);
            await output.FlushAsync();
            Thread.MemoryBarrier();
        }
    }
}
