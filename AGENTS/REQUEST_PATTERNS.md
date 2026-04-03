# Request Patterns

## Пересмотр плана
```text
Пересмотри план для AdaptiveSpritesDMItool:
- Сначала проверь реализацию через targeted search, bindings, callsites и data flow.
- Отдельно оцени влияние на глобальное состояние в Controllers/** и на соседние страницы.
- Если задача про UI, проверь View, ViewModel, code-behind и регистрацию в App.xaml.cs.
- Если задача про import/export или настройки, проверь EnvironmentController, FilesSearcher, SettingsManager и связанные модели.
- Сформируй challenge-block: минимум 3 сомнения, минимум 2 альтернативы, причины выбора и отказа.
- Укажи acceptance criteria и конкретные команды проверки, обычно начиная с dotnet build AdaptiveSpritesDMItool.sln.
- Ничего не реализуй до утверждения плана.
```

## Реализация
```text
PLEASE IMPLEMENT THIS PLAN:
- Сошлись на утвержденный challenge-block и подтвержденные допущения.
- Сохрани существующее разделение между Views/ViewModels, Controllers/Resources и Processors/Helpers.
- Не ломай регистрацию страниц и сервисов в App.xaml.cs и PageService.
- Если меняешь глобальное состояние, перечисли затронутые callsites и проверь побочные эффекты.
- Если меняешь import/export или config/save поведение, проверь совместимость путей, форматов и режима Override.
- После изменений прогони релевантные проверки из AGENTS/WORKFLOW_RULES.md.
- В ответе дай:
  1) что изменено,
  2) какие файлы затронуты,
  3) результаты проверок,
  4) какие сомнения подтвердились или не подтвердились,
  5) остаточные риски, если есть.
```

## Аудит после реализации
```text
Пройдись по измененной зоне AdaptiveSpritesDMItool:
- проверь broken bindings, пропущенные DI-регистрации и несовпадение View/ViewModel контрактов,
- проверь неиспользуемый код, лишние зависимости и дубли логики,
- проверь side effects на global-state контроллеры,
- проверь кодировку и ссылки в документации,
- проверь fallout для import/export, settings и batch-processing, если задача их затрагивает,
- предложи правки и, если запрошено, реализуй их с результатами проверок.
```
