using System.Globalization;
using System.Xml.Linq;
using CurrencyExchange.Domain;

namespace CurrencyExchange.Infrastructure.Nationalbanken;

/// <summary>
/// Parses the Danmarks Nationalbank daily rates feed (lang=en):
/// <code>
/// &lt;exchangerates refcur="DKK"&gt;
///   &lt;dailyrates id="2026-11-05"&gt;
///     &lt;currency code="EUR" desc="Euro" rate="747.35" /&gt;
/// </code>
/// Rates are DKK per 100 units, even though the feed says refamt="1".
/// </summary>
public static class NationalbankenRatesParser
{
    public static ExchangeRateTable Parse(XDocument document, string source)
    {
        var dailyRates = document.Root?.Element("dailyrates")
            ?? throw new InvalidDataException("Missing <dailyrates> element.");

        if (!DateOnly.TryParseExact((string?)dailyRates.Attribute("id"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var asOf))
        {
            throw new InvalidDataException("Missing or invalid rate date.");
        }

        var rates = dailyRates.Elements("currency").Select(ParseRate).ToList();
        if (rates.Count == 0)
        {
            throw new InvalidDataException("The feed contains no rates.");
        }

        return new ExchangeRateTable(rates, asOf, source);
    }

    private static ExchangeRate ParseRate(XElement element)
    {
        var code = (string?)element.Attribute("code");
        var rateText = (string?)element.Attribute("rate");

        var currency = Currency.Create(code);
        if (!currency.IsSuccess)
        {
            throw new InvalidDataException(currency.Error);
        }

        if (!decimal.TryParse(rateText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var rate) || rate <= 0)
        {
            throw new InvalidDataException($"Invalid rate '{rateText}' for {code}.");
        }

        return new ExchangeRate(currency.Value, rate);
    }
}
