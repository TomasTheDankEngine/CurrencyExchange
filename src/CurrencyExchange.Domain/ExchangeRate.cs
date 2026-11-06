namespace CurrencyExchange.Domain;

/// <summary>
/// Amount of DKK needed to buy 100 units of the currency, as quoted by Danmarks Nationalbank.
/// </summary>
public sealed record ExchangeRate
{
    public ExchangeRate(Currency currency, decimal dkkPer100)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dkkPer100);

        Currency = currency;
        DkkPer100 = dkkPer100;
    }

    public Currency Currency { get; }

    public decimal DkkPer100 { get; }
}
