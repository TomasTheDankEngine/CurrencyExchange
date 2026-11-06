using CurrencyExchange.Application;
using CurrencyExchange.Cli;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Logging.AddSimpleConsole(options => options.SingleLine = true);

builder.Services.AddCurrencyExchange(builder.Configuration, AppContext.BaseDirectory);
builder.Services.AddTransient(sp => new CliApplication(
    sp.GetRequiredService<ICurrencyConverter>(),
    Console.In,
    Console.Out));

using var host = builder.Build();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await host.Services.GetRequiredService<CliApplication>().RunAsync(cts.Token);
