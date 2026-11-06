using CurrencyExchange.Domain;

namespace CurrencyExchange.Tests.Domain;

public class CurrencyTests
{
    [Theory]
    [InlineData("EUR", "EUR")]
    [InlineData("eUr", "EUR")]
    [InlineData(" usd ", "USD")]
    public void Create_ValidCode_NormalizesToUpperCase(string input, string expected)
    {
        var result = Currency.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    public void Create_InvalidCode_ReturnsFailure(string? input)
    {
        var result = Currency.Create(input);

        Assert.False(result.IsSuccess);
        Assert.Contains("not a valid ISO currency code", result.Error);
    }

    [Fact]
    public void Currencies_WithSameCode_AreEqual()
    {
        Assert.Equal(Currency.Create("dkk").Value, Currency.Dkk);
    }
}
