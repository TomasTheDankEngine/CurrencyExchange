using CurrencyExchange.Application;
using CurrencyExchange.Domain;
using CurrencyExchange.Infrastructure;
using CurrencyExchange.Infrastructure.Caching;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace CurrencyExchange.Tests.Infrastructure;

public class RateSourceChainTests
{
    private readonly IRateCache _cache = Substitute.For<IRateCache>();
    private readonly IExchangeRateProvider _live = Substitute.For<IExchangeRateProvider>();
    private readonly IExchangeRateProvider _fallback = Substitute.For<IExchangeRateProvider>();
    private readonly RateSourceChain _chain;

    private readonly ExchangeRateTable _liveTable = CreateTable("Nationalbanken", 747.35m);
    private readonly ExchangeRateTable _cachedTable = CreateTable("Nationalbanken", 746m);
    private readonly ExchangeRateTable _fallbackTable = CreateTable("Embedded", 743.94m);

    public RateSourceChainTests()
    {
        _live.GetRatesAsync(Arg.Any<CancellationToken>()).Returns(_liveTable);
        _fallback.GetRatesAsync(Arg.Any<CancellationToken>()).Returns(_fallbackTable);
        _cache.LoadAsync(Arg.Any<CancellationToken>()).Returns((ExchangeRateTable?)null);

        _chain = new RateSourceChain(_cache, _live, _fallback, NullLogger<RateSourceChain>.Instance);
    }

    private static ExchangeRateTable CreateTable(string source, decimal eurRate) =>
        new([new ExchangeRate(Currency.Create("EUR").Value, eurRate)], new DateOnly(2026, 11, 5), source);

    private void GivenLiveFails(Exception exception) =>
        _live.GetRatesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(exception);

    [Fact]
    public async Task GetRatesAsync_LiveAvailable_ReturnsLiveRatesAndCachesThem()
    {
        var table = await _chain.GetRatesAsync(TestContext.Current.CancellationToken);

        Assert.Same(_liveTable, table);
        await _cache.Received(1).SaveAsync(_liveTable, Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().LoadAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRatesAsync_CalledTwice_LoadsOnlyOnce()
    {
        var first = await _chain.GetRatesAsync(TestContext.Current.CancellationToken);
        var second = await _chain.GetRatesAsync(TestContext.Current.CancellationToken);

        Assert.Same(first, second);
        await _live.Received(1).GetRatesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRatesAsync_FirstLoadCancelled_TriesAgainNextTime()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _live.GetRatesAsync(cts.Token).Returns(Task.FromCanceled<ExchangeRateTable>(cts.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _chain.GetRatesAsync(cts.Token));
        var table = await _chain.GetRatesAsync(TestContext.Current.CancellationToken);

        Assert.Same(_liveTable, table);
    }

    [Fact]
    public async Task GetRatesAsync_LiveFailsWithCache_ReturnsCache()
    {
        GivenLiveFails(new HttpRequestException("No such host is known."));
        _cache.LoadAsync(Arg.Any<CancellationToken>()).Returns(_cachedTable);

        var table = await _chain.GetRatesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(746m, table.GetRate(Currency.Create("EUR").Value).Value.DkkPer100);
        Assert.Equal("Nationalbanken, cached", table.Source);
        await _fallback.DidNotReceive().GetRatesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRatesAsync_LiveFailsWithoutCache_ReturnsFallback()
    {
        GivenLiveFails(new InvalidDataException("Bad feed"));

        var table = await _chain.GetRatesAsync(TestContext.Current.CancellationToken);

        Assert.Same(_fallbackTable, table);
    }

    [Fact]
    public async Task GetRatesAsync_LiveTimesOut_FallsBack()
    {
        GivenLiveFails(new TaskCanceledException("Timeout", new TimeoutException()));

        var table = await _chain.GetRatesAsync(TestContext.Current.CancellationToken);

        Assert.Same(_fallbackTable, table);
    }

    [Fact]
    public async Task GetRatesAsync_UserCancels_StopsWithoutFallingBack()
    {
        using var cts = new CancellationTokenSource();
        _live.GetRatesAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            cts.Cancel();
            return Task.FromCanceled<ExchangeRateTable>(cts.Token);
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _chain.GetRatesAsync(cts.Token));

        await _cache.DidNotReceive().LoadAsync(Arg.Any<CancellationToken>());
        await _fallback.DidNotReceive().GetRatesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRatesAsync_CacheCannotBeWritten_StillReturnsLiveRates()
    {
        _cache.SaveAsync(Arg.Any<ExchangeRateTable>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new UnauthorizedAccessException("Read-only folder"));

        var table = await _chain.GetRatesAsync(TestContext.Current.CancellationToken);

        Assert.Same(_liveTable, table);
    }
}
