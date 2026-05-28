# Adaptive Sprites DMI Tool v2.1

Adaptive Sprites DMI Tool is a Windows WPF application for authoring, previewing, and applying pixel-mapping configs to BYOND `.dmi` sprite files.

Russian documentation: [README-ru.md](README-ru.md)

## Current Release

- Application version: `2.1`
- Target platform: Windows x64
- UI framework: WPF on .NET 8
- Release package: self-contained `win-x64` ZIP
- Config schema version: `1`
- Primary config format: versioned JSON
- CSV compatibility: import only

The release ZIP contains the published WPF app. Extract it and run:

```text
AdaptiveSpritesDmiTool.Presentation.Wpf.exe
```

## v2.1 Highlights

- Russian and English UI resources with a persisted language setting.
- Shell controls for theme, language, editor viewport mode, workspace panel behavior, inactive Source canvas visibility, and multi-direction canvas fitting.
- Direction editing scopes for `Single`, `Parallel`, and `All` directions, plus larger scoped canvas layouts and a direction display selector.
- Localized batch workspace with folder/file selection, filtering, status display, run log, output-folder exclusion from input scans, and `One DIR` / `All DIR` preview.
- Fixes for `Fill`, `Move`, mirrored directions, and parallel direction editing.
- Rendering and performance improvements for editor updates, drawing, and zoom.
- More reliable shutdown and workspace state persistence.
- VS Code debug launch updated to the C# Dev Kit `dotnet` debug type, so the old `coreclr` adapter is no longer required.

## What It Does

- starts with an empty workspace
- opens base `.dmi` files manually
- supports optional landmark and overlay state sources for preview work
- edits per-pixel mappings for `4-dir` and `8-dir` sprites
- edits a single direction, parallel directions, or all directions from one workspace
- supports editor tools such as `Paint`, `Fill`, `Move`, `Erase`, undo, area undo, and selection
- previews base, landmark, overlay, composite, grid, and text-grid views
- saves and loads schema-versioned JSON configs
- imports CSV configs from older workflows
- validates config resolution and direction compatibility before apply flows
- runs deterministic batch processing with per-file status results
- supports batch overwrite policies: `SkipExisting`, `OverwriteExisting`, `FailIfExists`
- previews batch output direction mode with `One DIR` and `All DIR`
- persists workspace settings such as recent paths, selected states, direction, viewport, language, theme, panel behavior, source-canvas visibility, and batch folders

## Typical Workflow

1. Start the app. The shell opens in an empty workspace.
2. Open a base `.dmi` file.
3. Create a new config, load a JSON config, or import a CSV config.
4. Pick base, landmark, and overlay states from the state explorer.
5. Edit mappings in the source/editable panes.
6. Preview the result in composite, grid, or text-grid modes.
7. Save the config as JSON.
8. Run batch processing against an input folder and review per-file results.

## Config Formats

JSON is the primary format in v2.1. The current JSON schema uses:

- `version: 1`
- `supportedDirections: "four"` or `"eight"`
- `mappings` grouped by direction name
- `target: null` for transparent output pixels

CSV can be imported, but new configs are saved as JSON.

See:

- [docs/CONFIG_FORMAT.md](docs/CONFIG_FORMAT.md)
- [docs/MIGRATION_GUIDE.md](docs/MIGRATION_GUIDE.md)

## Build And Run From Source

Requirements:

- Windows
- .NET 8 SDK

Developer build:

```powershell
dotnet restore AdaptiveSpritesDMItool.sln -m:1
dotnet build AdaptiveSpritesDMItool.sln -c Release -m:1 -v minimal --no-restore
dotnet test AdaptiveSpritesDMItool.sln -c Release -m:1 -v minimal --no-build
dotnet run --project src/AdaptiveSpritesDmiTool.Presentation.Wpf/AdaptiveSpritesDmiTool.Presentation.Wpf.csproj -c Release
```

VS Code debug:

- install the recommended workspace extensions from `.vscode/extensions.json`
- select `Launch AdaptiveSpritesDmiTool WPF (.NET)`
- press F5

The launch configuration uses `type: "dotnet"` and `projectPath`; it does not require a `coreclr` debug adapter.

Release package:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./eng/build-release.ps1 -Version v2.1 -Runtime win-x64
```

The script creates:

- `artifacts/publish/AdaptiveSpritesDMItool-v2.1-win-x64/`
- `artifacts/release/AdaptiveSpritesDMItool-v2.1-win-x64.zip`
- `artifacts/release/AdaptiveSpritesDMItool-v2.1-win-x64.sha256.txt`

`artifacts/` is generated output and is intentionally ignored by git.

## Architecture

The active v2.1 runtime is a layered solution:

- `src/AdaptiveSpritesDmiTool.Domain`
  Pure domain model, value objects, validation, direction model, and config invariants.
- `src/AdaptiveSpritesDmiTool.Application`
  Use cases, editor session, undo/redo, batch orchestration, progress/cancellation, settings contracts.
- `src/AdaptiveSpritesDmiTool.Infrastructure`
  DMISharp adapters, JSON repositories, CSV importer, settings repository, preview builder, deterministic batch processor.
- `src/AdaptiveSpritesDmiTool.Presentation.Wpf`
  WPF MVVM shell, dialogs, pointer adapter, editor/preview UI, batch UI, startup/runtime hardening.
- `tests/AdaptiveSpritesDmiTool.Tests.Unit`
  Domain, application, and WPF shell smoke coverage.
- `tests/AdaptiveSpritesDmiTool.Tests.Integration`
  JSON persistence, CSV import, DMI adapters, settings persistence, and batch processing coverage.

## Testing

The v2.1 release validation passed:

- 118 unit tests
- 41 integration tests
- hidden Unicode scan
- Release build
- Release test run
- self-contained Windows x64 publish
- ZIP smoke check

See [docs/TEST_PLAN.md](docs/TEST_PLAN.md).

## Key Documents

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- [docs/RENDERING.md](docs/RENDERING.md)
- [docs/CONFIG_FORMAT.md](docs/CONFIG_FORMAT.md)
- [docs/MIGRATION_GUIDE.md](docs/MIGRATION_GUIDE.md)
- [docs/TEST_PLAN.md](docs/TEST_PLAN.md)
- [docs/releases/v2.1.md](docs/releases/v2.1.md)

## License

This repository is distributed under the terms of the GPL v3 license. See [LICENSE](LICENSE).
