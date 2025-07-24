using OllamaSharp;

var uri = new Uri("http://localhost:11434");
var ollama = new OllamaApiClient(uri);

// https://ollama.com/hoangquan456/qwen3-nothink
ollama.SelectedModel = "hoangquan456/qwen3-nothink:0.6b";

await foreach (var stream in ollama.GenerateAsync("Provide me a C# program that creates an ULID"))
    Console.Write(stream.Response);