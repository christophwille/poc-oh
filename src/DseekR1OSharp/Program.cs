using OllamaSharp; // https://github.com/awaescher/OllamaSharp

var uri = new Uri("http://localhost:11434");
var ollama = new OllamaApiClient(uri);

// ollama pull deepseek-r1:1.5b
// ollama run deepseek-r1:1.5b
ollama.SelectedModel = "deepseek-r1:1.5b";

await foreach (var stream in ollama.GenerateAsync("Provide me a C# program that creates an ULID"))
    Console.Write(stream.Response);

// Formatting ideas: https://github.com/JeremyMorgan/DeepSeek-Chat-Demo/blob/main/app/src/components/ChatInterface.vue