// See https://aka.ms/new-console-template for more information


using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusMods.Paths;
using NexusMods.ProxyConsole;
using NexusMods.SingleProcess;
using NexusMods.SingleProcess.TestApp;
using NexusMods.SingleProcess.TestApp.Commands;
using Spectre.Console;
using Spectre.Console.Cli;
using Progress = NexusMods.SingleProcess.TestApp.Commands.Progress;


var host = Host.CreateDefaultBuilder()
    .ConfigureServices(s =>
    {
        var registrar = new TypeRegistrar(s);
        s.AddLogging();
        s.AddSingleton<MainProcessDirector>();
        s.AddSingleton<ClientProcessDirector>();

        s.AddSingleton<ScopedConsole>();

        //s.Remove(s.First(d => d.ImplementationType == typeof(IAnsiConsole)));


        s.AddSingleton<SingleProcessSettings>(s =>
        {
            return new SingleProcessSettings
            {
                MainApplication = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory),
                MainApplicationArgs = new[] { "server-mode" }
            };
        });
        s.AddScoped<CommandApp>(s =>
        {
            var app = new CommandApp(registrar);
            app.Configure(c =>
            {
                c.AddCommand<Progress>("progress");
                c.AddCommand<ServerMode>("server-mode");
                c.AddCommand<TextPrompt>("text-prompt");
            });
            return app;
        });



        s.AddScoped<IAnsiConsole>(provider => provider.GetRequiredService<ScopedConsole>().Console);
    }).Build();

Console.OutputEncoding = Encoding.UTF8;

if (args[0] == "server-mode")
{
    using var scope = host.Services.CreateScope();
    var app = scope.ServiceProvider.GetRequiredService<CommandApp>();
    await app.RunAsync(args);
}
else
{
    var client = host.Services.GetRequiredService<ClientProcessDirector>();
    await client.StartClient(new ConsoleSettings
    {
        Arguments = args,
        Renderer = new SpectreRenderer(AnsiConsole.Console)
    });
}


public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _builder;

    public TypeRegistrar(IServiceCollection builder)
    {
        _builder = builder;
    }

    public ITypeResolver Build()
    {
        return new TypeResolver(_builder.BuildServiceProvider());
    }

    public void Register(Type service, Type implementation)
    {
        _builder.AddSingleton(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _builder.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> func)
    {
        if (service == typeof(IAnsiConsole))
            return;

        if (func is null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        _builder.AddSingleton(service, (provider) => func());
    }
}

public sealed class TypeResolver : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _provider;

    public TypeResolver(IServiceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public object? Resolve(Type? type)
    {
        if (type == null)
        {
            return null;
        }

        return _provider.GetService(type);
    }

    public void Dispose()
    {
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
