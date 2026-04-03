# Workflow Rules

## Discovery first
- Перед анализом большой зоны сначала сузить область через `rg`.
- Базовый порядок:
  1. найти entrypoints, XAML bindings, команды, callsites и связанные модели;
  2. проверить, где состояние хранится глобально, а где локально во ViewModel;
  3. понять side effects на импорт/экспорт, сохранения и превью;
  4. только после этого планировать правки и точечно менять файлы.
- Для UI-задач сначала читать связку `View + ViewModel + code-behind + controller/service`.
- Для задач по обработке DMI сначала читать `EnvironmentController`, нужный `ViewModel`, `DataImageState`/`DataPixelStorage`, затем `DrawController` или `DMIStatesProcessor`.
- Для задач по путям, конфигам и сохранению сначала смотреть `EnvironmentController`, `FilesSearcher`, `SettingsManager`, `Models/EnvironmentItem.cs`, `Models/AppConfig.cs`.
- Не читать крупные директории целиком, если задачу можно сузить выборочными запросами.

## Read-only и mutating границы
- Read-only действия: поиск, чтение, diff, анализ bindings/callsites/data flow, dry-run проверки без изменения tracked файлов.
- Mutating действия: редактирование файлов, форматирование с rewrite, генерация артефактов, команды с целевым изменением репозитория.
- Не смешивать exploratory шаги и реализацию без явного понимания, что уже подтверждено фактами.

## Правила выполнения задач
- Сначала определить тип задачи: docs, XAML/UI, ViewModel/controller, DMI processing, settings/path handling, build или смешанный scope.
- Для нетривиальных изменений сначала формировать decision-complete план с рисками, альтернативами и acceptance criteria.
- Перед правками global-state классов отдельно проверять влияние на другие страницы и режимы.
- Перед добавлением новой страницы или сервиса проверять регистрацию в `App.xaml.cs` и связность навигации через `PageService`.
- Перед изменением путей сохранения и экспорта отдельно проверять контракт `FilesSearcher.GetExportConfigPath(...)` и поля `EnvironmentController.lastImportPath/lastExportPath`.

## Минимальные проверки по типам задач
1. Docs-only:
   - проверить ссылки;
   - проверить UTF-8 и отсутствие mojibake;
   - `git diff --check`.
2. C# или XAML changes:
   - `dotnet build AdaptiveSpritesDMItool.sln`
3. Поведенческие изменения в import/export, настройках или превью:
   - `dotnet build AdaptiveSpritesDMItool.sln`
   - по возможности ручной smoke test на `Assets/Import/*.dmi` и `Assets/Storage/*.csv`
   - отдельно проверить, что пути импорта/экспорта и режим `Override` не поломаны

## Кодировка
- Все новые документы и правки держать в UTF-8.
- Нельзя оставлять mojibake (`Р...`, `�`, `????`) в коде и документации.
- Русскоязычные документы должны читаться в обычном UTF-8 просмотре.
