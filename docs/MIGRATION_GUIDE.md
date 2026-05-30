# Переход с CSV на JSON

В v2.2 основной формат конфигов - JSON. CSV оставлен для импорта старых таблиц mapping.

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
