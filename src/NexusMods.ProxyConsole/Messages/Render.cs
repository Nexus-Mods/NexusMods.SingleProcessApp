using MemoryPack;
using NexusMods.ProxyConsole.Abstractions;

namespace NexusMods.ProxyConsole.Messages;

[MemoryPackable]
public partial class Render : IMessage
{
    public required IRenderable Renderable { get; init; }
}
