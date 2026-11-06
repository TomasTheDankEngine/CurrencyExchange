using System.Globalization;
using CurrencyExchange.Application;
using CurrencyExchange.Cli;
using CurrencyExchange.Domain;

namespace CurrencyExchange.Tests.Cli;

public class ResultFormatterTests
{
    private static ConversionResult Result(string pair, decimal amount, decimal converted) => new(
        CurrencyPair.Parse(pair).Value,
        amount,
        converted,
        new DateOnly(2026, 11, 5),
        "Test");

    [Theory]
    [InlineData("59.74", "59.7400")]
    [InlineData("0.00803024", "0.0080")]
    [InlineData("1.00005", "1.0001")]
    [InlineData("1.00004999", "1.0000")]
    public void Format_RoundsToFourDecimalsAwayFromZero(string converted, string expected)
    {
        var text = ResultFormatter.Format(Result("EUR/DKK", 1m, decimal.Parse(converted, CultureInfo.InvariantCulture)));

        Assert.StartsWith($"1 EUR = {expected} DKK", text);
    }

    [Fact]
    public void Format_IncludesRateDateAndSource()
    {
        var text = ResultFormatter.Format(Result("EUR/DKK", 1m, 7.4394m));

        Assert.EndsWith("Rates as of 2026-11-05 (source: Test)", text);
    }

    [Fact]
    public void Format_DoesNotDependOnCurrentCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("da-DK");

            var text = ResultFormatter.Format(Result("GBP/SEK", 2.5m, 28.0174m));

            Assert.StartsWith("2.5 GBP = 28.0174 SEK", text);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
