# Архитектура v2.3.0

AdaptiveSpritesDMItool v2.3.0 - WPF-приложение для редактирования и применения pixel-mapping конфигов к `.dmi` sprites.

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

- Application version: `2.3.0`
- WPF target framework: `net10.0-windows`
- Release runtime: `win-x64`
- Publish mode: self-contained single-file
- Config schema: JSON `version: 2`
- Workspace schema: JSON `version: 7`
- Release executable: `AdaptiveDMITool-v2.3.0.exe`

## Точки Входа

- App composition: `src/AdaptiveSpritesDmiTool.Presentation.Wpf/App.xaml.cs`
- Main window: `src/AdaptiveSpritesDmiTool.Presentation.Wpf/MainWindow.xaml`
- Shell state: `src/AdaptiveSpritesDmiTool.Presentation.Wpf/MainWindowViewModel*.cs`
- Shell sections: `src/AdaptiveSpritesDmiTool.Presentation.Wpf/WorkspaceShellSections.cs`
- Application use cases: `src/AdaptiveSpritesDmiTool.Application/UseCases.cs`
- Batch contracts: `src/AdaptiveSpritesDmiTool.Application/BatchContracts.cs`

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

## Runtime Flow

1. User opens a `.dmi`.
2. User creates a config, loads JSON, or imports CSV.
3. Editor gestures build and validate an `EditorMutationPlan`, then commit once through Application use cases.
4. Preview is built through Infrastructure adapters.
5. Config is saved as JSON.
6. DMI output is saved beside the destination, reopened, fingerprint-verified, and atomically committed.
7. Batch processing applies the active config to selected `.dmi` files or an input folder.

## Imported State Layers

v2.3.0 keeps imported DMI state layers as workspace state. Each imported state can be:

- assigned to Source and/or Editable surfaces;
- placed as a background or overlay layer;
- ordered explicitly for deterministic composition;
- blended with per-layer opacity;
- restored from workspace settings on startup.

## Рендеринг и фоновые процессы

- **Асинхронный превью:** `DmiSharpPreviewBuilder` работает асинхронно, чтобы не блокировать UI-поток во время сборки составных изображений (base + overlay).
- **Кэширование I/O:** Используется `ConcurrentDictionary` для кэширования загруженных кадров, чтобы избежать повторного чтения с диска при каждой пересборке превью.
- **Атомарные жесты:** Paint, Erase и Restore интерполируют координаты и обновляют только временный preview с интервалом около 16ms. Конфигурация и Undo меняются один раз при успешном завершении жеста.
- **Оптимизация рендеринга:** Подробное описание WPF-рендеринга и оптимизаций см. в [docs/RENDERING.md](RENDERING.md).

## Batch Outputs

Batch processing writes output `.dmi` files to the selected output directory.

For tracked runs it can also write internal run artifacts under:

```text
.adaptive-sprites/
```

Those artifacts contain journals and run reports used by incremental batch behavior.
