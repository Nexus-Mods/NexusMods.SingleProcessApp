using MemoryPack;
using NexusMods.ProxyConsole.Abstractions;

namespace NexusMods.ProxyConsole.Abstractions.Implementations;

[MemoryPackable]
public partial class Table : IRenderable
{
    public required IRenderable[] Columns { get; init; }
    public required IRenderable[][] Rows { get; init; }
}
