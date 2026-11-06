using CurrencyExchange.Domain;

namespace CurrencyExchange.Tests.Domain;

public class ExchangeRateTableTests
{
    private static readonly DateOnly AsOf = new(2026, 11, 5);

    private static Currency C(string code) => Currency.Create(code).Value;

    private static ExchangeRateTable CreateTable() => new(
        [new ExchangeRate(C("EUR"), 743.94m), new ExchangeRate(C("JPY"), 5.9740m)],
        AsOf,
        "Test");

    [Fact]
    public void GetRate_KnownCurrency_ReturnsRate()
    {
        var result = CreateTable().GetRate(C("EUR"));

        Assert.True(result.IsSuccess);
        Assert.Equal(743.94m, result.Value.DkkPer100);
    }

    [Fact]
    public void GetRate_Dkk_IsAlwaysAvailableAt100()
    {
        var result = CreateTable().GetRate(Currency.Dkk);

        Assert.True(result.IsSuccess);
        Assert.Equal(100m, result.Value.DkkPer100);
    }

    [Fact]
    public void GetRate_UnknownCurrency_ReturnsFailureListingSupportedCurrencies()
    {
        var result = CreateTable().GetRate(C("XYZ"));

        Assert.False(result.IsSuccess);
        Assert.Equal("Unknown currency 'XYZ'. Supported currencies: DKK, EUR, JPY.", result.Error);
    }

    [Fact]
    public void Constructor_KeepsAsOfAndSource()
    {
        var table = CreateTable();

        Assert.Equal(AsOf, table.AsOf);
        Assert.Equal("Test", table.Source);
    }

    [Fact]
    public void Constructor_DuplicateCurrency_Throws()
    {
        ExchangeRate[] rates = [new(C("EUR"), 743.94m), new(C("EUR"), 744m)];

        Assert.Throws<ArgumentException>(() => new ExchangeRateTable(rates, AsOf, "Test"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ExchangeRate_NonPositiveRate_Throws(decimal rate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExchangeRate(C("EUR"), rate));
    }
}
