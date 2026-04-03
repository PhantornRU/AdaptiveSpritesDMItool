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
3. Локальный task-state для текущей задачи:
   - [AGENTS/local/README.md](./AGENTS/local/README.md)
   - `AGENTS/local/PLAN.md`
   - `AGENTS/local/TODO.md`
   - `AGENTS/local/DECISIONS.md`
   - `AGENTS/local/EVIDENCE.md`
   - `AGENTS/local/logs/`
4. Затем только релевантные документы и код проекта:
   - [README.md](./README.md)
   - [README-ru.md](./README-ru.md)
   - [AdaptiveSpritesDMItool.csproj](./AdaptiveSpritesDMItool.csproj)
   - [App.xaml](./App.xaml)
   - [App.xaml.cs](./App.xaml.cs)
   - `Views/**`, `ViewModels/**`, `Controllers/**`, `Models/**`, `Processors/**`, `Helpers/**`, `Services/**`, `Resources/**`
   - `Assets/Storage/*.csv` и `Assets/Import/*.dmi`, если задача затрагивает конфиги, импорт или экспорт

Если локальные `PLAN/TODO/DECISIONS/EVIDENCE` отсутствуют или не относятся к текущей задаче, ориентироваться только на stable guidance и затем на релевантные файлы проекта.

## Жесткие правила
- Перед правками собрать контекст: entrypoints, callsites, data flow, side effects. В этом проекте много глобального состояния в статических контроллерах, поэтому локальная правка легко дает побочный эффект в соседней странице.
- Для поиска использовать `rg` и точечное чтение файлов, а не широкое сканирование дерева.
- Сохранять текущие слои ответственности: UI и bindings в `Views/**` и `ViewModels/**`, общее runtime-состояние в `Controllers/**` и `Resources/**`, обработку файлов и изображений в `Processors/**` и `Helpers/**`.
- При добавлении новой страницы, сервиса или модели навигации проверять регистрацию в `App.xaml.cs` и разрешение через `Services/PageService.cs`.
- Не менять без явной причины контракты путей и форматов из `Controllers/EnvironmentController.cs`: `Assets/Import`, `Assets/Export`, `Assets/Storage`, `Assets/Saves`, а также `.dmi`, `.csv`, `.json`.
- Для крупных архитектурных рефакторингов дополнительно читать [AGENTS/REFACTOR_GUIDANCE.md](./AGENTS/REFACTOR_GUIDANCE.md). Этот документ применяется именно к крупным миграциям и не должен навязывать тяжелый процесс для маленьких локальных правок.
- Для C# и XAML правок минимальная проверка по умолчанию: `dotnet build AdaptiveSpritesDMItool.sln`. Для docs-only изменений минимум `git diff --check`.
- Не использовать деструктивные git-команды без прямого запроса пользователя.

## Маршрутизация
- Агентная база знаний: [AGENTS/](./AGENTS/README.md)
- Локальный task-state и сырые артефакты: [AGENTS/local/README.md](./AGENTS/local/README.md)
- Крупные рефакторинги и миграции: [AGENTS/REFACTOR_GUIDANCE.md](./AGENTS/REFACTOR_GUIDANCE.md)
- Продуктовые документы: `README*.md`, `AdaptiveSpritesDMItool.csproj`, `App.xaml*`, `Views/**`, `ViewModels/**`, `Controllers/**`, `Models/**`, `Processors/**`, `Helpers/**`, `Services/**`, `Resources/**`
