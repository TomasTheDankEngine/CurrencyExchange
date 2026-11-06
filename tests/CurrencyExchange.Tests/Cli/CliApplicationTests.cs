using CurrencyExchange.Application;
using CurrencyExchange.Cli;
using CurrencyExchange.Domain;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CurrencyExchange.Tests.Cli;

/// <summary>
/// Conversions themselves are covered end to end in CliPipelineTests.
/// These tests cover the input loop and exception handling.
/// </summary>
public class CliApplicationTests
{
    private readonly ICurrencyConverter _converter = Substitute.For<ICurrencyConverter>();
    private readonly StringWriter _output = new();

    public CliApplicationTests()
    {
        var conversion = new ConversionResult(CurrencyPair.Parse("EUR/DKK").Value, 1m, 7.4394m, new DateOnly(2026, 11, 5), "Test");
        _converter.ConvertAsync(Arg.Any<CurrencyPair>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(Result<ConversionResult>.Success(conversion));
    }

    private Task RunAsync(string input) =>
        new CliApplication(_converter, new StringReader(input), _output).RunAsync(TestContext.Current.CancellationToken);

    [Fact]
    public async Task RunAsync_ConvertsEachLineUntilExit()
    {
        await RunAsync("EUR/DKK 1\n\nEUR/DKK\neur/dkk 1\nexit\nEUR/DKK 1\n");

        Assert.StartsWith(CliApplication.Welcome, _output.ToString());
        Assert.Contains(InputParser.ExpectedFormat, _output.ToString());
        await _converter.Received(2).ConvertAsync(Arg.Any<CurrencyPair>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_EndOfInput_Stops()
    {
        await RunAsync("EUR/DKK 1");

        Assert.Contains("1 EUR = 7.4394 DKK", _output.ToString());
    }

    [Fact]
    public async Task RunAsync_UnexpectedException_WritesErrorAndContinues()
    {
        _converter.ConvertAsync(Arg.Any<CurrencyPair>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Boom"));

        await RunAsync("EUR/DKK 1\nEUR/DKK 2\n");

        Assert.Contains("Unexpected error: Boom", _output.ToString());
        await _converter.Received(2).ConvertAsync(Arg.Any<CurrencyPair>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_Cancelled_WritesCancelledAndStops()
    {
        _converter.ConvertAsync(Arg.Any<CurrencyPair>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await RunAsync("EUR/DKK 1\nEUR/DKK 2\n");

        Assert.Contains("Cancelled.", _output.ToString());
        await _converter.Received(1).ConvertAsync(Arg.Any<CurrencyPair>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    }
}
