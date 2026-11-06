using System.Globalization;
using CurrencyExchange.Application;
using CurrencyExchange.Domain;
using NSubstitute;

namespace CurrencyExchange.Tests.Application;

public class CurrencyConverterTests
{
    private static readonly DateOnly AsOf = new(2026, 11, 5);

    private readonly IExchangeRateProvider _rateProvider = Substitute.For<IExchangeRateProvider>();
    private readonly CurrencyConverter _converter;

    public CurrencyConverterTests()
    {
        _rateProvider.GetRatesAsync(Arg.Any<CancellationToken>()).Returns(CreateTaskTable());
        _converter = new CurrencyConverter(_rateProvider);
    }

    private static ExchangeRateTable CreateTaskTable() => new(
        [
            Rate("EUR", 743.94m),
            Rate("USD", 663.11m),
            Rate("GBP", 852.85m),
            Rate("SEK", 76.10m),
            Rate("JPY", 5.9740m),
        ],
        AsOf,
        "Test");

    private static ExchangeRate Rate(string code, decimal dkkPer100) => new(Currency.Create(code).Value, dkkPer100);

    private static CurrencyPair Pair(string pair) => CurrencyPair.Parse(pair).Value;

    [Theory]
    [InlineData("EUR/DKK", "1", "7.4394")]
    [InlineData("DKK/EUR", "100", "13.4419")]
    [InlineData("EUR/USD", "1", "1.1219")]
    [InlineData("GBP/SEK", "2.5", "28.0174")]
    [InlineData("EUR/JPY", "1", "124.5296")]
    [InlineData("JPY/EUR", "1", "0.0080")]
    public async Task ConvertAsync_KnownCurrencies_ConvertsViaDkk(string pair, string amount, string expected)
    {
        var result = await _converter.ConvertAsync(Pair(pair), decimal.Parse(amount, CultureInfo.InvariantCulture), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), Math.Round(result.Value.ConvertedAmount, 4, MidpointRounding.AwayFromZero));
    }

    [Theory]
    [InlineData("EUR/EUR")]
    [InlineData("DKK/DKK")]
    public async Task ConvertAsync_SameCurrency_ReturnsAmountUnchanged(string pair)
    {
        var result = await _converter.ConvertAsync(Pair(pair), 123.456789m, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(123.456789m, result.Value.ConvertedAmount);
    }

    [Theory]
    [InlineData("XYZ/DKK")]
    [InlineData("DKK/XYZ")]
    [InlineData("XYZ/XYZ")]
    public async Task ConvertAsync_UnknownCurrency_ReturnsFailure(string pair)
    {
        var result = await _converter.ConvertAsync(Pair(pair), 1m, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.StartsWith("Unknown currency 'XYZ'", result.Error);
    }

    [Fact]
    public async Task ConvertAsync_NegativeAmount_ReturnsFailureWithoutFetchingRates()
    {
        var result = await _converter.ConvertAsync(Pair("EUR/DKK"), -1m, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("Amount must not be negative.", result.Error);
        await _rateProvider.DidNotReceive().GetRatesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConvertAsync_AmountTooLarge_ReturnsFailure()
    {
        var result = await _converter.ConvertAsync(Pair("EUR/DKK"), decimal.MaxValue, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("The amount is too large to convert.", result.Error);
    }

    [Fact]
    public async Task ConvertAsync_ReturnsRateDateAndSource()
    {
        var result = await _converter.ConvertAsync(Pair("EUR/DKK"), 1m, TestContext.Current.CancellationToken);

        Assert.Equal(AsOf, result.Value.RatesAsOf);
        Assert.Equal("Test", result.Value.RatesSource);
    }

    [Fact]
    public async Task ConvertAsync_PassesCancellationTokenToProvider()
    {
        using var cts = new CancellationTokenSource();

        await _converter.ConvertAsync(Pair("EUR/DKK"), 1m, cts.Token);

        await _rateProvider.Received(1).GetRatesAsync(cts.Token);
    }
}
