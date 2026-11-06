using CurrencyExchange.Cli;

namespace CurrencyExchange.Tests.Cli;

public class InputParserTests
{
    [Theory]
    [InlineData("eur/dkk 1,5")]
    [InlineData("  EUR/DKK   1.5  ")]
    public void Parse_PairAndAmount_ReturnsRequest(string line)
    {
        var result = InputParser.Parse(line);

        Assert.True(result.IsSuccess);
        Assert.Equal("EUR/DKK", result.Value.Pair.ToString());
        Assert.Equal(1.5m, result.Value.Amount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("EUR/DKK")]
    [InlineData("EUR/DKK 1 extra")]
    public void Parse_WrongNumberOfParts_ReturnsExpectedFormat(string line)
    {
        var result = InputParser.Parse(line);

        Assert.False(result.IsSuccess);
        Assert.Equal(InputParser.ExpectedFormat, result.Error);
    }

    [Fact]
    public void Parse_InvalidPair_ReturnsPairError()
    {
        var result = InputParser.Parse("EURDKK 1");

        Assert.False(result.IsSuccess);
        Assert.Contains("not a valid currency pair", result.Error);
    }

    [Fact]
    public void Parse_InvalidAmount_ReturnsAmountError()
    {
        var result = InputParser.Parse("EUR/DKK abc");

        Assert.False(result.IsSuccess);
        Assert.Contains("not a valid amount", result.Error);
    }
}
