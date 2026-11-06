using CurrencyExchange.Domain;
using CurrencyExchange.Infrastructure.Caching;
using CurrencyExchange.Tests.TestHelpers;

namespace CurrencyExchange.Tests.Infrastructure;

public sealed class JsonFileRateCacheTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly JsonFileRateCache _cache;

    public JsonFileRateCacheTests()
    {
        _cache = new JsonFileRateCache(_temp.Combine("cache", "rates.json"));
    }

    public void Dispose() => _temp.Dispose();

    private static ExchangeRateTable CreateTable(decimal eurRate = 747.35m) => new(
        [
            new ExchangeRate(Currency.Create("EUR").Value, eurRate),
            new ExchangeRate(Currency.Create("JPY").Value, 4.2235m),
        ],
        new DateOnly(2026, 11, 5),
        "Nationalbanken");

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_ReturnsSameRates()
    {
        var ct = TestContext.Current.CancellationToken;

        await _cache.SaveAsync(CreateTable(), ct);
        var cached = await _cache.LoadAsync(ct);

        Assert.NotNull(cached);
        Assert.Equal(new DateOnly(2026, 11, 5), cached.AsOf);
        Assert.Equal("Nationalbanken", cached.Source);
        Assert.Equal(747.35m, cached.GetRate(Currency.Create("EUR").Value).Value.DkkPer100);
        Assert.Equal(4.2235m, cached.GetRate(Currency.Create("JPY").Value).Value.DkkPer100);
        Assert.False(File.Exists(_cache.FilePath + ".tmp"));
    }

    [Fact]
    public async Task SaveAsync_ExistingFile_IsOverwritten()
    {
        var ct = TestContext.Current.CancellationToken;
        await _cache.SaveAsync(CreateTable(747.35m), ct);

        await _cache.SaveAsync(CreateTable(750m), ct);

        var cached = await _cache.LoadAsync(ct);
        Assert.Equal(750m, cached?.GetRate(Currency.Create("EUR").Value).Value.DkkPer100);
    }

    [Fact]
    public async Task LoadAsync_NoFile_ReturnsNull()
    {
        Assert.Null(await _cache.LoadAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{ "asOf": "2026-11-05", "source": "X", "rates": { "EURO": 747.35 } }""")]
    [InlineData("""{ "asOf": "2026-11-05", "source": "X", "rates": { "EUR": 0 } }""")]
    public async Task LoadAsync_CorruptFile_ReturnsNull(string content)
    {
        var ct = TestContext.Current.CancellationToken;
        Directory.CreateDirectory(Path.GetDirectoryName(_cache.FilePath)!);
        await File.WriteAllTextAsync(_cache.FilePath, content, ct);

        Assert.Null(await _cache.LoadAsync(ct));
    }
}
