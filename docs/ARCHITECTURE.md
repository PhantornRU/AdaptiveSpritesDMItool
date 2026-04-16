# Architecture

## Purpose

AdaptiveSpritesDMItool is organized as a layered WPF application with explicit boundaries:

- `Domain`: pure business model, value objects, and validation
- `Application`: use cases, editor session, orchestration, undo/redo, progress, cancellation
- `Infrastructure`: DMI adapters, file system access, JSON persistence, legacy CSV import, preview extraction, batch processing, settings storage
- `Presentation.Wpf`: MVVM shell, window chrome, bindings, dialogs, and pointer/interaction adapters
- `Tests`: unit and integration coverage for the contracts between those layers

The current runtime should stay aligned with this structure. The old static-controller runtime is historical context, not the source of truth for new work.

## Current Source Of Truth

- Entry point and host composition: `src/AdaptiveSpritesDmiTool.Presentation.Wpf/App.xaml` and `App.xaml.cs`
- Main shell window: `src/AdaptiveSpritesDmiTool.Presentation.Wpf/MainWindow.xaml` and `MainWindow.xaml.cs`
- Shell state and commands: `src/AdaptiveSpritesDmiTool.Presentation.Wpf/MainWindowViewModel*.cs`
- Section/view models used by the shell: `src/AdaptiveSpritesDmiTool.Presentation.Wpf/WorkspaceShellSections.cs`
- Domain models and invariants: `src/AdaptiveSpritesDmiTool.Domain/Configurations/**` and `src/AdaptiveSpritesDmiTool.Domain/Workspaces/**`
- Application contracts and use cases: `src/AdaptiveSpritesDmiTool.Application/Contracts.cs`, `EditorSession.cs`, `UseCases.cs`
- Infrastructure adapters: `src/AdaptiveSpritesDmiTool.Infrastructure/Configs/**`, `Dmi/**`, `Preview/**`, `BatchProcessing/**`, `Settings/**`
- Unit and integration tests: `tests/AdaptiveSpritesDmiTool.Tests.Unit/**` and `tests/AdaptiveSpritesDmiTool.Tests.Integration/**`

## Dependency Rules

Allowed dependencies:

- `Presentation.Wpf -> Application`
- `Presentation.Wpf -> Domain` only for read-only display/value types when needed
- `Infrastructure -> Application`
- `Infrastructure -> Domain`
- `Application -> Domain`

Forbidden dependencies:

- `Domain -> WPF`
- `Domain -> filesystem`
- `Domain -> DMISharp`
- `Application -> WPF controls`
- `Presentation.Wpf -> DMISharp`
- any layer -> mutable static global state container

## Bounded Responsibilities

### Domain

- `SpriteConfig` and supporting value objects
- coordinate, resolution, direction, and compatibility validation
- mapping invariants and empty-workspace model

### Application

- editor session state
- use cases
- undo/redo orchestration
- batch job orchestration
- progress and cancellation contracts
- result/error model
- overwrite policy

### Infrastructure

- DMI read/write and frame access
- preview extraction and image conversion
- JSON config repository
- legacy CSV importer
- filesystem/path services
- workspace/settings persistence
- deterministic batch engine

### Presentation.Wpf

- shell and navigation
- commands and bindings
- dialogs
- pointer adapters
- view models
- error presentation
- progress display

## Runtime Flow

1. App starts into an empty workspace.
2. User opens a base `.dmi` manually.
3. Optional landmark and overlay sources are loaded independently.
4. User creates or loads a config.
5. Editor session applies mapping operations through application commands.
6. Preview is rebuilt through an application use case backed by infrastructure adapters.
7. Config is saved as versioned JSON.
8. Legacy CSV remains supported through an explicit import path.
9. Batch processing scans input files deterministically, applies the selected config, and emits per-file results.

## Current Constraints

- The app should not depend on demo/sample assets for startup correctness.
- Config persistence and batch processing are contract-sensitive; changing them requires corresponding test updates.
- `4-dir` and `8-dir` support must remain explicit and validated rather than inferred from UI defaults.
- UI should stay MVVM-first; code-behind should remain thin and focused on interaction glue.

## Definition Of Done For Architecture Changes

- No mandatory dependency on demo assets at startup
- No static mutable controllers required for runtime correctness
- Domain remains free of WPF, DMISharp, and filesystem dependencies
- JSON remains the primary config format
- CSV remains import-only compatibility
- Batch flow is awaitable, cancellable, deterministic, and reported per file
- 4-dir and 8-dir are modeled explicitly and validated
- UI uses MVVM with minimal code-behind and no control registries as application state
