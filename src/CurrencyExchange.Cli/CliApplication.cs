using CurrencyExchange.Application;

namespace CurrencyExchange.Cli;

/// <summary>
/// Reads lines like "EUR/DKK 1" and writes the converted amount, until "exit" or end of input.
/// </summary>
public sealed class CliApplication(ICurrencyConverter converter, TextReader input, TextWriter output)
{
    public const string Welcome = "Enter a currency pair and an amount, e.g. EUR/DKK 1. Type 'exit' to quit.";
    public const string Prompt = "> ";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await output.WriteLineAsync(Welcome);

        try
        {
            while (true)
            {
                await output.WriteAsync(Prompt);
                var line = (await input.ReadLineAsync(cancellationToken))?.Trim();

                // null means end of input, e.g. Ctrl+Z or Ctrl+C at the prompt.
                if (line is null || line.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (line.Length > 0)
                {
                    await output.WriteLineAsync(await ConvertAsync(line, cancellationToken));
                }
            }
        }
        catch (OperationCanceledException)
        {
            await output.WriteLineAsync("Cancelled.");
        }
    }

    private async Task<string> ConvertAsync(string line, CancellationToken cancellationToken)
    {
        var request = InputParser.Parse(line);
        if (!request.IsSuccess)
        {
            return request.Error;
        }

        try
        {
            var result = await converter.ConvertAsync(request.Value.Pair, request.Value.Amount, cancellationToken);
            return result.IsSuccess ? ResultFormatter.Format(result.Value) : result.Error;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return $"Unexpected error: {ex.Message}";
        }
    }
}
