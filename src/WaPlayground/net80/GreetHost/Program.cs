// https://github.com/extism/dotnet-sdk

using Extism.Sdk;
using System.Text;

var manifest = new Manifest(new PathWasmSource("../../../../jspdksample/plugin.wasm"));
using var plugin = new Plugin(manifest, new HostFunction[] { }, withWasi: true);

var output = Encoding.UTF8.GetString(
    plugin.Call("greet", Encoding.UTF8.GetBytes("Chris"))
);

Console.WriteLine(output);  // Hello, Chris