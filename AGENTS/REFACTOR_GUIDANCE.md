# Refactor Guidance

Этот документ применяется только к крупным архитектурным рефакторингам и миграциям. Для мелких фиксов, локальных UI-правок и docs-задач он не должен включать избыточный процесс.

## Operating mode
- Крупный рефакторинг нужно вести по отдельным workstream-ам, а не как одну монолитную перепись репозитория.
- Сначала определить целевую архитектуру, границы слоев, карту миграции и PR-roadmap, и только затем переходить к реализации.
- Workstream-ы здесь являются концептуальным разделением ответственности. Это не означает обязательный буквальный запуск нескольких sub-agent tools в каждой задаче.
- Изменения должны оставаться layer-responsible и по возможности PR-sized.

## Рекомендуемые workstream-ы
- `architecture`
  - целевая структура решения;
  - dependency graph;
  - legacy-to-new mapping;
  - roadmap миграции.
- `domain`
  - модели пиксельного преобразования, направлений, разрешений и инвариантов;
  - чистая предметная логика без WPF и файловой инфраструктуры.
- `application`
  - use cases и orchestration;
  - progress/cancellation contracts;
  - undo/redo contracts;
  - error/result abstractions.
- `infrastructure`
  - DMI adapters;
  - file system и persistence adapters;
  - JSON repositories;
  - legacy CSV import/migration path.
- `wpf-ui`
  - MVVM structure;
  - editor workspace;
  - batch screen;
  - dialogs, bindings и минимальный code-behind.
- `test`
  - regression matrix;
  - startup/save-load/batch-processing tests;
  - проверки 4-dir и 8-dir сценариев.
- `migration-doc`
  - архитектурные документы;
  - описание формата конфигов;
  - миграционный гайд;
  - сверка README с фактической архитектурой.

## Что из исходного шаблона действительно нужно этому репозиторию
- Не делать монолитный rewrite.
- Для новой или рефакторенной архитектуры не добавлять новый глобальный mutable static state.
- Не размещать domain logic в WPF code-behind.
- Не использовать fire-and-forget async в новом коде без явного жизненного цикла и контроля завершения.
- Не использовать exceptions как обычный механизм control flow.
- Держать изменения небольшими, послойными и с понятной ответственностью.

## Целевые архитектурные ориентиры
- `Domain`
- `Application`
- `Infrastructure`
- `Presentation.Wpf`
- `Unit tests`
- `Integration tests`

Эти слои являются целевой архитектурой для крупной миграции, а не описанием текущей структуры репозитория.

## Конфиги и формат данных
- Текущий проект живет на `.csv` для pixel-mapping конфигов, и это остается рабочим текущим контрактом.
- Для будущего крупного рефакторинга разумно считать `JSON` целевым основным форматом конфигов.
- Поддержка legacy `CSV` нужна как migration/import path, а не как обязательное будущее ядро новой архитектуры.
- Если начнется миграция форматов, она должна быть документирована и обратимо проверяема на реальных sample-данных.

## Продуктовые цели для крупной миграции
- Поддерживать как `4-direction`, так и `8-direction` DMI как целевое поведение refactored pipeline.
- Приложение должно уметь стартовать с пустым workspace без зависимости от demo-ассетов.
- Demo assets полезны для smoke/manual validation, но не должны быть обязательным условием запуска приложения.

## Required first deliverables
Перед большим архитектурным рефакторингом создать и поддерживать:
- `docs/ARCHITECTURE.md`
- `docs/REFACTOR_PLAN.md`
- `docs/TEST_PLAN.md`
- `docs/CONFIG_FORMAT.md`
- `docs/MIGRATION_GUIDE.md`
- `docs/adr/*`

Эти документы не нужно создавать заранее для каждой мелкой задачи. Они нужны именно в момент старта большой миграции.

## Execution order
1. Architecture design first
2. Domain + Application contracts
3. Infrastructure adapters
4. WPF UI migration
5. Regression/integration pass
6. Legacy code removal
7. Docs finalization

## Validation after each major step
- build;
- unit tests, если уже существуют;
- integration tests, если уже существуют;
- smoke validation запуска приложения;
- smoke validation load/save config;
- smoke validation batch processing на sample `.dmi`.
