# Adaptive Sprites DMI Tool v2.4.0

Adaptive Sprites DMI Tool is a Windows WPF application for importing, organizing, previewing, exporting, and applying pixel-mapping configs to BYOND DMI and PNG sprites.

Russian documentation: [README-ru.md](README-ru.md)

## Current Release

- Application version: `2.4.0`
- Target platform: Windows x64
- UI framework: WPF on .NET 10 LTS
- Release package: self-contained `win-x64` ZIP
- Config schema version: `2` (reads v1 and v2)
- Workspace schema version: `8` (migrates v6 and v7)
- Sprite document sidecar schema: `1`
- Primary config format: versioned JSON
- CSV compatibility: import only

The release ZIP contains the published WPF app. Extract it and run:

```text
AdaptiveDMITool-v2.4.0.exe
```

## v2.4.0 Highlights

- Added a source-referenced `SpriteDocument` model with ordered states, 1/4/8 directions, animation metadata, crops, transforms, and lazy frame reads.
- Added content-aware DMI/PNG detection, including DMI files carrying a `.png` extension and ordinary PNG files renamed to `.dmi`.
- Added a Documents workspace for native DMI, PNG single/sequence/sprite-sheet import, sidecar projects, preview, and DMI/PNG export.
- Added sidecar schema v1 with source fingerprints, relative-path relocation, per-source Accept/Relink/Cancel recovery, and bounded import validation. Accepted or relinked sources keep the project dirty until the updated provenance is saved.
- Added managed PNG sheet/sequence exports through sibling staging and ownership manifests; unmanaged directories are never replaced.
- Added Workspace schema v8 and mixed DMI/PNG batch with independent outputs and 1/4/8 raster profiles.
- Preserved DMI hotspot metadata through normalization followed by the existing reopen/metadata/RGBA verification.

V2.4 retains the V2.3 editor and writer hardening:

- Atomic editor gestures: Paint, Erase, Restore, Fill, Move, and Select/Move either commit once or leave config and history unchanged.
- Fast Paint, Erase, and Restore strokes interpolate skipped pointer samples; one gesture creates one Undo entry.
- Erase writes transparent RGBA while Restore removes mappings and returns the original pixel.
- Exact direction mirroring uses `x' = width - 1 - x + 2 * offsetPixels`; out-of-bounds projections are skipped instead of clamped.
- The mirror-axis offset is saved in config schema v2 and workspace schema v7, with an independent on-canvas guide.
- DMI output preserves state order, is reopened and verified by metadata and RGBA SHA-256, then committed with same-volume atomic replacement.
- Updated to .NET 10 LTS, WPF-UI 4.3.0, CommunityToolkit.Mvvm 8.4.2, and ImageSharp 3.1.12.
- NuGet lock files, locked CI restore, package vulnerability audit, and a win-x64 release artifact are part of CI.

## What It Does

- starts with an empty workspace
- opens base `.dmi` files manually
- opens DMI and `*.adaptive-dmi.json` document projects
- imports static PNG, ordered PNG sequences, and configured sprite sheets
- exports verified DMI and managed PNG sheet/sequence outputs
- supports optional landmark and overlay state sources for preview work
- edits per-pixel mappings for `4-dir` and `8-dir` sprites
- edits a single direction, parallel directions, or all directions from one workspace
- supports editor tools such as `Paint`, `Fill`, `Move`, `Erase`, `Restore`, area restore, undo, and selection
- previews base, landmark, overlay, composite, grid, and text-grid views
- saves and loads schema-versioned JSON configs
- imports CSV configs from older workflows
- validates config resolution and direction compatibility before apply flows
- runs deterministic DMI/PNG batch processing with per-output status results
- supports batch overwrite policies: `SkipExisting`, `OverwriteExisting`, `FailIfExists`
- previews batch output direction mode with `One DIR` and `All DIR`
- persists the last document, generic DMI/PNG auxiliary layers, batch output formats/profile, and existing editor/shell settings

## Typical Workflow

1. Start the app. The shell opens in an empty workspace.
2. Open a base `.dmi`, open a sidecar project, or import PNG graphics in Documents.
3. Create a new config, load a JSON config, or import a CSV config.
4. Pick base, landmark, and overlay states from the state explorer.
5. Edit mappings in the source/editable panes.
6. Preview the result in composite, grid, or text-grid modes.
7. Save the config as JSON.
8. Run DMI/PNG batch processing against an input folder and review each output result.

## Config Formats

JSON is the primary mapping-config format in v2.4.0. The current config schema uses:

- `version: 2`
- `editorSettings.mirrorAxisOffsetPixels`
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
- .NET 10 SDK

Developer build:

```powershell
dotnet restore AdaptiveSpritesDMItool.sln --locked-mode -m:1
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
powershell -NoProfile -ExecutionPolicy Bypass -File ./eng/build-release.ps1 -Version v2.4.0 -Runtime win-x64
```

The script creates:

- `artifacts/publish/AdaptiveSpritesDMItool-v2.4.0-win-x64/`
- `artifacts/release/AdaptiveSpritesDMItool-v2.4.0-win-x64.zip`
- `artifacts/release/AdaptiveSpritesDMItool-v2.4.0-win-x64.sha256.txt`
- `artifacts/release/AdaptiveSpritesDMItool-samples-v2.4.0.zip`
- `artifacts/release/AdaptiveSpritesDMItool-samples-v2.4.0.sha256.txt`

`artifacts/` is generated output and is intentionally ignored by git.

## Architecture

The active v2.4.0 runtime is a layered solution:

- `src/AdaptiveSpritesDmiTool.Domain`
  Pure config and source-referenced sprite-document models, value objects, validation, direction model, and invariants.
- `src/AdaptiveSpritesDmiTool.Application`
  Use cases, editor session, undo/redo, batch orchestration, progress/cancellation, settings contracts.
- `src/AdaptiveSpritesDmiTool.Infrastructure`
  DMISharp/ImageSharp adapters, content probe, sidecar/config/settings repositories, CSV importer, exporters, and deterministic batch processor.
- `src/AdaptiveSpritesDmiTool.Presentation.Wpf`
  WPF MVVM shell, dialogs, pointer adapter, editor/preview UI, batch UI, startup/runtime hardening.
- `tests/AdaptiveSpritesDmiTool.Tests.Unit`
  Domain, application, and WPF shell smoke coverage.
- `tests/AdaptiveSpritesDmiTool.Tests.Integration`
  JSON persistence, CSV import, DMI adapters, settings persistence, and batch processing coverage.

## Testing

The v2.4.0 automated validation covers:

- 242 unit tests
- 77 integration tests
- hidden Unicode scan
- Release build
- Release test run
- self-contained Windows x64 publish
- ZIP smoke check
- samples ZIP smoke check

See [docs/TEST_PLAN.md](docs/TEST_PLAN.md).

## Key Documents

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- [docs/RENDERING.md](docs/RENDERING.md)
- [docs/CONFIG_FORMAT.md](docs/CONFIG_FORMAT.md)
- [docs/MIGRATION_GUIDE.md](docs/MIGRATION_GUIDE.md)
- [docs/TEST_PLAN.md](docs/TEST_PLAN.md)
- [CHANGELOG.md](CHANGELOG.md)
- [docs/releases/v2.4.0.md](docs/releases/v2.4.0.md)
- [docs/releases/v2.3.0.md](docs/releases/v2.3.0.md)
- [docs/releases/v2.2.md](docs/releases/v2.2.md)
- [docs/releases/v2.1.md](docs/releases/v2.1.md)

## License

This repository is distributed under the terms of the GPL v3 license. See [LICENSE](LICENSE).
