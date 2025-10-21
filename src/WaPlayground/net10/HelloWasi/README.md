## .NET WASI app

Definitely read https://henrikrxn.github.io/blog/Wasi-dotnet-10/ and see the GH issue links for state of WASI.


Created with (after installing `wasi-experimental` workload):

`dotnet new wasiconsole -o HelloWasi`

Publish won't work with WASI SDK 27 (latest), had to go back to WASI SDK 25:

`dotnet publish -c Release` 

Fun part - without running dotnet publish the `bin/$(Configuration)/net10.0/wasi-wasm/AppBundle` directory looks like this:

```
Mode                 LastWriteTime         Length Name
----                 -------------         ------ ----
d----          10/21/2025  9:12 AM                tmp
-a---          10/21/2025  9:12 AM           2492 HelloWasi.runtimeconfig.json
-a---          10/21/2025  9:12 AM       27034753 HelloWasi.wasm
-a---          10/21/2025  9:12 AM            463 node.mjs
-a---          10/21/2025  9:12 AM             79 run-node.sh
-a---          10/21/2025  9:12 AM             32 run-wasmtime.sh
```

with publish:

```
Mode                 LastWriteTime         Length Name
----                 -------------         ------ ----
d----          10/21/2025  9:12 AM                tmp
-a---          10/21/2025  9:13 AM           2492 HelloWasi.runtimeconfig.json
-a---          10/21/2025  9:13 AM        5943555 HelloWasi.wasm
-a---          10/21/2025  9:13 AM            463 node.mjs
-a---          10/21/2025  9:13 AM             79 run-node.sh
-a---          10/21/2025  9:13 AM             32 run-wasmtime.sh
```

Running after build only:

```
wasmtime --dir . -- HelloWasi.wasm
Error: failed to run main module `HelloWasi.wasm`

Caused by:
    0: component imports instance `wasi:http/types@0.2.0`, but a matching implementation was not found in the linker
    1: instance export `fields` has the wrong type
    2: resource implementation is missing
```

Running after publish:

```
Hello, Wasi Console!
OS Description       : WASI
OS Architecture      : Wasm
Framework Description: .NET 10.0.0-rc.2.25502.107
Runtime Identifier   : wasi-wasm
```

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
