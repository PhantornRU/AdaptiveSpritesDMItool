# План реализации v2.4.0

## Scope

V2.4 добавляет формат-независимый документный слой, нативную работу с DMI и импорт/экспорт PNG. Остальные растровые кодеки, Aseprite, SVG, PSD, KRA, XCF и ZIP остаются вне этого PR.

Существующие публичные контракты mapping JSON v2, CSV import и `IDmiWriter` не меняются. DMI и PNG определяются по содержимому, а не только по расширению.

## Challenge block

Подтвержденные сомнения:

1. `.dmi` является PNG-контейнером с DMI metadata, поэтому extension-only routing ошибочно. Текущие file dialogs, shell и batch действительно фильтруют `.dmi` по расширению.
2. Плоский PNG не содержит state, direction и animation metadata. Текущий `DmiAssetInfo` допускает только 4/8 directions и не может честно представить 1-direction raster source.
3. Imported layers хранят DMI path и state name, а batch напрямую вызывает `IDmiWriter`. Добавление PNG-веток в эти классы размножит format-specific условия в Presentation и нарушит layer boundaries.
4. Полное хранение всех RGBA кадров в памяти упростило бы export, но конфликтует с лимитами декодированного объема и relink внешних источников.

Рассмотренные альтернативы:

- расширить `DmiAssetInfo` и добавить проверки расширений в ViewModel: отклонено из-за невозможности выразить 1-dir документы и неизбежного дублирования codec logic;
- хранить `SpriteDocument` как полностью materialized RGBA graph: отклонено из-за потребления памяти и отсутствия проверки изменения внешних источников;
- использовать source-referenced `SpriteDocument` и отдельные probe/import/frame/export adapters: принято. Модель остается чистой, кадры декодируются по запросу, а sidecar фиксирует provenance и slicing recipe.

## Workstreams

1. `architecture`: документная модель, codec routing, sidecar и managed export contracts.
2. `domain/application`: `SpriteDocument`, 1/4/8 direction depth, source references, slicing validation, use cases.
3. `infrastructure`: content probe, DMI/PNG importers, frame source, sidecar repository, PNG exporters.
4. `workspace/batch`: Workspace schema v8, generic auxiliary layers, output format profiles, isolated per-file failures.
5. `wpf-ui`: Document workspace, explicit Open DMI / Import graphics actions, sprite-sheet dialog and grid preview.
6. `test/docs`: security limits, migrations, content mismatch, round trips, batch matrix and manual smoke.

## Invariants

- Mapping config remains JSON schema v2 and supports only the existing 4/8 direction sets.
- Raster document states use direction depth 1, 4 or 8; depth 1 maps to South when a mapping config is applied.
- A 4-direction config may be applied to raster export profiles with 1 or 4 directions. An 8-direction raster profile requires an 8-direction config.
- `IDmiWriter` remains the only implementation used for safe mutation of an existing DMI.
- Batch consumes already calculated mappings and does not reinterpret editor mirror settings.
- No implicit bilinear scaling. Default import crops or pads without resize; an explicit resize uses nearest-neighbor.
- Decode is preceded by identify/probe and checked against all safety limits.

## Safety limits

- encoded source size: 128 MiB;
- width or height: 16,384 pixels;
- total decoded pixels: 67,108,864 pixels (256 MiB RGBA);
- frames or sliced cells: 4,096;
- states: 1,024.

Limits are validated with checked arithmetic before allocation. Cancellation and validation failures do not mutate the active document, Workspace, sidecar or export destination.

## Persistence

- Sidecar name: `<name>.adaptive-dmi.json`, schema v1.
- External sources store a project-relative path, absolute fallback, detected format, dimensions, encoded size and SHA-256.
- Frame references store crop, direction/frame placement and explicit transforms.
- A changed or missing source requires relink, accept-new-fingerprint or cancel; it is never accepted silently.
- Raster export writes a tool-managed directory through a sibling staging directory. Existing directories are replaced only when a valid ownership manifest identifies them as tool-owned.
- Workspace schema v8 stores the last document/sidecar, generic auxiliary layers, `Dmi`/`Png` batch selections and raster export profiles. V7 migrates with DMI and PNG selected by default.

## PR sequence inside v2.4

1. contracts and migrations;
2. DMI/PNG codecs and sidecar persistence;
3. Workspace and batch;
4. WPF document/import experience;
5. regression, package and documentation closeout.

## Acceptance

- content probing handles valid DMI, DMI-with-PNG-extension, ordinary PNG-with-DMI-extension, renamed PNG and corrupt input;
- PNG single-image, multi-file sequence and sprite-sheet recipes create valid documents without UI-thread decode;
- DMI import preserves state order, direction depth, frame count and animation metadata;
- sidecar roundtrip is deterministic and detects changed sources;
- PNG sheet/sequence export is deterministic and tool-owned output replacement is atomic;
- Workspace v7 migrates to v8 without losing V2.3 editor settings;
- batch runs DMI and PNG outputs independently and a PNG profile mismatch fails only that file/output;
- all existing V2.3 tests remain green and new V2.4 unit/integration tests pass in Debug and Release.
