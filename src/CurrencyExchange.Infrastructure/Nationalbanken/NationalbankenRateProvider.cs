using System.Xml.Linq;
using CurrencyExchange.Application;
using CurrencyExchange.Domain;

namespace CurrencyExchange.Infrastructure.Nationalbanken;

public sealed class NationalbankenRateProvider(HttpClient httpClient, NationalbankenOptions options) : IExchangeRateProvider
{
    public const string SourceName = "Nationalbanken";

    public async Task<ExchangeRateTable> GetRatesAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(options.Url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);

        return NationalbankenRatesParser.Parse(document, SourceName);
    }
}
