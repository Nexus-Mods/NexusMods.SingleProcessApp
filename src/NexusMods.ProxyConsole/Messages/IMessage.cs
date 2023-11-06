using MemoryPack;

namespace NexusMods.ProxyConsole.Messages;

[MemoryPackable]
[MemoryPackUnion(0x1, typeof(ProgramArgumentsRequest))]
[MemoryPackUnion(0x2, typeof(ProgramArgumentsResponse))]
[MemoryPackUnion(0x3, typeof(Render))]
[MemoryPackUnion(ushort.MaxValue, typeof(Acknowledge))]
public partial interface IMessage
{

}
