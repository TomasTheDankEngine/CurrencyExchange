using CurrencyExchange.Domain;

namespace CurrencyExchange.Application;

public sealed class CurrencyConverter(IExchangeRateProvider rateProvider) : ICurrencyConverter
{
    public async Task<Result<ConversionResult>> ConvertAsync(CurrencyPair pair, decimal amount, CancellationToken cancellationToken = default)
    {
        if (amount < 0)
        {
            return Result<ConversionResult>.Failure("Amount must not be negative.");
        }

        var rates = await rateProvider.GetRatesAsync(cancellationToken);

        var mainRate = rates.GetRate(pair.Main);
        if (!mainRate.IsSuccess)
        {
            return Result<ConversionResult>.Failure(mainRate.Error);
        }

        var moneyRate = rates.GetRate(pair.Money);
        if (!moneyRate.IsSuccess)
        {
            return Result<ConversionResult>.Failure(moneyRate.Error);
        }

        decimal converted;
        try
        {
            converted = pair.IsSameCurrency
                ? amount
                : amount * mainRate.Value.DkkPer100 / moneyRate.Value.DkkPer100;
        }
        catch (OverflowException)
        {
            return Result<ConversionResult>.Failure("The amount is too large to convert.");
        }

        return Result<ConversionResult>.Success(new ConversionResult(pair, amount, converted, rates.AsOf, rates.Source));
    }
}
