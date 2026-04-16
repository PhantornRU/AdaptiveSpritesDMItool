# AGENTS.md

Каноническая точка входа для Codex и совместимых AI-агентов в этом репозитории.

## Порядок чтения
1. [AGENTS/README.md](./AGENTS/README.md)
2. Stable guidance:
   - [AGENTS/PROJECT_CONTEXT.md](./AGENTS/PROJECT_CONTEXT.md)
   - [AGENTS/REFACTOR_GUIDANCE.md](./AGENTS/REFACTOR_GUIDANCE.md)
   - [AGENTS/CONFIRMED_UNRESOLVED_ERRORS.md](./AGENTS/CONFIRMED_UNRESOLVED_ERRORS.md)
   - [AGENTS/WORKFLOW_RULES.md](./AGENTS/WORKFLOW_RULES.md)
   - [AGENTS/POLICIES.md](./AGENTS/POLICIES.md)
   - [AGENTS/REQUEST_PATTERNS.md](./AGENTS/REQUEST_PATTERNS.md)
   - [AGENTS/local/README.md](./AGENTS/local/README.md)
   - `AGENTS/local/PLAN.md`
   - `AGENTS/local/TODO.md`
   - `AGENTS/local/DECISIONS.md`
   - `AGENTS/local/EVIDENCE.md`
   - `AGENTS/local/logs/`
3. Затем только релевантные документы и код проекта:
   - [README.md](./README.md)
   - [README-ru.md](./README-ru.md)
   - [AdaptiveSpritesDMItool.sln](./AdaptiveSpritesDMItool.sln)
   - `src/AdaptiveSpritesDmiTool.Domain/**`
   - `src/AdaptiveSpritesDmiTool.Application/**`
   - `src/AdaptiveSpritesDmiTool.Infrastructure/**`
   - `src/AdaptiveSpritesDmiTool.Presentation.Wpf/**`
   - `tests/AdaptiveSpritesDmiTool.Tests.Unit/**`
   - `tests/AdaptiveSpritesDmiTool.Tests.Integration/**`
   - `docs/**`, если задача затрагивает архитектуру, тестирование, контракты или миграцию
   - `samples/dmi/**`, если задача затрагивает импорт, экспорт или ручную проверку sample-данных

Если локальные `PLAN/TODO/DECISIONS/EVIDENCE` отсутствуют или не относятся к текущей задаче, ориентироваться только на stable guidance и затем на релевантные файлы проекта.

## Жесткие правила
- Перед правками собрать контекст: entrypoints, callsites, data flow, side effects. В проекте есть сильная связность между слоями, поэтому локальная правка легко даёт побочный эффект рядом.
- Для поиска использовать `rg` и точечное чтение файлов, а не широкое сканирование дерева.
- Сохранять текущие слои ответственности: `Domain` для модели и валидации, `Application` для use case и orchestration, `Infrastructure` для файлов/JSON/DMI, `Presentation.Wpf` для shell, bindings и команд, `Tests` для unit/integration coverage.
- При изменении shell-логики проверять регистрацию в `App.xaml.cs`, связь с `MainWindow.xaml`, `MainWindow.xaml.cs` и `MainWindowViewModel*.cs`.
- Не менять без явной причины контракты путей и форматов из `Controllers/EnvironmentController.cs`: `Assets/Import`, `Assets/Export`, `Assets/Storage`, `Assets/Saves`, а также `.dmi`, `.csv`, `.json`.
- Для крупных архитектурных рефакторингов дополнительно читать [AGENTS/REFACTOR_GUIDANCE.md](./AGENTS/REFACTOR_GUIDANCE.md). Этот документ применяется именно к большим миграциям и не должен навязывать тяжёлый процесс для маленьких локальных правок.
- Для C# и XAML правок минимальная проверка по умолчанию: `dotnet build AdaptiveSpritesDMItool.sln`. Для docs-only изменений минимум `git diff --check`.
- Не использовать деструктивные git-команды без прямого запроса пользователя.

## Маршрутизация
- Агентная база знаний: [AGENTS/](./AGENTS/README.md)
- Локальный task-state и сырые артефакты: [AGENTS/local/README.md](./AGENTS/local/README.md)
- Крупные рефакторинги и миграции: [AGENTS/REFACTOR_GUIDANCE.md](./AGENTS/REFACTOR_GUIDANCE.md)
- Продуктовые документы: `README*.md`, `AdaptiveSpritesDMItool.sln`, `src/AdaptiveSpritesDmiTool.*`, `tests/AdaptiveSpritesDmiTool.*`, `docs/**`
