# Extism JS Plugins

## Tooling Installation

### Extism CLI

https://github.com/extism/cli

### Extism JS PDK

https://github.com/extism/js-pdk

```
D:\>powershell Invoke-WebRequest -Uri https://raw.githubusercontent.com/extism/js-pdk/main/install-windows.ps1 -OutFile install-windows.ps1

D:\>powershell -executionpolicy bypass -File .\install-windows.ps1
ARCH is x86_64.
Downloading extism-js version v1.2.0.
Installing extism-js.
Missing Binaryen tool(s).
Downloading Binaryen version version_116.
Installing Binaryen.
Install done !
```

Don't forget to add both paths to ENV!

## Testing

```
extism-js plugin.js -i plugin.d.ts -o plugin.wasm
extism call plugin.wasm greet --input="Chris" --wasi
```

