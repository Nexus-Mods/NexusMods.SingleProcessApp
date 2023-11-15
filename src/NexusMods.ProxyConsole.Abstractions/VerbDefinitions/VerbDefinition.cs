using System.Reflection;

namespace NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

public record VerbDefinition(string Name, string Description, MethodInfo Info, OptionDefinition[] Options);
