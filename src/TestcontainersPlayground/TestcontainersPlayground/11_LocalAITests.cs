using Testcontainers.Ollama;
using OllamaSharp;

namespace TestcontainersPlayground;

public class LocalAITests
{
    // https://hub.docker.com/r/ollama/ollama/tags you don't want to pull those every time from Docker Hub
    public const string OllamaImage = "ollama/ollama:0.12.11";

    // https://ollama.com/library/gemma3
    public const string ModelToUse = "gemma3:270m";

    // https://github.com/testcontainers/testcontainers-dotnet/issues/1097
    // https://github.com/testcontainers/testcontainers-dotnet/pull/1099/files#diff-67236dff8acc6cf38c8e32c262d85219c27c38f492f88a473a0d689b3ff60d01
    // Note: those can use CPU atm only (another delta of dotnet testcontainers)
    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1051:Calls to methods which accept CancellationToken should use TestContext.Current.CancellationToken", Justification = "<Pending>")]
    public async Task SequentialForIllustrationPurposesOnlyTest()
    {
        // Note: without the volume mount you will download the model every time
        var container = new OllamaBuilder()
            .WithImage(OllamaImage)
            .WithVolumeMount("ollama", "/root/.ollama")  // inside VM (podman, Docker)
            .Build();

        await container.StartAsync();

        await container.ExecAsync(new List<string>() {
            "ollama", "pull", ModelToUse
        });

        string response = await TestModel(container.GetBaseAddress(), ModelToUse);

        var logs = await container.GetLogsAsync();

        await container.StopAsync();
        await container.DisposeAsync();
    }

    // https://github.com/testcontainers/testcontainers-java/pull/8369/files#diff-6ccb0da92bc4dbf274be2b8351c175720311d927814ebf34ff89ebaeeafc873e
    // https://www.docker.com/blog/how-to-run-hugging-face-models-programmatically-using-ollama-and-testcontainers/
    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1051:Calls to methods which accept CancellationToken should use TestContext.Current.CancellationToken", Justification = "<Pending>")]
    public async Task IsThereSomethingSimilarInDotNet()
    {
        // https://github.com/testcontainers/testcontainers-dotnet/discussions/1520   
    }

    private async Task<string> TestModel(string address, string model)
    {
        var uri = new Uri(address);
        var ollama = new OllamaApiClient(uri);

        ollama.SelectedModel = model;

        string response = "";
        await foreach (var stream in ollama.GenerateAsync("What is answer to life, the universe, and everything?"))
        {
            string.Join("\r\n", response, stream.Response);
        }

        return response;
    }
}
