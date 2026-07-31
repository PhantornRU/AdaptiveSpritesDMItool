# Архитектура v2.4.0

AdaptiveSpritesDMItool v2.4.0 развивает проверенный V2.3 DMI pipeline до документной модели с нативным DMI и импортом/экспортом PNG.

## Проекты

- `src/AdaptiveSpritesDmiTool.Domain`
  Модель конфигов, координаты, разрешение sprite frame, направления, validation и compatibility checks.
- `src/AdaptiveSpritesDmiTool.Application`
  Use cases, editor session, undo/redo, preview orchestration, batch orchestration, settings contracts.
- `src/AdaptiveSpritesDmiTool.Infrastructure`
  DMISharp adapters, JSON repositories, CSV importer, settings storage, preview builder, batch processing.
- `src/AdaptiveSpritesDmiTool.Presentation.Wpf`
  WPF shell, view models, dialogs, editor surface, preview panel, batch workspace.
- `tests/AdaptiveSpritesDmiTool.Tests.Unit`
  Unit tests для Domain, Application и Presentation view models.
- `tests/AdaptiveSpritesDmiTool.Tests.Integration`
  Integration tests для JSON, CSV, DMI, settings и batch paths.

## Версии

- Application version: `2.4.0` (target for this branch)
- WPF target framework: `net10.0-windows`
- Release runtime: `win-x64`
- Publish mode: self-contained single-file
- Config schema: JSON `version: 2`
- Workspace schema: JSON `version: 8`
- Import project sidecar: JSON `version: 1`
- Release executable: `AdaptiveDMITool-v2.4.0.exe`

## Точки Входа

- App composition: `src/AdaptiveSpritesDmiTool.Presentation.Wpf/App.xaml.cs`
- Main window: `src/AdaptiveSpritesDmiTool.Presentation.Wpf/MainWindow.xaml`
- Shell state: `src/AdaptiveSpritesDmiTool.Presentation.Wpf/MainWindowViewModel*.cs`
- Shell sections: `src/AdaptiveSpritesDmiTool.Presentation.Wpf/WorkspaceShellSections.cs`
- Application use cases: `src/AdaptiveSpritesDmiTool.Application/UseCases.cs`
- Batch/document contracts: `src/AdaptiveSpritesDmiTool.Application/Contracts.cs` and `DocumentContracts.cs`

## Зависимости

Разрешены:

- `Presentation.Wpf -> Application`
- `Presentation.Wpf -> Domain`
- `Infrastructure -> Application`
- `Infrastructure -> Domain`
- `Application -> Domain`

Запрещены:

- `Domain -> WPF`
- `Domain -> filesystem`
- `Domain -> DMISharp`
- `Application -> WPF controls`
- `Presentation.Wpf -> DMISharp`

## Document model

`SpriteDocument` is the format-independent in-memory project graph. It stores ordered states, direction depth, animation metadata and source-referenced frames. Pixel buffers are read lazily through Application contracts.

Content flow:

```text
file -> probe -> DMI/PNG importer -> SpriteDocument -> frame source -> editor/preview/exporter
                                      |
                                      +-> <name>.adaptive-dmi.json
```

The source-reference approach and alternatives are recorded in [ADR 0001](adr/0001-source-referenced-sprite-document.md). The staged implementation is tracked in [REFACTOR_PLAN.md](REFACTOR_PLAN.md).

## Runtime Flow

1. User opens a native DMI or imports PNG graphics after content probing.
2. User creates a config, loads JSON, or imports CSV.
3. Editor gestures build and validate an `EditorMutationPlan`, then commit once through Application use cases.
4. Preview is built through Infrastructure adapters.
5. Config is saved as JSON.
6. DMI output is saved beside the destination, reopened, fingerprint-verified, and atomically committed.
7. Batch processing applies the active config to DMI and/or PNG outputs using saved output profiles.

## Auxiliary Layers

V2.4 keeps DMI or PNG frame references as generic auxiliary workspace layers. Each layer can be:

- assigned to Source and/or Editable surfaces;
- placed as a background or overlay layer;
- ordered explicitly for deterministic composition;
- blended with per-layer opacity;
- restored from Workspace v8 settings on startup with its source format and frame index.

## Рендеринг и фоновые процессы

- **Асинхронный превью:** `DmiSharpPreviewBuilder` работает асинхронно, чтобы не блокировать UI-поток во время сборки составных изображений (base + overlay).
- **Кэширование I/O:** Используется `ConcurrentDictionary` для кэширования загруженных кадров, чтобы избежать повторного чтения с диска при каждой пересборке превью.
- **Атомарные жесты:** Paint, Erase и Restore интерполируют координаты и обновляют только временный preview с интервалом около 16ms. Конфигурация и Undo меняются один раз при успешном завершении жеста.
- **Оптимизация рендеринга:** Подробное описание WPF-рендеринга и оптимизаций см. в [docs/RENDERING.md](RENDERING.md).

## Batch Outputs

Batch processing writes selected DMI and PNG outputs. Raster outputs live in tool-managed directories with an ownership manifest and are staged in a sibling directory before commit.

For tracked runs it can also write internal run artifacts under:

```text
.adaptive-sprites/
```

Those artifacts contain journals and run reports used by incremental batch behavior.
