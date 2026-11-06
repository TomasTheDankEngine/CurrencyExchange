namespace CurrencyExchange.Domain;

public sealed record Currency
{
    public static readonly Currency Dkk = new("DKK");

    private Currency(string code) => Code = code;

    public string Code { get; }

    public static Result<Currency> Create(string? code)
    {
        var normalized = code?.Trim().ToUpperInvariant() ?? string.Empty;

        if (normalized.Length != 3 || !normalized.All(char.IsAsciiLetterUpper))
        {
            return Result<Currency>.Failure($"'{code}' is not a valid ISO currency code.");
        }

        return Result<Currency>.Success(new Currency(normalized));
    }

    public override string ToString() => Code;
}
