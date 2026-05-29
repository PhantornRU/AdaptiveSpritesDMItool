# Архитектура v2.2

AdaptiveSpritesDMItool v2.2 - WPF-приложение для редактирования и применения pixel-mapping конфигов к `.dmi` sprites.

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

- Application version: `2.2`
- WPF target framework: `net8.0-windows`
- Release runtime: `win-x64`
- Publish mode: self-contained single-file
- Config schema: JSON `version: 1`
- Release executable: `AdaptiveDMITool-v2.2.exe`

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
3. Editor commands update mappings through Application use cases.
4. Preview is built through Infrastructure adapters.
5. Config is saved as JSON.
6. Batch processing applies the active config to selected `.dmi` files or an input folder.

## Imported State Layers

v2.2 keeps imported DMI state layers as workspace state. Each imported state can be:

- assigned to Source and/or Editable surfaces;
- placed as a background or overlay layer;
- ordered explicitly for deterministic composition;
- blended with per-layer opacity;
- restored from workspace settings on startup.

## Рендеринг и фоновые процессы

- **Асинхронный превью:** `DmiSharpPreviewBuilder` работает асинхронно, чтобы не блокировать UI-поток во время сборки составных изображений (base + overlay).
- **Кэширование I/O:** Используется `ConcurrentDictionary` для кэширования загруженных кадров, чтобы избежать повторного чтения с диска при каждой пересборке превью.
- **Batching обновлений:** Инструменты рисования и стирания генерируют множество событий изменения конфигурации. Чтобы не перегружать пайплайн тяжелыми операциями пересборки превью, применяется батчинг (batching) с использованием 16ms таймера. Это позволяет объединить частые обновления в один цикл рендеринга (примерно 60 FPS).
- **Оптимизация рендеринга:** Подробное описание WPF-рендеринга и оптимизаций см. в [docs/RENDERING.md](RENDERING.md).

## Batch Outputs

Batch processing writes output `.dmi` files to the selected output directory.

For tracked runs it can also write internal run artifacts under:

```text
.adaptive-sprites/
```

Those artifacts contain journals and run reports used by incremental batch behavior.
