using System.Globalization;
using CurrencyExchange.Application;

namespace CurrencyExchange.Cli;

public static class ResultFormatter
{
    public const int DisplayDecimals = 4;

    public static string Format(ConversionResult result)
    {
        var converted = Math.Round(result.ConvertedAmount, DisplayDecimals, MidpointRounding.AwayFromZero);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{result.Amount} {result.Pair.Main} = {converted:F4} {result.Pair.Money}{Environment.NewLine}" +
            $"Rates as of {result.RatesAsOf:yyyy-MM-dd} (source: {result.RatesSource})");
    }
}
