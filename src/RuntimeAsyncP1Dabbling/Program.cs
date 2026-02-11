// https://dotnet.microsoft.com/en-us/download/dotnet/11.0
// https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview1/runtime.md#runtime-async

await WriteToFileAsync("Hello, World!");
Console.WriteLine("done");

static async Task<bool> WriteToFileAsync(string text)
{
    await System.IO.File.WriteAllTextAsync("sample.txt", text);
    return true;
}