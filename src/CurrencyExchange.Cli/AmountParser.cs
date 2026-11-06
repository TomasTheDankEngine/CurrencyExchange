using System.Globalization;
using CurrencyExchange.Domain;

namespace CurrencyExchange.Cli;

/// <summary>
/// Parses amounts independently of culture. A single '.' or ',' is treated as the decimal separator. Thousands separators not supported.
/// </summary>
public static class AmountParser
{
    public static Result<decimal> Parse(string? input)
    {
        var text = input?.Trim() ?? string.Empty;

        if (text.StartsWith('-'))
        {
            return Result<decimal>.Failure("Amount must not be negative.");
        }

        var separatorCount = text.Count(c => c is '.' or ',');
        var normalized = text.Replace(',', '.');

        if (separatorCount > 1
            || !decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
        {
            return Result<decimal>.Failure(
                $"'{input}' is not a valid amount. Use digits with an optional '.' or ',' as decimal separator, e.g. 1.5 or 1,5.");
        }

        return Result<decimal>.Success(amount);
    }
}
