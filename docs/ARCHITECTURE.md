# Архитектура v2.0

AdaptiveSpritesDMItool v2.0 - WPF-приложение для редактирования и применения pixel-mapping конфигов к `.dmi` sprites.

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

- Application version: `2.0`
- WPF target framework: `net8.0-windows`
- Release runtime: `win-x64`
- Publish mode: self-contained single-file
- Config schema: JSON `version: 1`

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

## Batch Outputs

Batch processing writes output `.dmi` files to the selected output directory.

For tracked runs it can also write internal run artifacts under:

```text
.adaptive-sprites/
```

Those artifacts contain journals and run reports used by incremental batch behavior.
