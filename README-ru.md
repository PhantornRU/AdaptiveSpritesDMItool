# Adaptive Sprites DMI Tool

Production-grade WPF-приложение для редактирования конфигов пиксельных преобразований и пакетной обработки `.dmi`.

## Возможности

- пустой workspace на старте без зависимости от demo/test assets
- ручное открытие `.dmi`
- редактирование pixel mappings для `4-dir` и `8-dir`
- предпросмотр `base`, `landmark`, `overlay`, `composite`, `grid` и `text-grid`
- сохранение и загрузка versioned JSON-конфигов
- импорт legacy CSV как миграционный путь
- детерминированная, awaitable и cancellable batch processing
- сохранение пользовательских настроек и путей между запусками

## Пользовательский сценарий

1. Запустить приложение. Откроется пустой workspace.
2. Открыть `.dmi`.
3. Создать новый конфиг или загрузить/import существующий.
4. Выбрать `base`, `landmark` и `overlay` через state explorer.
5. Редактировать mapping’и в source/editable pane инструментами `Single`, `Fill`, `Delete`, `Undo`, `UndoArea`, `Select`, `Move`.
6. Сохранить конфиг в JSON.
7. Запустить batch processing по папке и проверить per-file results.

## Архитектура

Репозиторий разделен на слои:

- `src/AdaptiveSpritesDmiTool.Domain`
  Чистая доменная модель, value objects, валидация, direction model, empty workspace model.
- `src/AdaptiveSpritesDmiTool.Application`
  Use cases, editor session, undo/redo, batch orchestration, progress/cancellation, settings contracts.
- `src/AdaptiveSpritesDmiTool.Infrastructure`
  Адаптеры DMISharp, JSON repository, legacy CSV importer, settings repository, preview builder, deterministic batch processor.
- `src/AdaptiveSpritesDmiTool.Presentation.Wpf`
  MVVM shell, dialogs, pointer adapter, preview/editor UI, startup/runtime hardening.
- `tests/AdaptiveSpritesDmiTool.Tests.Unit`
  Domain/application тесты и WPF shell smoke checks.
- `tests/AdaptiveSpritesDmiTool.Tests.Integration`
  Persistence, DMI adapters, settings, batch, legacy CSV import.

Legacy static controllers и старый root WPF runtime path удалены из финальной runtime-архитектуры.

## Форматы конфигов

Основной формат:

- versioned JSON

Совместимость:

- CSV поддерживается только как import path
- новые конфиги в CSV не записываются

См.:

- [docs/CONFIG_FORMAT.md](docs/CONFIG_FORMAT.md)
- [docs/MIGRATION_GUIDE.md](docs/MIGRATION_GUIDE.md)

## Сборка и запуск

Требования:

- Windows
- .NET 8 SDK

Команды:

```powershell
dotnet build AdaptiveSpritesDMItool.sln -m:1 -v minimal
dotnet test AdaptiveSpritesDMItool.sln -m:1 -v minimal
dotnet run --project src/AdaptiveSpritesDmiTool.Presentation.Wpf/AdaptiveSpritesDmiTool.Presentation.Wpf.csproj
```

## Тестирование

Автоматически покрыты:

- startup без demo assets
- empty workspace
- валидация загрузки `.dmi`, включая empty/invalid cases
- JSON roundtrip и ошибки валидации
- import legacy CSV
- сценарии `4-dir` и `8-dir`
- undo/redo и grouped editor mutations
- overwrite behavior и deterministic batch processing
- реальные integration tests для DMI writer/apply
- persistence пользовательских настроек
- smoke checks нового WPF shell

Подробности: [docs/TEST_PLAN.md](docs/TEST_PLAN.md)

## Ключевые документы

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- [docs/REFACTOR_PLAN.md](docs/REFACTOR_PLAN.md)
- [docs/TEST_PLAN.md](docs/TEST_PLAN.md)
- [docs/CONFIG_FORMAT.md](docs/CONFIG_FORMAT.md)
- [docs/MIGRATION_GUIDE.md](docs/MIGRATION_GUIDE.md)

## Лицензия

Репозиторий распространяется по GPL v3. См. [LICENSE](LICENSE).
