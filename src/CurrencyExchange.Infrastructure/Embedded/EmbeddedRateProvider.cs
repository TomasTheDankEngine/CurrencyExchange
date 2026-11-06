using CurrencyExchange.Application;
using CurrencyExchange.Domain;

namespace CurrencyExchange.Infrastructure.Embedded;

/// <summary>
/// The sample rates from the task description, shipped inside the assembly.
/// Used as the last fallback when no other source is available.
/// </summary>
public sealed class EmbeddedRateProvider : IExchangeRateProvider
{
    public const string SourceName = "Embedded sample rates";

    private const string ResourceName = "CurrencyExchange.Infrastructure.rates.json";

    public async Task<ExchangeRateTable> GetRatesAsync(CancellationToken cancellationToken = default)
    {
        await using var stream = typeof(EmbeddedRateProvider).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found.");

        return await RatesJson.ReadAsync(stream, SourceName, cancellationToken);
    }
}
