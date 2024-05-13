using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.Implementations;

namespace NexusMods.SingleProcess.TestApp.Commands;

public class GuidTable
{
    public static async Task<int> ExecuteAsync(IRenderer renderer, int count)
    {
        count = count == 0 ? 10 : count;
        var rows = new List<IRenderable[]>();
        for (var i = 0; i < count; i++)
        {
            rows.Add([new Text { Template = i.ToString() }, new Text { Template = Guid.NewGuid().ToString() }]);
        }
        await renderer.RenderAsync(new Table
        {
            Columns =
            [
                new Text { Template = "Index" },
                new Text { Template = "Guid" }
            ],
            Rows = rows.ToArray()
        });
        return 0;
    }
}
