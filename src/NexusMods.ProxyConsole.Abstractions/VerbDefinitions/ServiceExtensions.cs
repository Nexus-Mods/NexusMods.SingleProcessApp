using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

/// <summary>
/// Service extensions for verb definitions.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Registers a method as a CLI verb. The method must be static and have a return type of
    /// Task&lt;int&gt;.
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public static IServiceCollection AddVerb(this IServiceCollection services, MethodInfo info)
    {
        if (info.ReturnType != typeof(Task<int>))
        {
            throw new ArgumentException("Method must return a Task<int>", nameof(info));
        }

        var verbDefinition = info.GetCustomAttributes()
            .OfType<VerbAttribute>()
            .FirstOrDefault();
        if (verbDefinition is null)
        {
            throw new ArgumentException("Method must be marked with a VerbAttribute", nameof(info));
        }

        var options = new List<OptionDefinition>();
        foreach (var param in info.GetParameters())
        {
            var option = param.GetCustomAttribute<OptionAttribute>();
            var injected = param.GetCustomAttribute<InjectedAttribute>();
            if (option is null && injected is null)
            {
                throw new ArgumentException(
                    "Method parameters must be marked with either an OptionAttribute or an InjectedAttribute",
                    nameof(info));
            }

            if (option is not null && injected is not null)
            {
                throw new ArgumentException(
                    "Method parameters cannot be marked with both an OptionAttribute and an InjectedAttribute",
                    nameof(info));
            }

            if (option is not null)
            {
                options.Add(new OptionDefinition(param.ParameterType, option.ShortName, option.LongName, option.HelpText, false));
            }
            else if (injected is not null)
            {
                options.Add(new OptionDefinition(param.ParameterType, string.Empty, string.Empty, string.Empty, true));
            }
        }

        return services.AddSingleton(_ => new VerbDefinition(verbDefinition.Name, verbDefinition!.Description, info, options.ToArray()));

    }
}
