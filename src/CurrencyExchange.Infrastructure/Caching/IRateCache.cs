using CurrencyExchange.Domain;

namespace CurrencyExchange.Infrastructure.Caching;

public interface IRateCache
{
    /// <summary>Returns the last saved rates, or null if there is no usable cache.</summary>
    Task<ExchangeRateTable?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(ExchangeRateTable table, CancellationToken cancellationToken = default);
}
