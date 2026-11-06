namespace CurrencyExchange.Domain;

public sealed record CurrencyPair(Currency Main, Currency Money)
{
    public bool IsSameCurrency => Main == Money;

    public static Result<CurrencyPair> Parse(string? input)
    {
        var parts = (input ?? string.Empty).Split('/');
        if (parts.Length != 2)
        {
            return Result<CurrencyPair>.Failure($"'{input}' is not a valid currency pair. Expected format: EUR/DKK.");
        }

        var main = Currency.Create(parts[0]);
        if (!main.IsSuccess)
        {
            return Result<CurrencyPair>.Failure(main.Error);
        }

        var money = Currency.Create(parts[1]);
        if (!money.IsSuccess)
        {
            return Result<CurrencyPair>.Failure(money.Error);
        }

        return Result<CurrencyPair>.Success(new CurrencyPair(main.Value, money.Value));
    }

    public override string ToString() => $"{Main}/{Money}";
}
