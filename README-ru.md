# Adaptive Sprites DMI Tool v2.1

Adaptive Sprites DMI Tool - Windows WPF-приложение для создания, предпросмотра и применения конфигов пиксельных преобразований к BYOND `.dmi` спрайтам.

English documentation: [README.md](README.md)

## Текущая версия

- Версия приложения: `2.1`
- Целевая платформа: Windows x64
- UI: WPF на .NET 8
- Релизный пакет: self-contained `win-x64` ZIP
- Версия схемы JSON-конфига: `1`
- Основной формат конфигов: versioned JSON
- Совместимость со старым форматом: только импорт CSV

Релизный ZIP содержит опубликованное WPF-приложение. Распакуйте архив и запустите:

```text
AdaptiveSpritesDmiTool.Presentation.Wpf.exe
```

## Возможности

- старт с пустой рабочей областью
- ручное открытие базового `.dmi`
- подключение дополнительных landmark и overlay state-источников для предпросмотра
- редактирование pixel mappings для `4-dir` и `8-dir` спрайтов
- инструменты редактора: `Paint`, `Fill`, `Move`, `Erase`, undo, area undo и selection
- предпросмотр base, landmark, overlay, composite, grid и text-grid режимов
- сохранение и загрузка JSON-конфигов со схемной версией
- импорт CSV-конфигов из старых рабочих процессов
- проверка совместимости конфига по разрешению и набору направлений перед применением
- детерминированная пакетная обработка с результатом по каждому файлу
- политики перезаписи для batch: `SkipExisting`, `OverwriteExisting`, `FailIfExists`
- сохранение пользовательских настроек: последние пути, выбранные states, направление, viewport, тема и batch-папки

## Основной сценарий

1. Запустите приложение. Откроется пустая рабочая область.
2. Откройте базовый `.dmi`.
3. Создайте новый конфиг, загрузите JSON или импортируйте CSV.
4. Выберите base, landmark и overlay states через state explorer.
5. Отредактируйте mappings в source/editable панелях.
6. Проверьте результат в composite, grid или text-grid предпросмотре.
7. Сохраните конфиг как JSON.
8. Запустите batch processing по входной папке и проверьте результаты по файлам.

## Форматы конфигов

В v2.1 основной формат - JSON. Текущая схема использует:

- `version: 1`
- `supportedDirections: "four"` или `"eight"`
- `mappings`, сгруппированные по именам направлений
- `target: null` для прозрачного выходного пикселя

CSV можно импортировать, но новые конфиги сохраняются как JSON.

Подробнее:

- [docs/CONFIG_FORMAT.md](docs/CONFIG_FORMAT.md)
- [docs/MIGRATION_GUIDE.md](docs/MIGRATION_GUIDE.md)

## Сборка и запуск из исходников

Требования:

- Windows
- .NET 8 SDK

Обычная developer-сборка:

```powershell
dotnet restore AdaptiveSpritesDMItool.sln -m:1
dotnet build AdaptiveSpritesDMItool.sln -c Release -m:1 -v minimal --no-restore
dotnet test AdaptiveSpritesDMItool.sln -c Release -m:1 -v minimal --no-build
dotnet run --project src/AdaptiveSpritesDmiTool.Presentation.Wpf/AdaptiveSpritesDmiTool.Presentation.Wpf.csproj -c Release
```

Релизный пакет:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./eng/build-release.ps1 -Version v2.1 -Runtime win-x64
```

Скрипт создает:

- `artifacts/publish/AdaptiveSpritesDMItool-v2.1-win-x64/`
- `artifacts/release/AdaptiveSpritesDMItool-v2.1-win-x64.zip`
- `artifacts/release/AdaptiveSpritesDMItool-v2.1-win-x64.sha256.txt`

`artifacts/` - сгенерированный вывод сборки, он намеренно исключен из git.

## Архитектура

Активная runtime-архитектура v2.1 разделена на слои:

- `src/AdaptiveSpritesDmiTool.Domain`
  Чистая доменная модель, value objects, валидация, модель направлений и инварианты конфигов.
- `src/AdaptiveSpritesDmiTool.Application`
  Use cases, editor session, undo/redo, batch orchestration, progress/cancellation, contracts для settings.
- `src/AdaptiveSpritesDmiTool.Infrastructure`
  DMISharp adapters, JSON repositories, CSV importer, settings repository, preview builder, deterministic batch processor.
- `src/AdaptiveSpritesDmiTool.Presentation.Wpf`
  WPF MVVM shell, dialogs, pointer adapter, editor/preview UI, batch UI и runtime hardening.
- `tests/AdaptiveSpritesDmiTool.Tests.Unit`
  Unit coverage для Domain/Application и smoke checks WPF shell.
- `tests/AdaptiveSpritesDmiTool.Tests.Integration`
  JSON persistence, CSV import, DMI adapters, settings persistence и batch processing.

## Тестирование

Для v2.1 пройдена релизная проверка:

- 118 unit tests
- 41 integration tests
- hidden Unicode scan
- Release build
- Release test run
- self-contained Windows x64 publish
- ZIP smoke check

Подробнее: [docs/TEST_PLAN.md](docs/TEST_PLAN.md)

## Ключевые документы

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- [docs/RENDERING.md](docs/RENDERING.md)
- [docs/CONFIG_FORMAT.md](docs/CONFIG_FORMAT.md)
- [docs/MIGRATION_GUIDE.md](docs/MIGRATION_GUIDE.md)
- [docs/TEST_PLAN.md](docs/TEST_PLAN.md)
- [docs/releases/v2.1.md](docs/releases/v2.1.md)

## Лицензия

Репозиторий распространяется по GPL v3. См. [LICENSE](LICENSE).
