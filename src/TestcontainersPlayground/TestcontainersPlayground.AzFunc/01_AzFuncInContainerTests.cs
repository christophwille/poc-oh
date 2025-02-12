using Shouldly;

namespace TestcontainersPlayground;

// https://www.tpeczek.com/2023/10/azure-functions-integration-testing.html
public class AzFuncInContainerTests : IClassFixture<AzureFunctionsTestcontainersFixture>
{
    private readonly AzureFunctionsTestcontainersFixture _azureFunctionsTestcontainersFixture;

    public AzFuncInContainerTests(AzureFunctionsTestcontainersFixture azureFunctionsTestcontainersFixture)
    {
        _azureFunctionsTestcontainersFixture = azureFunctionsTestcontainersFixture;
    }

    [Fact]
    public async Task Request_ReturnsResponseWithServerTimingHeader()
    {
        HttpClient httpClient = new HttpClient();
        var requestUri = new UriBuilder(Uri.UriSchemeHttp,
            _azureFunctionsTestcontainersFixture.AzureFunctionsContainerInstance.Hostname,
            _azureFunctionsTestcontainersFixture.AzureFunctionsContainerInstance.GetMappedPublicPort(80),
            "api/SampleHttpFunctionNoDeps").Uri;

        HttpResponseMessage response = await httpClient.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        content.ShouldBe("Welcome to Azure Functions!");
    }
}