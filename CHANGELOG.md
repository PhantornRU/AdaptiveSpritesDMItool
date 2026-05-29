# Changelog

## v2.2

- Updated application metadata to `2.2`.
- Added imported DMI state layers for Source and Editable surfaces.
- Added imported layer `Background` / `Overlay` placement, explicit layer order, and per-layer opacity.
- Persisted imported layer assignment, placement, order, and opacity in workspace settings.
- Improved state loading and state ordering for restored workspaces and imported DMI state selections.
- Updated sample DMI data used by state-loading validation.
- Updated release packaging so the application ZIP contains `AdaptiveDMITool-v2.2.exe`.
- Added a separate samples ZIP containing the full `samples/` folder.
- Updated README, Russian README, architecture/config/rendering/test docs, and release notes for v2.2.

Release notes: [docs/releases/v2.2.md](docs/releases/v2.2.md)

## v2.1

- Added Russian and English UI resources and language selection.
- Added shell/settings controls for theme, language, editor viewport, panels, source-canvas visibility, and multi-direction canvas fitting.
- Added direction scopes `Single`, `Parallel`, and `All`.
- Improved batch workspace UI and preview modes.
- Fixed `Fill`, `Move`, mirrored direction, and parallel direction scenarios.
- Improved rendering/performance and shutdown behavior.
- Updated VS Code debug launch to C# Dev Kit `dotnet` debug type.

Release notes: [docs/releases/v2.1.md](docs/releases/v2.1.md)
