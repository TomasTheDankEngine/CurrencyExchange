using System.Xml.Linq;
using CurrencyExchange.Domain;
using CurrencyExchange.Infrastructure.Nationalbanken;

namespace CurrencyExchange.Tests.Infrastructure;

public class NationalbankenRatesParserTests
{
    public static XDocument LoadFixture() =>
        XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Infrastructure", "Fixtures", "nationalbanken-2026-11-05.xml"));

    private static ExchangeRateTable ParseXml(string xml) => NationalbankenRatesParser.Parse(XDocument.Parse(xml), "Test");

    [Fact]
    public void Parse_RealFeed_ReadsDateAndRates()
    {
        var table = NationalbankenRatesParser.Parse(LoadFixture(), "Test");

        Assert.Equal(new DateOnly(2026, 11, 5), table.AsOf);
        Assert.Equal("Test", table.Source);
        Assert.Equal(747.47m, table.GetRate(Currency.Create("EUR").Value).Value.DkkPer100);
        Assert.Equal(4.2028m, table.GetRate(Currency.Create("JPY").Value).Value.DkkPer100);
    }

    [Fact]
    public void Parse_RealFeed_ContainsAllTaskCurrencies()
    {
        var table = NationalbankenRatesParser.Parse(LoadFixture(), "Test");

        foreach (var code in new[] { "EUR", "USD", "GBP", "SEK", "NOK", "CHF", "JPY", "DKK" })
        {
            Assert.True(table.GetRate(Currency.Create(code).Value).IsSuccess, $"{code} is missing");
        }
    }

    [Theory]
    [InlineData("<exchangerates />")]
    [InlineData("<exchangerates><dailyrates id=\"03-11-2026\"><currency code=\"EUR\" rate=\"747.35\" /></dailyrates></exchangerates>")]
    [InlineData("<exchangerates><dailyrates id=\"2026-11-05\" /></exchangerates>")]
    [InlineData("<exchangerates><dailyrates id=\"2026-11-05\"><currency code=\"EURO\" rate=\"747.35\" /></dailyrates></exchangerates>")]
    [InlineData("<exchangerates><dailyrates id=\"2026-11-05\"><currency code=\"EUR\" rate=\"747,35\" /></dailyrates></exchangerates>")]
    public void Parse_InvalidFeed_ThrowsInvalidDataException(string xml)
    {
        Assert.Throws<InvalidDataException>(() => ParseXml(xml));
    }
}
