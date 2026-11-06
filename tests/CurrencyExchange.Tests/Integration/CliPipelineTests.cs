using System.Net;
using CurrencyExchange.Application;
using CurrencyExchange.Cli;
using CurrencyExchange.Infrastructure.Embedded;
using CurrencyExchange.Infrastructure.Nationalbanken;
using CurrencyExchange.Tests.Infrastructure;
using CurrencyExchange.Tests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyExchange.Tests.Integration;

/// <summary>
/// Runs the whole application in-process with the real service registrations. Only the edges are replaced:
/// console input/output, HTTP (stub handler returning the saved Nationalbanken feed) and the cache folder.
/// Each RunAsync call is a separate start of the app; the cache folder is shared between them.
/// </summary>
public sealed class CliPipelineTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly StubHttpMessageHandler _http;
    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Nationalbanken:Url"] = "https://nationalbanken.test/rates",
        })
        .Build();

    private readonly string _feedXml = NationalbankenRatesParserTests.LoadFixture().ToString();

    private Func<HttpResponseMessage> _respond;

    public CliPipelineTests()
    {
        _respond = () => StubHttpMessageHandler.Response(HttpStatusCode.OK, _feedXml);
        _http = new StubHttpMessageHandler((_, _) => Task.FromResult(_respond()));
    }

    public void Dispose() => _temp.Dispose();

    /// <summary>Starts the app, types the given lines and returns everything it wrote.</summary>
    private async Task<string> RunAsync(params string[] lines)
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddCurrencyExchange(_configuration, _temp.Path)
            .AddHttpClient<NationalbankenRateProvider>().ConfigurePrimaryHttpMessageHandler(() => _http).Services
            .BuildServiceProvider();

        var output = new StringWriter();
        var app = new CliApplication(
            services.GetRequiredService<ICurrencyConverter>(),
            new StringReader(string.Join('\n', lines)),
            output);

        await app.RunAsync(TestContext.Current.CancellationToken);

        return output.ToString();
    }

    [Fact]
    public async Task Run_SeveralLines_FetchesRatesOnlyOnce()
    {
        await RunAsync("EUR/DKK 1", "EUR/USD 1", "GBP/SEK 1");

        Assert.Equal(1, _http.RequestCount);
    }

    [Fact]
    public async Task Run_SeveralLines_ConvertsEachWithLiveRates()
    {
        var output = await RunAsync("eur/dkk 1", "EUR/XYZ 1", "gbp/sek 2,5", "exit");

        Assert.Contains("1 EUR = 7.4747 DKK", output);
        Assert.Contains("Rates as of 2026-11-05 (source: Nationalbanken)", output);
        Assert.Contains("Unknown currency 'XYZ'", output);
        Assert.Contains("2.5 GBP = ", output);
    }

    [Fact]
    public async Task Run_FeedDownAfterEarlierSuccess_UsesCachedRates()
    {
        await RunAsync("EUR/DKK 1");
        _respond = () => StubHttpMessageHandler.Response(HttpStatusCode.ServiceUnavailable);

        var output = await RunAsync("EUR/DKK 1");

        Assert.Contains("1 EUR = 7.4747 DKK", output);
        Assert.Contains("(source: Nationalbanken, cached)", output);
    }

    [Fact]
    public async Task Run_FeedDownWithoutCache_UsesEmbeddedRates()
    {
        _respond = () => throw new HttpRequestException("No such host is known.");

        var output = await RunAsync("EUR/DKK 1");

        Assert.Contains("1 EUR = 7.4394 DKK", output);
        Assert.Contains($"(source: {EmbeddedRateProvider.SourceName})", output);
    }

    [Fact]
    public async Task Run_SameCurrency_ReturnsAmount()
    {
        var output = await RunAsync("JPY/jpy 1234,5");

        Assert.Contains("1234.5 JPY = 1234.5000 JPY", output);
    }

    [Fact]
    public async Task Run_InvalidInput_ShowsExpectedFormatWithoutCallingFeed()
    {
        var output = await RunAsync("EUR/DKK");

        Assert.Contains(InputParser.ExpectedFormat, output);
        Assert.Equal(0, _http.RequestCount);
    }
}
