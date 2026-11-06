namespace CurrencyExchange.Domain;

/// <summary>
/// A consistent set of rates from one source at one point in time.
/// DKK is the base currency and is always present with a rate of 100.
/// </summary>
public sealed class ExchangeRateTable
{
    private readonly Dictionary<Currency, ExchangeRate> _rates;

    public ExchangeRateTable(IEnumerable<ExchangeRate> rates, DateOnly asOf, string source)
    {
        _rates = rates.ToDictionary(r => r.Currency);
        _rates[Currency.Dkk] = new ExchangeRate(Currency.Dkk, 100m);

        AsOf = asOf;
        Source = source;
    }

    public DateOnly AsOf { get; }

    public string Source { get; }

    public IEnumerable<ExchangeRate> Rates => _rates.Values.OrderBy(r => r.Currency.Code);

    public IEnumerable<Currency> Currencies => Rates.Select(r => r.Currency);

    public ExchangeRateTable WithSource(string source) => new(Rates, AsOf, source);

    public Result<ExchangeRate> GetRate(Currency currency)
    {
        if (_rates.TryGetValue(currency, out var rate))
        {
            return Result<ExchangeRate>.Success(rate);
        }

        var supported = string.Join(", ", Currencies);
        return Result<ExchangeRate>.Failure($"Unknown currency '{currency}'. Supported currencies: {supported}.");
    }
}
