using MemoryPack;
using NexusMods.ProxyConsole.Abstractions;

namespace NexusMods.ProxyConsole.Implementations;

[MemoryPackable]
public partial class Text : IRenderable
{
    public required string Template { get; init; }
}
