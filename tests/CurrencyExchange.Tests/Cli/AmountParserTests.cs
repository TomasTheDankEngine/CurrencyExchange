using System.Globalization;
using CurrencyExchange.Cli;

namespace CurrencyExchange.Tests.Cli;

public class AmountParserTests
{
    [Theory]
    [InlineData("1", "1")]
    [InlineData("0", "0")]
    [InlineData("1.5", "1.5")]
    [InlineData(" 2,25 ", "2.25")]
    public void Parse_ValidAmount_ReturnsValue(string input, string expected)
    {
        var result = AmountParser.Parse(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("1.000,50")]
    [InlineData("1,000.50")]
    [InlineData("1e5")]
    [InlineData("+1")]
    public void Parse_InvalidAmount_ReturnsFailure(string? input)
    {
        var result = AmountParser.Parse(input);

        Assert.False(result.IsSuccess);
        Assert.Contains("is not a valid amount", result.Error);
    }

    [Fact]
    public void Parse_NegativeAmount_ReturnsFailure()
    {
        var result = AmountParser.Parse("-1");

        Assert.False(result.IsSuccess);
        Assert.Equal("Amount must not be negative.", result.Error);
    }

    [Fact]
    public void Parse_DoesNotDependOnCurrentCulture()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("da-DK");

            Assert.Equal(1.5m, AmountParser.Parse("1.5").Value);
            Assert.Equal(1.5m, AmountParser.Parse("1,5").Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
