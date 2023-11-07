using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Implementations;

namespace NexusMods.Spectre.ProxyConsole.Tests;

public class ProxyTests : AProxyConsoleTest
{
    [Fact]
    public async Task CanRenderTest()
    {
        await Server.RenderAsync(new Text {Template = "Hello World!"});

        LoggingRenderer.Messages.Should().BeEquivalentTo(new List<(string Method, object Message)>
        {
            ("RenderAsync", new Text {Template = "Hello World!"})
        });
    }

    [Fact]
    public async Task CanRenderMultipleTexts()
    {
        await Server.RenderAsync(new Text {Template = "Hello World!1"});
        await Server.RenderAsync(new Text {Template = "Hello World!2"});
        await Server.RenderAsync(new Text {Template = "Hello World!3"});

        LoggingRenderer.Messages.Should().BeEquivalentTo(new List<(string Method, object Message)>
        {
            ("RenderAsync", new Text {Template = "Hello World!1"}),
            ("RenderAsync", new Text {Template = "Hello World!2"}),
            ("RenderAsync", new Text {Template = "Hello World!3"})
        });
    }

    [Fact]
    public async Task CanRenderTable()
    {
        await Server.RenderAsync(new Table
        {
            Rows = new[]
            {
                new IRenderable[] { new Text { Template = "Hello World!1" } , new Text { Template = "4" }},
                new IRenderable[] { new Text { Template = "Hello World!2" } , new Text { Template = "5" }},
            }
        });


        LoggingRenderer.Messages.Should().BeEquivalentTo(new List<(string Method, object Message)>
        {
            ("RenderAsync",
                new Table
                {
                    Rows = new[]
                    {
                        new IRenderable[] { new Text { Template = "Hello World!1" }, new Text { Template = "4" }},
                        new IRenderable[] { new Text { Template = "Hello World!2" }, new Text { Template = "5" }},
                    }
                })
        });
    }


}