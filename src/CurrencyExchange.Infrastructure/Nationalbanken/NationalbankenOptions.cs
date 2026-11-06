namespace CurrencyExchange.Infrastructure.Nationalbanken;

public sealed class NationalbankenOptions
{
    public const string SectionName = "Nationalbanken";

    public Uri Url { get; set; } = new("https://www.nationalbanken.dk/api/currencyratesxml?lang=en");

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}
