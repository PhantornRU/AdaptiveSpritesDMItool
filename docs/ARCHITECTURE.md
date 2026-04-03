# Architecture

## Purpose

AdaptiveSpritesDMItool is being migrated from a static-controller WPF application to a layered application with explicit boundaries:

- `Domain`: pure business model and validation
- `Application`: use cases, orchestration, results, undo/redo, progress, cancellation
- `Infrastructure`: DMISharp adapters, file system, JSON persistence, legacy CSV import, preview extraction, batch processing
- `Presentation.Wpf`: MVVM shell, dialogs, bindings, pointer adapters
- `Tests`: unit and integration coverage for critical workflows

The refactor is contract-first. The final architecture must not keep the current static-controller model as its runtime core.

## Current Architecture Audit

### Source of truth observed in the current repository

- Entry point and DI: `App.xaml`, `App.xaml.cs`
- Startup shell: `Services/ApplicationHostService.cs`, `Views/Windows/MainWindow.xaml*`
- Navigation: `Services/PageService.cs`, `ViewModels/Windows/MainWindowViewModel.cs`
- Runtime/global state: `Controllers/EnvironmentController.cs`, `Controllers/StatesController.cs`, `Controllers/DrawController.cs`, `Controllers/EditorController.cs`, `Controllers/MouseController.cs`, `Controllers/ButtonsController.cs`, `Controllers/StatusBarController.cs`
- Config storage and preview state: `Resources/DataPixelStorage.cs`, `Models/DataImageState.cs`
- File processing: `Processors/DMIStatesProcessor.cs`
- Infrastructure-like helpers mixed into app core: `Helpers/ImageEncoder.cs`, `Helpers/FilesSearcher.cs`, `Helpers/SettingsManager.cs`, `Helpers/UndoSavesManager.cs`
- Legacy UI-heavy workflow: `Views/Pages/StatesEditorPage.xaml.cs`, `Views/Pages/DataPage.xaml.cs`, `ViewModels/Pages/StatesEditorViewModel.cs`, `ViewModels/Pages/DataViewModel.cs`

### Confirmed architectural violations

1. Startup depends on demo/test assets.
   Evidence:
   `Controllers/EnvironmentController.cs` loads `default.dmi`, `testBodyMonkey.dmi`, and `testClothingDefaultCoat.dmi` during initialization.

2. Business logic is coupled to mutable static state.
   Evidence:
   `Controllers/StatesController.cs` stores edit mode, quantity mode, directions, UI toggles, `ObservableCollection<ConfigItem>`, WPF `Image` references, and status bar references.

3. Domain-like config state is coupled to UI rendering and persistence.
   Evidence:
   `Resources/DataPixelStorage.cs` owns mappings, calls `DrawController`, integrates undo, and reads/writes CSV directly.

4. Direction support is hardcoded to four directions in critical paths.
   Evidence:
   `Resources/DataPixelStorage.cs` stores `DirectionDepth directionDepth = DirectionDepth.Four`.
   `Models/DataImageState.cs` initializes preview dictionaries with `StatesController.GetAllStateDirections(DirectionDepth.Four)`.

5. Batch processing is unsafe and UI-coupled.
   Evidence:
   `Processors/DMIStatesProcessor.cs` stores static `ProgressBar`, `Label`, shared counters, shared `Dictionary<string, DMIFile>`, and starts background work with `Task.Run`.

6. Save decision for processed DMI files is not deterministic.
   Evidence:
   `Processors/DMIStatesProcessor.cs` decides `isNeedSave` from state count and then logs `NOT SAVED` even when contents can differ.

7. UI code-behind is oversized and contains workflow logic.
   Evidence:
   `Views/Pages/StatesEditorPage.xaml.cs` is 765 lines and registers control references into static dictionaries.
   `Views/Pages/DataPage.xaml.cs` drives scanning, progress, config selection, and processing flow.

8. File system and path contracts are string-hacked.
   Evidence:
   `Helpers/FilesSearcher.cs` uses `Replace` against import paths to derive export paths.

9. Settings persistence writes directly into global controller state.
   Evidence:
   `Helpers/SettingsManager.cs` deserializes straight into `StatesController` fields.

10. README, license, and package metadata are inconsistent.
    Evidence:
    `README.md` states MIT for samples, `LICENSE` is GPLv3, and `AdaptiveSpritesDMItool.csproj` has incorrect `Authors` property syntax and a package asset path outside the repository.

## Why the static controllers must be removed

- They hide dependencies and side effects across unrelated pages.
- They make startup order part of correctness.
- They make testing expensive because every test needs global reset semantics.
- They force UI controls into application state, which blocks headless testing and deterministic orchestration.
- They make async correctness worse because work can mutate shared globals from multiple flows.

The final runtime architecture will allow temporary bridges only during migration. The final solution will not depend on those controllers for correctness.

## Target Solution Structure

```text
src/
  AdaptiveSpritesDmiTool.Domain/
  AdaptiveSpritesDmiTool.Application/
  AdaptiveSpritesDmiTool.Infrastructure/
  AdaptiveSpritesDmiTool.Presentation.Wpf/

tests/
  AdaptiveSpritesDmiTool.Tests.Unit/
  AdaptiveSpritesDmiTool.Tests.Integration/

docs/
  ARCHITECTURE.md
  REFACTOR_PLAN.md
  TEST_PLAN.md
  CONFIG_FORMAT.md
  MIGRATION_GUIDE.md
  adr/
```

The solution file may remain `AdaptiveSpritesDMItool.sln` initially, but the project boundaries above are the target architecture.

## Dependency Rules

Allowed dependencies:

- `Presentation.Wpf -> Application`
- `Presentation.Wpf -> Domain` only for read-only DTO/value display if needed, but prefer `Application` contracts
- `Infrastructure -> Application`
- `Infrastructure -> Domain`
- `Application -> Domain`

Forbidden dependencies:

- `Domain -> WPF`, `Domain -> DMISharp`, `Domain -> filesystem`, `Domain -> dialogs`
- `Application -> WPF controls`
- `Application -> concrete filesystem or DMISharp types`
- `Presentation.Wpf -> DMISharp`
- any layer -> mutable static global state container

## Bounded Responsibilities

### Domain

- `SpriteConfig` aggregate root
- coordinate, resolution, direction, metadata, validation types
- invariants for mappings
- compatibility rules for resolution and direction depth
- empty workspace model

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

## Domain Must Not Depend On WPF

- Domain validation must run in tests without a windowing subsystem.
- Batch processing and config import must be reusable outside WPF.
- Direction, resolution, mappings, and compatibility rules are business concepts, not UI concepts.
- The current design leaks `WriteableBitmap`, `Image`, `TextBlock`, and control appearance into business flow. That prevents clean reuse and makes every regression harder to isolate.

## Why the primary config format must be versioned JSON

- The current CSV has no schema version, metadata, or strict validation.
- It cannot represent import provenance, timestamps, or migration details cleanly.
- It is brittle for future extension such as additional direction metadata, validation diagnostics, or per-config settings.
- JSON can be versioned, validated, round-tripped, and migrated while still preserving legacy CSV as import-only compatibility.

Target config requirements:

- `version`
- `name`
- `resolution`
- `directionDepth` and ordered `supportedDirections`
- `mappingsByDirection`
- `metadata` with `createdUtc`, `updatedUtc`, `source`, `importedFromLegacy`

## Why batch processing belongs in Application and Infrastructure

- Batch flow is orchestration, not presentation.
- Determinism, cancellation, progress, overwrite policy, and per-file reports are workflow concerns.
- Actual DMI reading and writing are infrastructure concerns.
- The current processor mixes UI controls, file traversal, state mutation, and transformation.

The replacement will be:

- `Application`: `ApplyConfigToDmiBatchUseCase`
- `Infrastructure`: batch processor + DMI adapter + filesystem traversal

## 4-dir and 8-dir modeling

The new model will not treat four-direction as the default truth.

Target shape:

- `SpriteDirection` enum with 8 values
- `SupportedDirectionSet` value object
- `DirectionDepth` domain concept derived from the set
- ordered directions exposed by the value object
- compatibility validation between config and DMI document

Rules:

- four-direction configs own only cardinal directions
- eight-direction configs own all cardinal and diagonal directions
- no hidden widening or narrowing without explicit compatibility checks
- preview, editor, and batch flows must take direction set from the loaded DMI or loaded config, not from hardcoded defaults

## Legacy To New Mapping

| Legacy file/class | Replacement |
| --- | --- |
| `Controllers/EnvironmentController.cs` | application workspace/session service + infrastructure path/settings services |
| `Controllers/StatesController.cs` | application editor session + presentation view model state |
| `Controllers/DrawController.cs` | application mapping operations + infrastructure preview renderer + presentation overlays |
| `Controllers/EditorController.cs` | application edit commands/use cases |
| `Controllers/MouseController.cs` | presentation pointer adapter |
| `Controllers/ButtonsController.cs` | view model commands and key bindings |
| `Controllers/StatusBarController.cs` | view model derived status text |
| `Resources/DataPixelStorage.cs` | domain `SpriteConfig` + infrastructure repositories + application history |
| `Models/DataImageState.cs` | application preview state + infrastructure DMI preview extraction |
| `Processors/DMIStatesProcessor.cs` | application batch use case + infrastructure batch processor |
| `Helpers/ImageEncoder.cs` | infrastructure imaging adapter |
| `Helpers/FilesSearcher.cs` | infrastructure filesystem/path service |
| `Helpers/SettingsManager.cs` | infrastructure settings repository |
| `Helpers/UndoSavesManager.cs` | application undo/redo history |
| `ViewModels/Pages/StatesEditorViewModel.cs` | `WorkspaceShellViewModel`, `EditorWorkspaceViewModel`, `PreviewPaneViewModel`, `ConfigExplorerViewModel`, `ToolbarViewModel` |
| `ViewModels/Pages/DataViewModel.cs` | batch screen view model + application batch use cases |
| `Views/Pages/StatesEditorPage.xaml.cs` | MVVM editor page with thin pointer bridge |
| `Views/Pages/DataPage.xaml.cs` | MVVM batch page |
| `Services/ApplicationHostService.cs` | startup orchestration service |
| `App.xaml.cs` | composition root + exception handling |
| `Models/ConfigItem.cs`, `Models/StateItem.cs`, `Models/EnvironmentItem.cs`, `Models/AppConfig.cs` | layer-specific DTOs and domain/application contracts |

## Legacy Files And Classes To Rework

Mandatory legacy inventory:

- `Controllers/EnvironmentController.cs`
- `Controllers/StatesController.cs`
- `Controllers/DrawController.cs`
- `Controllers/EditorController.cs`
- `Controllers/MouseController.cs`
- `Controllers/ButtonsController.cs`
- `Controllers/StatusBarController.cs`
- `Resources/DataPixelStorage.cs`
- `Models/DataImageState.cs`
- `Processors/DMIStatesProcessor.cs`
- `Helpers/ImageEncoder.cs`
- `Helpers/FilesSearcher.cs`
- `Helpers/SettingsManager.cs`
- `Helpers/UndoSavesManager.cs`
- `Helpers/ShowMessages.cs`
- `ViewModels/Pages/StatesEditorViewModel.cs`
- `ViewModels/Pages/DataViewModel.cs`
- `ViewModels/Pages/DashboardViewModel.cs`
- `Views/Pages/StatesEditorPage.xaml.cs`
- `Views/Pages/DataPage.xaml.cs`
- `Views/Pages/DashboardPage.xaml.cs`
- `Views/Windows/MainWindow.xaml.cs`
- `Services/ApplicationHostService.cs`
- `App.xaml.cs`
- `Models/ConfigItem.cs`
- `Models/StateItem.cs`
- `Models/AppConfig.cs`
- `Models/EnvironmentItem.cs`
- `Models/StateEditType.cs`

## Target Runtime Flow

1. App starts into an empty workspace.
2. User opens a base DMI manually.
3. Optional landmark and overlay DMI/state are loaded independently.
4. User creates or loads a JSON config.
5. Editor session applies mapping operations through application commands.
6. Preview is rebuilt through an application use case backed by infrastructure adapters.
7. Config is saved as versioned JSON.
8. Legacy CSV is supported through an explicit import use case.
9. Batch processing scans input files deterministically, applies the selected config, and emits per-file results.

## Definition Of Architecture Done

- No mandatory dependency on demo assets at startup
- No static mutable controllers required for runtime correctness
- Domain is free of WPF, DMISharp, and filesystem dependencies
- JSON is the primary config format
- CSV remains import-only
- Batch flow is awaitable, cancellable, deterministic, and reported per file
- 4-dir and 8-dir are modeled explicitly and validated
- UI uses MVVM with minimal code-behind and no control registries as app state