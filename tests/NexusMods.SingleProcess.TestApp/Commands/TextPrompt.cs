using Spectre.Console;
using Spectre.Console.Cli;

namespace NexusMods.SingleProcess.TestApp.Commands;

public class TextPrompt : AsyncCommand
{
    private readonly ScopedConsole _console;

    public TextPrompt(ScopedConsole console)
    {
        _console = console;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var value = Globals.Console.Value!.Ask<string>("What is your [green]name[/]?");
        AnsiConsole.MarkupLine($"Hello [bold][blue]{value}[/][/]!");

        return 0;
    }
}
