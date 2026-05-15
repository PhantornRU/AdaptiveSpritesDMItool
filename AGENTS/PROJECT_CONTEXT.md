# Контекст проекта

## Что такое `.dmi` в контексте этого репозитория
- `.dmi` здесь является основным рабочим форматом sprite-ассетов.
- Один `.dmi`-файл содержит набор `states`; у каждого state могут быть направления (`South`, `North`, `East`, `West` и т.д.) и несколько кадров анимации.
- Инструмент не рисует спрайты с нуля. Он изменяет соответствие пикселей и применяет эти преобразования к уже существующим `.dmi`-ассетам.

## Что является source of truth
- Решение и зависимости: `AdaptiveSpritesDMItool.sln`.
- Доменные модели и валидация: `src/AdaptiveSpritesDmiTool.Domain/**`.
- Use cases, контракты, orchestration и session state: `src/AdaptiveSpritesDmiTool.Application/**`.
- DMI/JSON/CSV/preview/batch/storage инфраструктура: `src/AdaptiveSpritesDmiTool.Infrastructure/**`.
- WPF shell, bindings, команды и адаптеры поверхности: `src/AdaptiveSpritesDmiTool.Presentation.Wpf/**`.
- Unit и integration coverage: `tests/AdaptiveSpritesDmiTool.Tests.Unit/**` и `tests/AdaptiveSpritesDmiTool.Tests.Integration/**`.
- Sample assets для ручной проверки: `samples/dmi/**`.

## Основные контуры текущей реализации
- Точка входа и composition root: `src/AdaptiveSpritesDmiTool.Presentation.Wpf/App.xaml` и `App.xaml.cs`.
- Главное окно и shell: `src/AdaptiveSpritesDmiTool.Presentation.Wpf/MainWindow.xaml*`.
- Основная логика shell: `src/AdaptiveSpritesDmiTool.Presentation.Wpf/MainWindowViewModel.cs` и partial-файлы рядом с ним.
- Локальная навигация и секции shell: `src/AdaptiveSpritesDmiTool.Presentation.Wpf/WorkspaceShellSections.cs`.
- Базовые доменные типы: `src/AdaptiveSpritesDmiTool.Domain/Configurations/*` и `src/AdaptiveSpritesDmiTool.Domain/Workspaces/*`.
- Application contracts и use cases: `src/AdaptiveSpritesDmiTool.Application/Contracts.cs`, `EditorSession.cs`, `UseCases.cs`.
- Infrastructure слои: `Configs`, `Dmi`, `Preview`, `BatchProcessing`, `Settings`.

## Практические ограничения
- Не возвращать старый static-controller runtime как основу корректности. Если требуется bridge для миграции, он должен оставаться временным.
- Не менять пути и форматы без причины и без проверки обратной совместимости: `Assets/Import`, `Assets/Export`, `Assets/Storage`, `Assets/Saves`, `.dmi`, `.csv`, `.json`.
- Любое изменение shell-логики проверять вместе с XAML, code-behind, `MainWindowViewModel*` и соответствующими тестами.
- Внешние sample-данные могут использоваться для ручной проверки, но не должны становиться обязательной зависимостью старта приложения.

## Пайплайн работы с `.dmi`
- Пользователь открывает исходный `.dmi`.
- При необходимости подключаются дополнительные state- или preview-источники.
- Конфиг создаётся, импортируется из CSV или загружается из JSON.
- Mapping-операции применяются через `EditorSession` и WPF shell.
- Preview собирается через infrastructure-адаптеры.
- Конфиг сохраняется как versioned JSON.
- Batch processing применяется отдельно к набору входных файлов и должен давать детерминированный результат.

## Архитектурные границы
- `Domain` не должен зависеть от WPF, DMISharp и файловой системы.
- `Application` не должен знать о конкретных WPF-контролах и должен работать через контракты.
- `Infrastructure` отвечает за реальные адаптеры, репозитории и I/O.
- `Presentation.Wpf` отвечает за shell, navigation, bindings и пользовательские команды.

## Что не считать актуальной архитектурой
- Старые ссылки на `Controllers/**`, `Views/**` в корне проекта и root-WPF runtime больше не описывают текущую форму репозитория.
- При чтении старых заметок использовать их только как исторический контекст, а не как source of truth.
