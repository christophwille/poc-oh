namespace SagasWithWolverine.Web;

public class OrderApiClient(HttpClient httpClient)
{
    public async Task<string?> StartOrderAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync("/orders/start", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<StartOrderResult>(cancellationToken);
        return result?.OrderId;
    }

    public async Task CompleteOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync($"/orders/{orderId}/complete", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

record StartOrderResult(string OrderId);
