# Форматы конфигов v2.1

## Статус

Основной формат пользовательских конфигов в v2.1 - JSON schema `version: 1`.

CSV можно импортировать, но новые конфиги сохраняются как JSON.

## JSON Config Schema Version 1

Файл JSON сохраняется и загружается через `JsonSpriteConfigRepository`.

Пример:

```json
{
  "version": 1,
  "name": "jumpsuit-default",
  "resolution": {
    "width": 32,
    "height": 32
  },
  "supportedDirections": "eight",
  "metadata": {
    "createdUtc": "2026-04-05T00:00:00+00:00",
    "updatedUtc": "2026-04-05T00:00:00+00:00",
    "source": "UserCreated",
    "sourceIdentifier": null,
    "importedFromLegacy": null
  },
  "mappings": {
    "South": [
      {
        "source": { "x": 0, "y": 0 },
        "target": { "x": 1, "y": 0 }
      },
      {
        "source": { "x": 2, "y": 0 },
        "target": null
      }
    ],
    "North": []
  }
}
```

## Required Fields

- `version`
- `name`
- `resolution.width`
- `resolution.height`
- `supportedDirections`
- `metadata`
- `mappings`

## Field Semantics

- `version`: текущая поддерживаемая версия схемы, сейчас только `1`.
- `name`: непустое имя конфига.
- `resolution`: размер sprite frame в пикселях.
- `supportedDirections`: строка `"four"` или `"eight"`.
- `metadata.createdUtc`: дата создания конфига.
- `metadata.updatedUtc`: дата последнего изменения.
- `metadata.source`: `UserCreated`, `Json` или `ImportedLegacyCsv`.
- `metadata.sourceIdentifier`: краткий идентификатор источника, например имя импортированного файла.
- `metadata.importedFromLegacy`: исходный CSV path, если конфиг импортирован.
- `mappings`: объект, где ключ - имя направления, а значение - массив mappings.
- `source`: координата исходного пикселя.
- `target`: координата целевого пикселя или `null`.

`target: null` означает прозрачный выходной пиксель.

Если `source` и `target` совпадают, runtime рассматривает это как отсутствие пользовательского mapping.

## Direction Values

Для `supportedDirections: "four"` используются:

- `South`
- `North`
- `East`
- `West`

Для `supportedDirections: "eight"` используются:

- `South`
- `North`
- `East`
- `West`
- `SouthEast`
- `SouthWest`
- `NorthEast`
- `NorthWest`

Набор направлений должен быть ровно стандартным 4-dir или 8-dir набором.

## Validation Rules

- `version` должен быть поддерживаемым.
- `name` должен быть непустым.
- `resolution.width` и `resolution.height` должны быть положительными.
- `supportedDirections` должен быть `"four"` или `"eight"`.
- каждый direction key в `mappings` должен входить в `supportedDirections`.
- координаты `source` и `target` должны находиться внутри `resolution`.
- `metadata.updatedUtc` не должен быть раньше `metadata.createdUtc`.
- при применении конфига к `.dmi` resolution и direction set должны совпадать с целевым sprite asset.

## CSV Import

CSV импортируется приложением и затем сохраняется как JSON.

Формат строки:

```text
Direction,SourceX,SourceY,TargetX,TargetY
```

Текущий importer ожидает строки данных без обязательного header. Пустые строки пропускаются.

Пример:

```text
South,0,0,1,0
South,2,0,-1,-1
North,0,0,0,1
```

CSV semantics:

- `Direction` должен быть одним из `SpriteDirection`.
- координаты должны быть integer values.
- `TargetX=-1` и `TargetY=-1` означают transparent output.
- resolution выводится из максимальных координат.
- supported direction set выводится из направлений в CSV и должен совпасть с 4-dir или 8-dir набором.
- invalid rows fail fast с validation error.
- импортированный конфиг получает metadata source `ImportedLegacyCsv`.

После импорта конфиг нужно сохранить как JSON для дальнейшей работы.

## Batch Manifest Version 1

Batch manifest - advanced JSON формат для запуска набора batch jobs через Application layer.

Пример:

```json
{
  "version": 1,
  "outputRoot": "D:\\Sprites\\Export",
  "defaultRunMode": "Incremental",
  "jobs": [
    {
      "jobId": "jumpsuits",
      "title": "Jumpsuits",
      "enabled": true,
      "inputDirectory": "D:\\Sprites\\Input",
      "outputSubdirectory": "jumpsuits",
      "configPath": "D:\\Sprites\\Configs\\jumpsuit-default.json",
      "overwritePolicy": "SkipExisting",
      "explicitFiles": null
    }
  ]
}
```

Supported values:

- `defaultRunMode`: `Incremental` или `RebuildAll`
- `overwritePolicy`: `SkipExisting`, `OverwriteExisting`, `FailIfExists`

Batch artifacts пишутся в output root под `.adaptive-sprites`:

- `processed-journal.json`
- `runs/<runId>.json`
- `runs/<runId>.summary.txt`

## Workspace Settings

Workspace settings - внутренний JSON приложения. Пользователь обычно не редактирует его вручную.

В settings сохраняются последние пути, выбранные states, selected direction, overwrite policy, theme, viewport и состояние рабочих панелей.
