using System.Net;
using CurrencyExchange.Domain;
using CurrencyExchange.Infrastructure.Nationalbanken;
using CurrencyExchange.Tests.TestHelpers;

namespace CurrencyExchange.Tests.Infrastructure;

public class NationalbankenRateProviderTests
{
    private static readonly NationalbankenOptions Options = new() { Url = new Uri("https://example.test/rates?lang=en") };

    private static NationalbankenRateProvider CreateProvider(HttpMessageHandler handler) =>
        new(new HttpClient(handler), Options);

    [Fact]
    public async Task GetRatesAsync_ValidResponse_RequestsConfiguredUrlAndParsesRates()
    {
        var xml = NationalbankenRatesParserTests.LoadFixture().ToString();
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK, xml);

        var table = await CreateProvider(handler).GetRatesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(Options.Url, handler.LastRequest?.RequestUri);
        Assert.Equal(NationalbankenRateProvider.SourceName, table.Source);
        Assert.Equal(747.47m, table.GetRate(Currency.Create("EUR").Value).Value.DkkPer100);
    }

    [Fact]
    public async Task GetRatesAsync_ErrorStatus_ThrowsHttpRequestException()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.ServiceUnavailable);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateProvider(handler).GetRatesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetRatesAsync_InvalidXml_ThrowsXmlException()
    {
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.OK, "<html>Maintenance</html");

        await Assert.ThrowsAsync<System.Xml.XmlException>(
            () => CreateProvider(handler).GetRatesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetRatesAsync_Timeout_ThrowsTaskCanceledExceptionWithTimeout()
    {
        var handler = new StubHttpMessageHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var provider = new NationalbankenRateProvider(
            new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(50) },
            Options);

        var exception = await Assert.ThrowsAsync<TaskCanceledException>(
            () => provider.GetRatesAsync(TestContext.Current.CancellationToken));

        Assert.IsType<TimeoutException>(exception.InnerException);
    }
}
