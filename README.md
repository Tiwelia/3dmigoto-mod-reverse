# 3Dmigoto Mod Reverse

Clean-room implementation of the model reverse/export functions used by common
3Dmigoto, XXMI and WWMI mods.

The first supported workflows are:

- normal single-mod INI export;
- DrawIndexed-based mesh splitting;
- ZZZ split-buffer layouts;
- WWMI component export, including dynamic ShapeKey vertex elements;
- original DDS preservation plus PNG/JPG conversion.

The Buffer-based branch workflow is reserved in the architecture and will be
implemented after a representative fixture is available.

## Repository layout

- `src/ModReverse.Core`: ordered 3Dmigoto INI parsing and binary model logic;
- `src/ModReverse.Cli`: batch-friendly command-line interface;
- `src/ModReverse.App`: WPF desktop interface;
- `tests/ModReverse.Tests`: unit and golden-file regression tests.

This repository uses the project-local .NET SDK under `.dotnet` during
development. Release builds will be self-contained for Windows x64.

Texture conversion uses Microsoft's DirectXTex `texconv.exe`. The executable
and its license are kept under `tools/DirectXTex`.

## Build

```powershell
dotnet build ModReverse.slnx
dotnet test ModReverse.slnx
```

## CLI examples

```powershell
dotnet run --project src\ModReverse.Cli -- inspect --ini "D:\Mods\mod.ini"

dotnet run --project src\ModReverse.Cli -- single `
  --game WWMI `
  --ini "D:\Mods\mod.ini" `
  --output "D:\Mods\mod-SingleModIniReverse"

dotnet run --project src\ModReverse.Cli -- drawindexed `
  --game ZZZ `
  --ini "D:\Mods\mod.ini" `
  --output "D:\Mods\mod-DrawIndexedBasedToggleModIniReverse" `
  --jpg
```

The WPF project is `src/ModReverse.App`. Its Buffer-based branch button is
intentionally disabled until a representative Buffer-toggle fixture is
available.
