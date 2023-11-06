using MemoryPack;
using NexusMods.ProxyConsole.Implementations;

namespace NexusMods.ProxyConsole.Abstractions;

[MemoryPackable]
[MemoryPackUnion(1, typeof(Text))]
[MemoryPackUnion(2, typeof(Table))]
public partial interface IRenderable
{

}
