# Форматы конфигов v2.4.0

## Статус

Основной формат pixel-mapping конфигов в v2.4.0 остается JSON schema `version: 2`.

CSV можно импортировать, но новые конфиги сохраняются как JSON.

## JSON Config Schema Version 2

Файл JSON сохраняется и загружается через `JsonSpriteConfigRepository`.

Пример:

```json
{
  "version": 2,
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
  "editorSettings": {
    "mirrorAxisOffsetPixels": 0
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

- `version`: поддерживаются `1` и `2`; новые сохранения всегда используют `2`.
- `name`: непустое имя конфига.
- `resolution`: размер sprite frame в пикселях.
- `supportedDirections`: строка `"four"` или `"eight"`.
- `metadata.createdUtc`: дата создания конфига.
- `metadata.updatedUtc`: дата последнего изменения.
- `metadata.source`: `UserCreated`, `Json` или `ImportedLegacyCsv`.
- `metadata.sourceIdentifier`: краткий идентификатор источника, например имя импортированного файла.
- `metadata.importedFromLegacy`: исходный CSV path, если конфиг импортирован.
- `editorSettings.mirrorAxisOffsetPixels`: целочисленное смещение вертикальной оси; допустимо `abs(offset) <= floor((width-1)/2)`.
- `mappings`: объект, где ключ - имя направления, а значение - массив mappings.
- `source`: историческое имя координаты изменяемого выходного пикселя (`Editable` в UI).
- `target`: координата пикселя, читаемого из исходного кадра направления (`Source` в UI), или `null`.

`target: null` означает прозрачный выходной пиксель.

Таким образом, JSON-запись описывает связь `Editable(source) <- Source(target)`. Имена полей
сохраняются для совместимости с JSON v1/v2 и legacy CSV.

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
- `editorSettings.mirrorAxisOffsetPixels` должен попадать в диапазон разрешения.
- legacy direct-DMI pipeline требует совпадения resolution и direction set с целевым sprite asset;
- V2.4 document batch проверяет resolution и выбранный raster profile: one-direction source реплицируется, 4-dir profile принимает 4-dir или 8-dir config, а 8-dir profile требует 8-dir config.

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

## Import Project Sidecar Version 1

Файл `<name>.adaptive-dmi.json` описывает `SpriteDocument` и не заменяет mapping config. Sidecar содержит:

- `version: 1`, canvas и ordered states;
- direction depth `1`, `4` или `8`, animation metadata и frame placement;
- source-relative path и absolute fallback;
- фактически обнаруженный формат, размеры, encoded length и SHA-256;
- crop, nearest-neighbor transform flags и slicing recipe;
- manifest ownership для managed raster exports.

При несовпадении fingerprint пользователь должен выбрать relink, принять новый fingerprint или отменить загрузку. Отсутствующий source допускает только relink или отмену. Решение принимается отдельно для каждого source; автоматическое принятие запрещено. Успешная загрузка после Accept или Relink помечает проект измененным, пока обновленные fingerprint/path не будут сохранены в sidecar.

## Workspace Settings

Workspace settings schema v8 - внутренний JSON приложения. Пользователь обычно не редактирует его вручную.

В settings сохраняются последние пути, выбранные states, auxiliary DMI/PNG layers, selected direction, overwrite policy, language, theme, viewport, `mirrorAxisOffsetPixels`, `showMirrorAxisGuide`, `mirrorAcrossDirections`, batch formats, raster export profile и состояние рабочих панелей.

При загрузке JSON значение оси из конфига главнее Workspace. JSON v1, CSV и старые Workspace получают нулевое смещение; новый конфиг наследует последнее допустимое значение Workspace.

Auxiliary layer settings include source path and format, state name, frame index, Source/Editable assignment, placement mode, order, and opacity percent. These settings are workspace state, not part of the public sprite mapping config schema.

V8 обобщает imported state settings до auxiliary sprite layers и сохраняет последние выбранные batch formats (`Dmi`, `Png`) и raster export profile. Миграция V7 сохраняет все редакторские поля и выбирает DMI + PNG по умолчанию.
