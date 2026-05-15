# Adaptive Sprites DMI Tool

Production-grade WPF application for authoring and applying pixel-mapping configs to `.dmi` sprites.

## What It Does

- starts with an empty workspace and no demo/test asset dependency
- opens `.dmi` files manually
- edits per-pixel mappings for `4-dir` and `8-dir` sprites
- previews base, landmark, overlay, composite, grid, and text-grid states
- saves and loads versioned JSON configs
- imports legacy CSV configs as a migration path
- runs deterministic, awaitable, cancellable batch processing with per-file results
- persists workspace/settings across restarts

## Current Workflow

1. Start the app. The shell opens in an empty workspace.
2. Open a `.dmi` file.
3. Create a new config or load/import an existing one.
4. Pick base, landmark, and overlay states from the state explorer.
5. Edit mappings in the source/editable panes with tools such as `Single`, `Fill`, `Delete`, `Undo`, `UndoArea`, `Select`, and `Move`.
6. Save the config as JSON.
7. Run batch processing against an input folder and review per-file results.

## Sample Assets

Optional example `.dmi` files are available under `samples/dmi`.

- they are kept in the repository for manual exploration and debugging
- they are not loaded automatically on startup
- the application still starts with an empty workspace

See [samples/dmi/README.md](samples/dmi/README.md).

## Architecture

The repository is organized as a layered solution:

- `src/AdaptiveSpritesDmiTool.Domain`
  Pure domain model, value objects, validation, direction model, empty workspace model.
- `src/AdaptiveSpritesDmiTool.Application`
  Use cases, editor session, undo/redo, batch orchestration, progress/cancellation, settings contracts.
- `src/AdaptiveSpritesDmiTool.Infrastructure`
  DMISharp adapters, JSON config repository, legacy CSV importer, settings repository, preview builder, deterministic batch processor.
- `src/AdaptiveSpritesDmiTool.Presentation.Wpf`
  MVVM shell, dialogs, pointer adapter, preview/editor UI, startup/runtime hardening.
- `tests/AdaptiveSpritesDmiTool.Tests.Unit`
  Domain, application, and WPF shell smoke tests.
- `tests/AdaptiveSpritesDmiTool.Tests.Integration`
  JSON persistence, legacy CSV import, DMI adapters, settings persistence, batch processing.

Legacy static controllers and the old root WPF runtime path were removed from the active runtime architecture.

## Config Formats

Primary format:

- versioned JSON

Legacy compatibility:

- CSV import only
- new configs are not written as CSV

See:

- [docs/CONFIG_FORMAT.md](docs/CONFIG_FORMAT.md)
- [docs/MIGRATION_GUIDE.md](docs/MIGRATION_GUIDE.md)

## Build And Run

Requirements:

- Windows
- .NET 8 SDK

Commands:

```powershell
dotnet build AdaptiveSpritesDMItool.sln -m:1 -v minimal
dotnet test AdaptiveSpritesDMItool.sln -m:1 -v minimal
dotnet run --project src/AdaptiveSpritesDmiTool.Presentation.Wpf/AdaptiveSpritesDmiTool.Presentation.Wpf.csproj
```

## Testing

Automated coverage currently includes:

- empty-workspace startup
- DMI load validation, including empty/invalid inputs
- JSON config roundtrip and validation failures
- legacy CSV import
- `4-dir` and `8-dir` compatibility
- undo/redo and grouped editor mutations
- deterministic batch overwrite behavior
- real DMI write/apply integration
- workspace settings persistence
- WPF shell smoke checks

See [docs/TEST_PLAN.md](docs/TEST_PLAN.md).

## Key Documents

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- [docs/REFACTOR_PLAN.md](docs/REFACTOR_PLAN.md)
- [docs/TEST_PLAN.md](docs/TEST_PLAN.md)
- [docs/CONFIG_FORMAT.md](docs/CONFIG_FORMAT.md)
- [docs/MIGRATION_GUIDE.md](docs/MIGRATION_GUIDE.md)

## License

This repository is distributed under the terms of the GPL v3 license. See [LICENSE](LICENSE).
