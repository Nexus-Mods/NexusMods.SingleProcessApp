using System;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.SingleProcess;

/// <summary>
/// Extension methods for the service collection.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Add the single process services to the service collection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configFn"></param>
    /// <returns></returns>
    public static IServiceCollection AddSingleProcess(this IServiceCollection services,
        Func<IServiceProvider, SingleProcessSettings, SingleProcessSettings> configFn) =>
        services.AddSingleton<MainProcessDirector>()
            .AddSingleton<ClientProcessDirector>()
            .AddSingleton<StartupDirector>()
            .AddSingleton<SingleProcessSettings>(s => configFn(s, new SingleProcessSettings()));
}
