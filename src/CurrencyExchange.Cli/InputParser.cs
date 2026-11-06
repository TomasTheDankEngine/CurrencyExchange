using CurrencyExchange.Domain;

namespace CurrencyExchange.Cli;

public sealed record ConversionRequest(CurrencyPair Pair, decimal Amount);

/// <summary>
/// Parses one line of user input, e.g. "EUR/DKK 1".
/// </summary>
public static class InputParser
{
    public const string ExpectedFormat = "Expected a currency pair and an amount, e.g. EUR/DKK 1";

    public static Result<ConversionRequest> Parse(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return Result<ConversionRequest>.Failure(ExpectedFormat);
        }

        var pair = CurrencyPair.Parse(parts[0]);
        if (!pair.IsSuccess)
        {
            return Result<ConversionRequest>.Failure(pair.Error);
        }

        var amount = AmountParser.Parse(parts[1]);
        if (!amount.IsSuccess)
        {
            return Result<ConversionRequest>.Failure(amount.Error);
        }

        return Result<ConversionRequest>.Success(new ConversionRequest(pair.Value, amount.Value));
    }
}
