using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.ProxyConsole.Abstractions.Implementations;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.SingleProcess.Tests;

public class CliParserTests(CommandLineConfigurator configurator)
{
    private readonly LoggingRenderer _renderer = new();

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private CancellationToken Token => _cancellationTokenSource.Token;
    public static IServiceCollection AddCliParserTestVerbs(IServiceCollection service)
    {
        service.AddVerb(() => CancelationTokenVerb);
        service.AddVerb(() => ParserError);
        return service;

    }

    [Verb("cancellation-token", "Test of the CancelationToken")]
    internal static Task<int> CancelationTokenVerb([Injected] CancellationToken token)
    {
        token.Should().NotBeNull();
        return Task.FromResult(0);
    }


    [Fact]
    public async Task CanInjectCancellationTokens()
    {
        await configurator.RunAsync(new[] { "cancellation-token" }, _renderer, Token);
    }


    [Fact]
    public async Task NoArgsPrintsHelp()
    {
        await configurator.RunAsync(new string[] {"--help"}, _renderer, Token);
        _renderer.Log.OfType<Text>()
            .Select(t => t.Template)
            .Aggregate((acc, itm) => acc + itm)
            .Should().Contain("  cancellation-token  Test of the CancelationToken");
    }


    [Verb("parser-error", "Test of how error handling is done")]
    internal static Task<int> ParserError([Option("t", "testOption", "Test option")] int test)
    {
        return Task.FromResult(test);
    }

    [Fact]
    public async Task ParseErrorsAreHandled()
    {
        await configurator.RunAsync(new[] { "parser-error", "-t", "test" }, _renderer, Token);
        _renderer.Log.OfType<Text>()
            .Select(t => t.Template)
            .Aggregate((acc, itm) => acc + itm)
            .Should().Contain("Invalid integer");
    }

}
