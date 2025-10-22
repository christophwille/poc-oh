using Extism.Sdk;
using System.Text;

const string samplePluginPath = "../../../../ShopSamplePlugin/bin/Debug/net8.0/wasi-wasm/AppBundle/ShopSamplePlugin.wasm";

var functions = new[]
{
    HostFunction.FromMethod("api_getcustomerdata", IntPtr.Zero, (CurrentPlugin plugin, long offset) =>
    {
        var input = plugin.ReadString(offset);

        Console.WriteLine($"Requesting customer data: {input}");

        return plugin.WriteString(@"{ ""ResponseId"": ""42"" }");
    })
};

Plugin.ConfigureCustomLogging(LogLevel.Info);

var manifest = new Manifest(new PathWasmSource(samplePluginPath));
using var plugin = new Plugin(manifest, functions, withWasi: true);

var output = Encoding.UTF8.GetString(
    plugin.Call("calculate_discount", Encoding.UTF8.GetBytes(@"{ ""CustomerId"": 4711, ""TotalInCents"": 8080 }"))
);

Console.WriteLine("Output from discount calculation: " + output);

Plugin.DrainCustomLogs(line => Console.WriteLine($"[Plugin Log] {line}"));