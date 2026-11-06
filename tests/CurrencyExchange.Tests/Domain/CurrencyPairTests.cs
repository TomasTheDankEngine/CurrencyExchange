using CurrencyExchange.Domain;

namespace CurrencyExchange.Tests.Domain;

public class CurrencyPairTests
{
    [Theory]
    [InlineData("EUR/DKK", "EUR", "DKK")]
    [InlineData("Eur/DkK", "EUR", "DKK")]
    [InlineData("USD / JPY", "USD", "JPY")]
    public void Parse_ValidPair_ReturnsMainAndMoney(string input, string main, string money)
    {
        var result = CurrencyPair.Parse(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(main, result.Value.Main.Code);
        Assert.Equal(money, result.Value.Money.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("EURDKK")]
    [InlineData("EUR/DKK/USD")]
    public void Parse_WrongFormat_ReturnsFailure(string? input)
    {
        var result = CurrencyPair.Parse(input);

        Assert.False(result.IsSuccess);
        Assert.Contains("Expected format", result.Error);
    }

    [Theory]
    [InlineData("EU/DKK")]
    [InlineData("EUR/D1K")]
    public void Parse_InvalidCurrencyCode_ReturnsFailure(string input)
    {
        var result = CurrencyPair.Parse(input);

        Assert.False(result.IsSuccess);
        Assert.Contains("not a valid ISO currency code", result.Error);
    }

    [Theory]
    [InlineData("eur/EUR", true)]
    [InlineData("EUR/DKK", false)]
    public void IsSameCurrency_ComparesMainAndMoney(string input, bool expected)
    {
        Assert.Equal(expected, CurrencyPair.Parse(input).Value.IsSameCurrency);
    }

    [Fact]
    public void Parse_ValidCodeButUnknownCurrency_Succeeds()
    {
        Assert.True(CurrencyPair.Parse("XYZ/DKK").IsSuccess);
    }
}
