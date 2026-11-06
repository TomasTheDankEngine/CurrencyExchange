using CurrencyExchange.Domain;
using CurrencyExchange.Infrastructure.Embedded;

namespace CurrencyExchange.Tests.Infrastructure;

public class EmbeddedRateProviderTests
{
    private readonly EmbeddedRateProvider _provider = new();

    [Fact]
    public async Task GetRatesAsync_ContainsTheTaskCurrencies()
    {
        var table = await _provider.GetRatesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            ["CHF", "DKK", "EUR", "GBP", "JPY", "NOK", "SEK", "USD"],
            table.Currencies.Select(c => c.Code));
    }

    [Theory]
    [InlineData("EUR", 743.94)]
    [InlineData("JPY", 5.9740)]
    public async Task GetRatesAsync_ReadsRatesExactly(string code, decimal expected)
    {
        var table = await _provider.GetRatesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(expected, table.GetRate(Currency.Create(code).Value).Value.DkkPer100);
    }

    [Fact]
    public async Task GetRatesAsync_SetsSource()
    {
        var table = await _provider.GetRatesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(EmbeddedRateProvider.SourceName, table.Source);
    }
}
