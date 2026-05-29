# Test Plan v2.2

## Strategy

Проверка v2.2 строится на трех уровнях:

- unit tests для Domain invariants, Application use cases и WPF shell view models;
- integration tests для JSON persistence, CSV import, DMI adapters, preview, settings и batch behavior;
- manual smoke checks для реального WPF UI и release ZIP.

## Automated Coverage

### Unit

Покрываются:

- `SpriteConfig`, `PixelCoordinate`, `SpriteResolution`, `SupportedDirectionSet`
- config validation и compatibility checks
- empty workspace lifecycle
- create/load/save/import use cases
- preview-selection normalization
- selected direction state
- mapping operations
- undo/redo и grouped editor mutations
- workspace-settings load/save use cases
- imported DMI state layer assignment, order, opacity, and restore behavior
- WPF shell smoke checks
- editor tools and viewport state
- config queue behavior
- batch workspace view-model state

### Integration

Покрываются:

- JSON config repository roundtrip
- unsupported JSON config version
- JSON validation failures
- CSV import success and malformed-input failures
- DMI load and direction detection
- empty or invalid DMI rejection
- preview extraction with optional landmark/overlay
- DMI writer apply/save for `4-dir` and `8-dir`
- deterministic batch end-to-end
- overwrite/skip behavior
- stable input/output reporting
- workspace settings repository roundtrip and version validation
- imported state workspace settings validation
- batch manifest validation and artifacts behavior

## v2.2 Release Validation

Последняя release-проверка v2.2 прошла:

- hidden Unicode scan
- `dotnet restore`
- `dotnet build` in Release configuration
- `dotnet test` in Release configuration
- 127 unit tests
- 46 integration tests
- self-contained Windows x64 publish
- ZIP packaging
- samples ZIP packaging
- ZIP smoke expand
- samples ZIP smoke expand
- executable existence check

## Regression Matrix

Обязательные сценарии:

1. startup into an empty workspace
2. load a valid `.dmi`
3. reject invalid or empty `.dmi`
4. create a new config
5. save and load JSON config roundtrip
6. import CSV config
7. reject malformed CSV
8. apply config to `4-dir` DMI
9. apply config to `8-dir` DMI
10. reject config with incompatible resolution
11. reject config with incompatible direction set
12. preview base state
13. preview with optional landmark and overlay
14. preview composite/grid/text-grid modes
15. edit mapping with paint/fill/move/erase tools
16. undo and redo sequence
17. area undo behavior
18. transparent output pixel behavior
19. batch processing with `SkipExisting`
20. batch processing with `OverwriteExisting`
21. batch processing with `FailIfExists`
22. batch cancellation
23. deterministic batch ordering
24. output folder excluded from input enumeration when nested
25. workspace settings persist across restart
26. imported DMI states restore across restart
27. imported DMI layer order and opacity affect Source/Editable composition

## Manual Smoke

Run after large presentation, release, or packaging changes:

1. Launch the app from the published folder.
2. Confirm the shell opens with an empty workspace.
3. Open a `.dmi` file manually.
4. Create a config.
5. Select base, landmark, and overlay states.
6. Edit several mappings in source/editable panes.
7. Verify preview modes: composite, base, landmark, overlay, grid, text-grid.
8. Save JSON.
9. Reload JSON and verify the same config appears.
10. Import a CSV and save it as JSON.
11. Run batch processing on a small copied input folder.
12. Verify processed/skipped/failed counts and output files.
13. Restart the app and verify recent settings were restored.
14. Add imported DMI state layers, adjust order and opacity, restart, and verify layer settings were restored.

## Validation Commands

Developer validation:

```powershell
dotnet restore AdaptiveSpritesDMItool.sln -m:1
dotnet build AdaptiveSpritesDMItool.sln -c Release -m:1 -v minimal --no-restore
dotnet test AdaptiveSpritesDMItool.sln -c Release -m:1 -v minimal --no-build
```

Release validation:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./eng/build-release.ps1 -Version v2.2 -Runtime win-x64
```

Docs-only validation:

```powershell
git diff --check
powershell -NoProfile -ExecutionPolicy Bypass -File ./eng/check-hidden-unicode.ps1
```
