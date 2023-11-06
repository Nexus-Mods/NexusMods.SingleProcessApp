using System.Collections.Generic;
using MemoryPack;
using NexusMods.ProxyConsole.Abstractions;
using Spectre.Console;

namespace NexusMods.ProxyConsole.Implementations;

[MemoryPackable]
public partial class Table : IRenderable
{
    public required IRenderable[][] Rows { get; init; }
}
