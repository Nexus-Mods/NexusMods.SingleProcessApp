using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NexusMods.SingleProcess;

public static class TextStreamExtensions
{

    public static async Task CopyTo(this TextReader reader, TextWriter writer, CancellationToken token)
    {
        var buffer = new char[1024];
        while (!token.IsCancellationRequested)
        {
            var read = await reader.ReadAsync(buffer, token);
            if (read == 0)
            {
                break;
            }

            await writer.WriteAsync(buffer, 0, read);
            await writer.FlushAsync();
        }
    }
}
