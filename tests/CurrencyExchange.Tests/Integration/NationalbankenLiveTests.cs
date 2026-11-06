using CurrencyExchange.Domain;
using CurrencyExchange.Infrastructure.Nationalbanken;

namespace CurrencyExchange.Tests.Integration;

/// <summary>
/// Calls the real Danmarks Nationalbank feed, so it needs network access.
/// </summary>
[Trait("Category", "Integration")]
public class NationalbankenLiveTests
{
    [Fact]
    public async Task GetRatesAsync_RealFeed_ReturnsRecentRatesForAllTaskCurrencies()
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var provider = new NationalbankenRateProvider(httpClient, new NationalbankenOptions());

        var table = await provider.GetRatesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(NationalbankenRateProvider.SourceName, table.Source);

        // Rates are published on banking days, so allow for weekends and holidays.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Assert.InRange(table.AsOf, today.AddDays(-7), today);

        foreach (var code in new[] { "EUR", "USD", "GBP", "SEK", "NOK", "CHF", "JPY" })
        {
            var rate = table.GetRate(Currency.Create(code).Value);
            Assert.True(rate.IsSuccess, $"{code} is missing from the live feed");
        }

        var eur = table.GetRate(Currency.Create("EUR").Value).Value.DkkPer100;
        Assert.InRange(eur, 730m, 760m);
    }
}
