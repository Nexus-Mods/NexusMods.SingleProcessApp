using System;

namespace NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

public record OptionDefinition(Type Type, string ShortName, string LongName, string HelpText, bool IsInjected);
