## .NET WASI app

Definitely read https://henrikrxn.github.io/blog/Wasi-dotnet-10/ and see the GH issue links for state of WASI.


Created with (after installing `wasi-experimental` workload):

`dotnet new wasiconsole -o HelloWasi`


Building and running:

```
dotnet build -c Debug
cd bin\Debug\net10.0\wasi-wasm\AppBundle
wasmtime run --wasi http --dir . dotnet.wasm HelloWasi
```


Output:

```
Hello, Wasi Console!
OS Description       : WASI
OS Architecture      : Wasm
Framework Description: .NET 10.0.0-rc.2.25502.107
Runtime Identifier   : wasi-wasm
```

---

Default README content created by .NET template follows:

## Build
### Default

You can build the app from Visual Studio or from the command-line:

```
dotnet build -c Debug/Release
```

After building the app, the result is in the `bin/$(Configuration)/net10.0/wasi-wasm/AppBundle` directory.

### As a single file bundle

Add `<WasmSingleFileBundle>true</WasmSingleFileBundle>` to your project file to enable this. It will result in a single `<name_of_the_main_assembly>.wasm` file which contains all the assemblies.

## Run

You can build the app from Visual Studio or the command-line:

```
dotnet run -c Debug/Release
```

Or directly start `wasmtime` from the AppBundle directory:

### default case

```
cd bin/$(Configuration)/net10.0/wasi-wasm/AppBundle
wasmtime run --dir .  -- dotnet.wasm <name_of_the_main_assembly>
```

### for single file bundle

```
cd bin/$(Configuration)/net10.0/wasi-wasm/AppBundle
wasmtime --dir . -- <name_of_the_main_assembly>.wasm
```
