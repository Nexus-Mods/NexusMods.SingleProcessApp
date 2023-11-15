using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.SingleProcess;

/// <summary>
/// A configurator for the commandline parser. It looks for all verb definitions (created by AddVerb) and
/// and adds them to the parser. It also adds all injected types to the binding context. The RunAsync method
/// can be used to run the parser and execute the matching verb.
/// </summary>
public class CommandLineConfigurator
{
    private readonly RootCommand _rootCommand;
    private readonly Type[] _injectedTypes;
    private readonly IServiceProvider _provider;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="verbDefinitions"></param>
    public CommandLineConfigurator(IServiceProvider provider, IEnumerable<VerbDefinition> verbDefinitions)
    {
        (_rootCommand, _injectedTypes) = MakeRootCommand(verbDefinitions);
        _provider = provider;
    }

    /// <summary>
    /// Builds the root command and returns it, along with the list of injected types.
    /// </summary>
    /// <param name="verbDefinitions"></param>
    /// <returns></returns>
    private (RootCommand, Type[]) MakeRootCommand(IEnumerable<VerbDefinition> verbDefinitions)
    {
        var rootCommand = new RootCommand();
        var injectedTypes = new HashSet<Type>();

        foreach (var verbDefinition in verbDefinitions)
        {
            var command = new Command(verbDefinition.Name, verbDefinition.Description);
            var getters = new List<Func<InvocationContext, object?>>();

            foreach (var optionDefinition in verbDefinition.Options)
            {
                if (optionDefinition.IsInjected)
                {
                    // Injected options are pulled from the service provider via the binding context
                    injectedTypes.Add(optionDefinition.Type);
                    getters.Add(ctx => ctx.BindingContext.GetRequiredService(optionDefinition.Type));
                }
                else
                {
                    var optionType = typeof(Option<>).MakeGenericType(optionDefinition.Type);

                    var aliases = new[] { "-" + optionDefinition.ShortName, "--" + optionDefinition.LongName };
                    var option = (Option)Activator.CreateInstance(optionType, aliases, optionDefinition.HelpText)!;
                    command.AddOption(option);
                    getters.Add(ctx => ctx.ParseResult.GetValueForOption(option));
                }
            }
            command.Handler = new CommandHandler(getters, verbDefinition.Info);

            rootCommand.AddCommand(command);
        }
        return (rootCommand, injectedTypes.ToArray());
    }

    /// <summary>
    /// Runs the commandline parser and executes the verb using the given renderer and arguments.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="renderer"></param>
    /// <returns></returns>
    public async Task<int> RunAsync(string[] args, IRenderer renderer)
    {
        var parser = new CommandLineBuilder(_rootCommand)
            .AddMiddleware((ctx, next) =>
            {
                foreach (var type in _injectedTypes)
                {
                    if (type == typeof(IRenderer))
                        ctx.BindingContext.AddService(type, _ => renderer);
                    else
                        ctx.BindingContext.AddService(type, _ => _provider.GetRequiredService(type));
                }
                return next(ctx);
            })
            .Build();

        return await parser.InvokeAsync(args);
    }
}
