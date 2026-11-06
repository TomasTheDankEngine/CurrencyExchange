using System.Text.Json;
using CurrencyExchange.Domain;

namespace CurrencyExchange.Infrastructure.Caching;

/// <summary>
/// Keeps the last successfully fetched rates in a local JSON file, as a fallback for when the live source
/// is unavailable. The cache is best effort: a missing or unreadable file is treated as "no cache".
/// </summary>
public sealed class JsonFileRateCache(string filePath) : IRateCache
{
    public string FilePath { get; } = filePath;

    public async Task<ExchangeRateTable?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(FilePath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(FilePath);
            return await RatesJson.ReadAsync(stream, "Cache", cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or IOException)
        {
            return null;
        }
    }

    public async Task SaveAsync(ExchangeRateTable table, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        // Write to a temporary file first, so a crash halfway never leaves a half-written cache behind.
        var tempPath = FilePath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await RatesJson.WriteAsync(stream, table, cancellationToken);
        }

        File.Move(tempPath, FilePath, overwrite: true);
    }
}
