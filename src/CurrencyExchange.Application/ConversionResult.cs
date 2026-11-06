using CurrencyExchange.Domain;

namespace CurrencyExchange.Application;

public sealed record ConversionResult(
    CurrencyPair Pair,
    decimal Amount,
    decimal ConvertedAmount,
    DateOnly RatesAsOf,
    string RatesSource);
