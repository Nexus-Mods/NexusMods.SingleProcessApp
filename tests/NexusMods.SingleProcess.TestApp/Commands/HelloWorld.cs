using Spectre.Console;
using Spectre.Console.Cli;

namespace NexusMods.SingleProcess.TestApp.Commands;

public class HelloWorld : AsyncCommand<HelloWorld.Settings>
{
    public class Settings : CommandSettings
    {
        /// <summary>
        /// The name to say hello to
        /// </summary>
        [CommandArgument(0, "[Name]")]
        public string? Name { get; set; }
    }

    public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.MarkupLine($"Hello [bold][blue]{settings.Name}[/][/]!");
        return Task.FromResult(0);
    }
}
