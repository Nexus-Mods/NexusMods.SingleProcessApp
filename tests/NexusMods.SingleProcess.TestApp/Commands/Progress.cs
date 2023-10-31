using Spectre.Console;
using Spectre.Console.Cli;

namespace NexusMods.SingleProcess.TestApp.Commands;

public class Progress : AsyncCommand<Progress.Settings>
{
    private readonly ScopedConsole _console;

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[Tasks]")]
        public int Tasks { get; set; }
    }

    public Progress(ScopedConsole console)
    {
        _console = console;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Progress.Settings settings)
    {
        await _console.Console.Progress()
            .AutoClear(false)
            .Columns(new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                List<ProgressTask> progressTasks = new();
                for (var i = 0; i < settings.Tasks; i++)
                {
                    progressTasks.Add(ctx.AddTask($"Task {i + 1}"));
                }

                while(!ctx.IsFinished)
                {
                    await Task.Delay(100);
                    for (var i = 0; i < settings.Tasks; i++)
                    {
                        progressTasks[i].Increment(Random.Shared.Next(0, 10));
                    }
                }
            });

        return 0;
    }
}
