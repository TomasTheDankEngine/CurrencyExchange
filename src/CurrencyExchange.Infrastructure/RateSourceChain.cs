using CurrencyExchange.Application;
using CurrencyExchange.Domain;
using CurrencyExchange.Infrastructure.Caching;
using Microsoft.Extensions.Logging;

namespace CurrencyExchange.Infrastructure;

/// <summary>
/// Decides where the rates come from, in this order:
/// 1. the live source (the result is saved to the cache)
/// 2. the cache,
/// 3. the fallback source (sample rates embedded in the application)
/// A failing live source (network error, timeout, bad data) moves on to the next step.
/// Cancellation by the user (Ctrl+C) stops immediately.
/// The rates are loaded once and reused for the rest of the session, so every conversion
/// uses the same rates and only the first one waits for the network.
/// </summary>
public sealed class RateSourceChain(
    IRateCache cache,
    IExchangeRateProvider liveProvider,
    IExchangeRateProvider fallbackProvider,
    ILogger<RateSourceChain> logger) : IExchangeRateProvider
{
    private ExchangeRateTable? _rates;

    public async Task<ExchangeRateTable> GetRatesAsync(CancellationToken cancellationToken = default) =>
        _rates ??= await LoadAsync(cancellationToken);

    private async Task<ExchangeRateTable> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var live = await liveProvider.GetRatesAsync(cancellationToken);
            await TrySaveAsync(live, cancellationToken);
            return live;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Live exchange rates are unavailable: {Reason}", ex.Message);
        }

        var cached = await cache.LoadAsync(cancellationToken);
        if (cached is not null)
        {
            return cached.WithSource($"{cached.Source}, cached");
        }

        return await fallbackProvider.GetRatesAsync(cancellationToken);
    }

    private async Task TrySaveAsync(ExchangeRateTable rates, CancellationToken cancellationToken)
    {
        try
        {
            await cache.SaveAsync(rates, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning("Could not write the rate cache: {Reason}", ex.Message);
        }
    }
}
