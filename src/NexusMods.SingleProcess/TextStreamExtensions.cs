using System.IO;
using System.Text;
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

    public static async Task CopyToAsync(this TextReader src, Stream dest)
    {
        var buffer = new char[1024];
        while (true)
        {
            var read = await src.ReadAsync(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            var bytes = Encoding.UTF8.GetBytes(buffer, 0, read);
            await dest.WriteAsync(bytes, 0, bytes.Length);
            await dest.FlushAsync();
        }
    }

    public static async Task CopyToAsync(this Stream src, TextWriter dest)
    {
        var buffer = new byte[1024];
        while (true)
        {
            var read = await src.ReadAsync(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            var str = Encoding.UTF8.GetString(buffer, 0, read);
            await dest.WriteAsync(str);
            await dest.FlushAsync();
        }
    }
}
