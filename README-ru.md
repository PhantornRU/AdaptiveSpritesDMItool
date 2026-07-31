# Adaptive Sprites DMI Tool v2.4.0

Adaptive Sprites DMI Tool — Windows WPF-приложение для импорта, организации, предпросмотра, экспорта и применения конфигов пиксельных преобразований к DMI- и PNG-спрайтам.

English documentation: [README.md](README.md)

## Текущая версия

- Версия приложения: `2.4.0`
- Целевая платформа: Windows x64
- UI: WPF на .NET 10 LTS
- Релизный пакет: self-contained `win-x64` ZIP
- Версия схемы JSON-конфига: `2` (чтение v1 и v2)
- Версия схемы Workspace: `8` (миграция v6 и v7)
- Версия sidecar-проекта SpriteDocument: `1`
- Основной формат конфигов: versioned JSON
- Совместимость со старым форматом: только импорт CSV

Релизный ZIP содержит опубликованное WPF-приложение. Распакуйте архив и запустите:

```text
AdaptiveDMITool-v2.4.0.exe
```

## Главное в v2.4.0

- Добавлена source-referenced модель `SpriteDocument`: упорядоченные states, 1/4/8 направлений, animation metadata, crop/transform и ленивое чтение кадров.
- DMI и PNG определяются по содержимому, включая DMI с расширением `.png` и обычный PNG, переименованный в `.dmi`.
- Добавлен раздел Documents для DMI, одиночных PNG, последовательностей, sprite sheets, sidecar-проектов, preview и DMI/PNG export.
- Sidecar schema v1 хранит относительные пути и fingerprints источников, поддерживает явный relink/accept и проверяет лимиты до массового декодирования.
- PNG sheet/sequence записываются в управляемую папку через sibling staging и ownership manifest; чужая папка не перезаписывается.
- Workspace schema v8 сохраняет документ, generic auxiliary layers, форматы batch и raster profile.
- Batch обрабатывает DMI и PNG независимо; ошибка одного output не блокирует другой.
- Hotspot metadata DMI нормализуется и проверяется повторным открытием вместе с остальными metadata и RGBA hash.

V2.4 сохраняет все исправления редактора и writer из V2.3:

- Добавлены русские и английские ресурсы интерфейса с сохраняемой настройкой языка.
- Добавлены настройки оболочки: тема, язык, режим viewport редактора, поведение панелей workspace, видимость неактивных Source-полотен и вписывание нескольких direction-полотен.
- Добавлены scopes редактора направлений: `Single`, `Parallel` и `All`, большие canvas layout для scope-режимов и selector отображаемых направлений.
- Импортированные DMI state-слои можно назначать на Source и Editable поверхности, явно сортировать, размещать как background или overlay и смешивать с индивидуальной opacity.
- Порядок, размещение, opacity и назначение импортированных state-слоев сохраняются в workspace settings и восстанавливаются при запуске.
- Загрузка states и сортировка списков states стали стабильнее, чтобы восстановленные workspace и импортированные DMI state selections были предсказуемее.
- Улучшен batch workspace: локализация, выбор папок и файлов, фильтрация, статусы, run log, исключение output-папки из input-сканирования и предпросмотр `One DIR` / `All DIR`.
- Жесты `Paint`, `Erase`, `Restore`, `Fill`, `Move` и `Select/Move` стали атомарными и создают ровно одну запись Undo.
- Быстрые штрихи интерполируются без пропущенных пикселей; Erase пишет прозрачный RGBA, а Restore удаляет mapping.
- Зеркальная проекция использует точную формулу без скрытого `-1` и clamp; смещение оси хранится в JSON v2 и Workspace v7.
- Добавлена независимая направляющая зеркальной оси на всех canvas.
- DMI writer сохраняет порядок states, повторно проверяет metadata и SHA-256 RGBA-кадров и атомарно заменяет файл.
- Проект переведен на .NET 10 LTS; обновлены WPF-UI, CommunityToolkit.Mvvm и ImageSharp, добавлены lock-файлы и аудит зависимостей.
- Улучшены rendering и производительность при обновлениях редактора, рисовании и zoom.
- Сделано более надежное закрытие приложения и сохранение состояния workspace.
- Release workflow теперь создает ZIP с приложением и отдельный samples ZIP с полной папкой `samples/`.
- VS Code debug переведен на C# Dev Kit `dotnet` debug type, поэтому старый `coreclr` adapter больше не требуется.

## Возможности

- старт с пустой рабочей областью
- ручное открытие базового `.dmi`
- открытие DMI и sidecar-проектов `*.adaptive-dmi.json`
- импорт одиночного PNG, последовательности PNG и sprite sheet с настраиваемой сеткой
- экспорт проверенного DMI и управляемых PNG sheet/sequence
- подключение дополнительных landmark и overlay state-источников для предпросмотра
- редактирование pixel mappings для `4-dir` и `8-dir` спрайтов
- редактирование одного направления, параллельных направлений или всех направлений из одного workspace
- инструменты редактора: `Paint`, `Fill`, `Move`, `Erase`, `Restore`, area restore, undo и selection
- предпросмотр base, landmark, overlay, composite, grid и text-grid режимов
- сохранение и загрузка JSON-конфигов со схемной версией
- импорт CSV-конфигов из старых рабочих процессов
- проверка совместимости конфига по разрешению и набору направлений перед применением
- детерминированная DMI/PNG batch-обработка с результатом по каждому output
- политики перезаписи для batch: `SkipExisting`, `OverwriteExisting`, `FailIfExists`
- предпросмотр batch output direction mode через `One DIR` и `All DIR`
- сохранение пользовательских настроек: последние пути, выбранные states, импортированные DMI state-слои, направление, viewport, язык, тема, поведение панелей, видимость Source-полотен и batch-папки

## Основной сценарий

1. Запустите приложение. Откроется пустая рабочая область.
2. Откройте базовый `.dmi`, sidecar-проект или импортируйте PNG в разделе Documents.
3. Создайте новый конфиг, загрузите JSON или импортируйте CSV.
4. Выберите base, landmark и overlay states через state explorer.
5. Отредактируйте mappings в source/editable панелях.
6. Проверьте результат в composite, grid или text-grid предпросмотре.
7. Сохраните конфиг как JSON.
8. Запустите DMI/PNG batch processing по входной папке и проверьте результаты каждого output.

## Форматы конфигов

В v2.4.0 основной формат mapping-конфигов — JSON. Текущая схема использует:

- `version: 2`
- `editorSettings.mirrorAxisOffsetPixels`
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
- .NET 10 SDK

Обычная developer-сборка:

```powershell
dotnet restore AdaptiveSpritesDMItool.sln --locked-mode -m:1
dotnet build AdaptiveSpritesDMItool.sln -c Release -m:1 -v minimal --no-restore
dotnet test AdaptiveSpritesDMItool.sln -c Release -m:1 -v minimal --no-build
dotnet run --project src/AdaptiveSpritesDmiTool.Presentation.Wpf/AdaptiveSpritesDmiTool.Presentation.Wpf.csproj -c Release
```

VS Code debug:

- установите рекомендованные workspace extensions из `.vscode/extensions.json`
- выберите `Launch AdaptiveSpritesDmiTool WPF (.NET)`
- нажмите F5

Launch-конфигурация использует `type: "dotnet"` и `projectPath`; `coreclr` debug adapter больше не нужен.

Релизный пакет:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./eng/build-release.ps1 -Version v2.4.0 -Runtime win-x64
```

Скрипт создает:

- `artifacts/publish/AdaptiveSpritesDMItool-v2.4.0-win-x64/`
- `artifacts/release/AdaptiveSpritesDMItool-v2.4.0-win-x64.zip`
- `artifacts/release/AdaptiveSpritesDMItool-v2.4.0-win-x64.sha256.txt`
- `artifacts/release/AdaptiveSpritesDMItool-samples-v2.4.0.zip`
- `artifacts/release/AdaptiveSpritesDMItool-samples-v2.4.0.sha256.txt`

`artifacts/` - сгенерированный вывод сборки, он намеренно исключен из git.

## Архитектура

Активная runtime-архитектура v2.4.0 разделена на слои:

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

Автоматизированная проверка v2.4.0 покрывает:

- 241 unit tests
- 74 integration tests
- hidden Unicode scan
- Release build
- Release test run
- self-contained Windows x64 publish
- ZIP smoke check
- samples ZIP smoke check

Подробнее: [docs/TEST_PLAN.md](docs/TEST_PLAN.md)

## Ключевые документы

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- [docs/RENDERING.md](docs/RENDERING.md)
- [docs/CONFIG_FORMAT.md](docs/CONFIG_FORMAT.md)
- [docs/MIGRATION_GUIDE.md](docs/MIGRATION_GUIDE.md)
- [docs/TEST_PLAN.md](docs/TEST_PLAN.md)
- [CHANGELOG.md](CHANGELOG.md)
- [docs/releases/v2.4.0.md](docs/releases/v2.4.0.md)
- [docs/releases/v2.3.0.md](docs/releases/v2.3.0.md)
- [docs/releases/v2.2.md](docs/releases/v2.2.md)
- [docs/releases/v2.1.md](docs/releases/v2.1.md)

## Лицензия

Репозиторий распространяется по GPL v3. См. [LICENSE](LICENSE).
