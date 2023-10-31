using System.Text;
using Spectre.Console;

namespace NexusMods.SingleProcess.TestApp;

public static class Globals
{
    public static AsyncLocal<IAnsiConsole> Console = new();

    public static void SetConsole(ProxiedConsole console)
    {
        Console.Value = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Detect,
            ColorSystem = ColorSystemSupport.Detect,
            Out = new AnsiConsoleOutput(new StreamWriter(console.StdOut, console.OutputEncoding))
        });
    }
}
