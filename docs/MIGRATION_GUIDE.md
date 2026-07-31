# Миграция данных v2.4.0

В v2.4.0 основной формат mapping-конфигов остается JSON schema v2. CSV оставлен для импорта старых таблиц mapping.

## Workspace v7 -> v8

Workspace v7 открывается автоматически. DMI-specific imported state entries преобразуются в auxiliary DMI layers, все V2.3 mirror/editor settings сохраняются, а набор batch export formats инициализируется значениями DMI + PNG. Mapping JSON при этом не изменяется.

## PNG projects

Импорт PNG создает source-referenced document. Сохраните рядом sidecar `<name>.adaptive-dmi.json`, если проект нужно открыть повторно без повторной настройки нарезки. Переносите sidecar вместе с относительными source-файлами. При изменении или отсутствии source приложение потребует явного relink/accept/cancel.

## JSON v1 -> v2

JSON v1 открывается без ручной конвертации. Для него используется стандартная ось с `mirrorAxisOffsetPixels: 0`. При следующем сохранении файл записывается как v2 и получает:

```json
"editorSettings": {
  "mirrorAxisOffsetPixels": 0
}
```

Workspace v6 также открывается автоматически как v7: смещение оси равно нулю, направляющая выключена, зеркалирование направлений включено. Значение из загруженного JSON имеет приоритет над Workspace; новый конфиг наследует последнее допустимое значение Workspace.

## Как перенести CSV

1. Запустите приложение.
2. Откройте нужный `.dmi`.
3. Импортируйте CSV config.
4. Проверьте mapping в editor и preview.
5. Сохраните config как JSON.
6. Дальше используйте JSON для редактирования и batch processing.

## CSV Format

Каждая строка CSV должна иметь 5 колонок:

```text
Direction,SourceX,SourceY,TargetX,TargetY
```

Пример:

```text
South,0,0,1,0
South,2,0,-1,-1
North,0,0,0,1
```

Правила:

- header row не нужен;
- `TargetX=-1` и `TargetY=-1` означают прозрачный output pixel;
- directions должны образовывать стандартный 4-dir или 8-dir набор;
- coordinates должны быть целыми числами;
- некорректные строки отклоняются с validation error.

## После импорта

Проверьте:

- правильный `.dmi` открыт;
- resolution совпадает с ожидаемым sprite frame;
- direction set определился как `four` или `eight`;
- preview выглядит корректно для нескольких states;
- сохраненный JSON можно заново загрузить.

Подробная схема JSON описана в [CONFIG_FORMAT.md](CONFIG_FORMAT.md).
