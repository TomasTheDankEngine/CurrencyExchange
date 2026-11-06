using CurrencyExchange.Domain;

namespace CurrencyExchange.Application;

public interface IExchangeRateProvider
{
    Task<ExchangeRateTable> GetRatesAsync(CancellationToken cancellationToken = default);
}
