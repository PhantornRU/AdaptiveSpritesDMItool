# AGENTS.md

Каноническая точка входа и индекс ownership для Codex и совместимых AI-агентов в этом репозитории.

## Ownership
- Universal workflow и safety: [`.roo/rules/00-global-workflow.md`](.roo/rules/00-global-workflow.md), [`.roo/rules/10-safety.md`](.roo/rules/10-safety.md), [`.roo/rules/20-project-context.md`](.roo/rules/20-project-context.md).
- Canonical project overlay: [`AGENTS/README.md`](AGENTS/README.md) и релевантные документы в [`AGENTS/*.md`](AGENTS/README.md).
- Thin bridge для Zoo project overlay: [`.roo/rules/30-project-overlay.md`](.roo/rules/30-project-overlay.md).

## Порядок чтения
1. [`AGENTS/README.md`](AGENTS/README.md).
2. Stable guidance из [`AGENTS/PROJECT_CONTEXT.md`](AGENTS/PROJECT_CONTEXT.md), [`AGENTS/REFACTOR_GUIDANCE.md`](AGENTS/REFACTOR_GUIDANCE.md), [`AGENTS/CONFIRMED_UNRESOLVED_ERRORS.md`](AGENTS/CONFIRMED_UNRESOLVED_ERRORS.md), [`AGENTS/WORKFLOW_RULES.md`](AGENTS/WORKFLOW_RULES.md), [`AGENTS/POLICIES.md`](AGENTS/POLICIES.md), [`AGENTS/REQUEST_PATTERNS.md`](AGENTS/REQUEST_PATTERNS.md).
3. [`AGENTS/local/README.md`](AGENTS/local/README.md) и локальный task-state только если он существует и относится к текущей задаче.
4. Затем только релевантные product docs и код: [`README.md`](README.md), [`README-ru.md`](README-ru.md), [`AdaptiveSpritesDMItool.sln`](AdaptiveSpritesDMItool.sln), [`docs/**`](docs/ARCHITECTURE.md), [`src/AdaptiveSpritesDmiTool.*`](src/AdaptiveSpritesDmiTool.Application/AdaptiveSpritesDmiTool.Application.csproj), [`tests/AdaptiveSpritesDmiTool.*`](tests/AdaptiveSpritesDmiTool.Tests.Unit/AdaptiveSpritesDmiTool.Tests.Unit.csproj), [`samples/dmi/**`](samples/dmi/README.md) при задачах на import/export/manual smoke.

Если локальные [`AGENTS/local/PLAN.md`](AGENTS/local/README.md), [`AGENTS/local/TODO.md`](AGENTS/local/README.md), [`AGENTS/local/DECISIONS.md`](AGENTS/local/README.md), [`AGENTS/local/EVIDENCE.md`](AGENTS/local/README.md) отсутствуют или не относятся к текущей задаче, ориентироваться только на stable guidance и затем на релевантные файлы проекта.

## Routing
- Агентная база знаний и project overlay hub: [`AGENTS/README.md`](AGENTS/README.md).
- Layer boundaries, protected contracts и project context: [`AGENTS/PROJECT_CONTEXT.md`](AGENTS/PROJECT_CONTEXT.md).
- Project-specific workflow и verification expectations: [`AGENTS/WORKFLOW_RULES.md`](AGENTS/WORKFLOW_RULES.md).
- Крупные рефакторинги и миграции: [`AGENTS/REFACTOR_GUIDANCE.md`](AGENTS/REFACTOR_GUIDANCE.md).
- Локальный task-state и сырые артефакты: [`AGENTS/local/README.md`](AGENTS/local/README.md).

## Notes
- Контракты путей и форматов [`Assets/Import`](Assets/Import), [`Assets/Export`](Assets/Export), [`Assets/Storage`](Assets/Storage), [`Assets/Saves`](Assets/Saves), а также форматы `*.dmi`, `*.csv`, `*.json` считать project-specific и менять только в явном scope; устаревшая ссылка на `Controllers/EnvironmentController.cs` удалена, потому что такого пути в текущем дереве нет.
