using System.Text.Json;
using CurrencyExchange.Domain;

namespace CurrencyExchange.Infrastructure;

/// <summary>
/// JSON format shared by the embedded rates and the cache file:
/// <code>{ "asOf": "2026-11-05", "source": "Nationalbanken", "rates": { "EUR": 743.94, ... } }</code>
/// </summary>
internal static class RatesJson
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <exception cref="JsonException">The content is not valid JSON.</exception>
    /// <exception cref="InvalidDataException">The JSON does not contain valid rates.</exception>
    public static async Task<ExchangeRateTable> ReadAsync(Stream stream, string defaultSource, CancellationToken cancellationToken)
    {
        var document = await JsonSerializer.DeserializeAsync<RatesDocument>(stream, JsonOptions, cancellationToken);
        if (document?.Rates is null)
        {
            throw new InvalidDataException("The rates file contains no rates.");
        }

        var rates = document.Rates.Select(r => ToExchangeRate(r.Key, r.Value)).ToList();

        return new ExchangeRateTable(rates, document.AsOf, document.Source ?? defaultSource);
    }

    public static Task WriteAsync(Stream stream, ExchangeRateTable table, CancellationToken cancellationToken)
    {
        var document = new RatesDocument(
            table.AsOf,
            table.Source,
            table.Rates.ToDictionary(r => r.Currency.Code, r => r.DkkPer100));

        return JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
    }

    private static ExchangeRate ToExchangeRate(string code, decimal rate)
    {
        var currency = Currency.Create(code);
        if (!currency.IsSuccess)
        {
            throw new InvalidDataException(currency.Error);
        }

        if (rate <= 0)
        {
            throw new InvalidDataException($"Invalid rate {rate} for {code}.");
        }

        return new ExchangeRate(currency.Value, rate);
    }

    private sealed record RatesDocument(DateOnly AsOf, string? Source, Dictionary<string, decimal>? Rates);
}
