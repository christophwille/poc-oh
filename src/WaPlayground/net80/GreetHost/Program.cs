// https://github.com/extism/dotnet-sdk

using Extism.Sdk;
using System.Text;

const string jspdksamplePath = "../../../../jspdksample/plugin.wasm";
const string dotnetpdksamplePath = "../../../../GreetPlugin/bin/Debug/net8.0/wasi-wasm/AppBundle/GreetPlugin.wasm";

var manifest = new Manifest(new PathWasmSource(dotnetpdksamplePath));
using var plugin = new Plugin(manifest, new HostFunction[] { }, withWasi: true);

var output = Encoding.UTF8.GetString(
    plugin.Call("greet", Encoding.UTF8.GetBytes("Chris"))
);

Console.WriteLine(output);  // Hello, Chris