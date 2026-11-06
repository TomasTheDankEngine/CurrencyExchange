using CurrencyExchange.Application;
using CurrencyExchange.Infrastructure;
using CurrencyExchange.Infrastructure.Caching;
using CurrencyExchange.Infrastructure.Embedded;
using CurrencyExchange.Infrastructure.Nationalbanken;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CurrencyExchange.Cli;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers everything needed to convert currencies.
    /// </summary>
    /// <param name="cacheDirectory">Where the rate cache file is stored (the application folder when running).</param>
    public static IServiceCollection AddCurrencyExchange(this IServiceCollection services, IConfiguration configuration, string cacheDirectory)
    {
        var nationalbankenOptions = configuration.GetSection(NationalbankenOptions.SectionName).Get<NationalbankenOptions>()
            ?? new NationalbankenOptions();

        services.AddSingleton(nationalbankenOptions);
        services.AddHttpClient<NationalbankenRateProvider>(client => client.Timeout = nationalbankenOptions.Timeout);
        services.AddSingleton<EmbeddedRateProvider>();
        services.AddSingleton<IRateCache>(new JsonFileRateCache(Path.Combine(cacheDirectory, "rates-cache.json")));

        services.AddSingleton<IExchangeRateProvider>(sp => new RateSourceChain(
            sp.GetRequiredService<IRateCache>(),
            sp.GetRequiredService<NationalbankenRateProvider>(),
            sp.GetRequiredService<EmbeddedRateProvider>(),
            sp.GetRequiredService<ILogger<RateSourceChain>>()));

        services.AddTransient<ICurrencyConverter, CurrencyConverter>();

        return services;
    }
}
