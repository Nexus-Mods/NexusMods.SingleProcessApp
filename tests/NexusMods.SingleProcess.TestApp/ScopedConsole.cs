using System.Text;
using Spectre.Console;

namespace NexusMods.SingleProcess.TestApp;

public class ScopedConsole
{
    public IAnsiConsole Console
    {
        get
        {
            if (_console == null)
            {
                throw new InvalidOperationException("Console has not been set.");
            }
            return _console!;
        }
    }

    private IAnsiConsole? _console;

    public IAnsiConsole SetConsole(ProxiedConsole console)
    {
        _console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Detect,
            ColorSystem = ColorSystemSupport.Detect,
            Out = new AnsiConsoleOutput(new StreamWriter(console.StdOut))
        });
        return _console;
    }
}
