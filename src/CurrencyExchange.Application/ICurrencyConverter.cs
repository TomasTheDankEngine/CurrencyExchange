using CurrencyExchange.Domain;

namespace CurrencyExchange.Application;

public interface ICurrencyConverter
{
    Task<Result<ConversionResult>> ConvertAsync(CurrencyPair pair, decimal amount, CancellationToken cancellationToken = default);
}
