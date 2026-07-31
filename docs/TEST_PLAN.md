# Test Plan v2.3.0

## Strategy

Проверка v2.3.0 строится на трех уровнях:

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
- exact 4/8-direction projection matrix, mirror parity, offsets and OOB skips
- interpolated atomic strokes, conflict rejection and semantic no-op history
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
- DMI state order, metadata/RGBA verification, in-place save and atomic failure safety
- asymmetric `3x3`/`4x4` fixtures with unique corners, multiple states, frames and directions
- independent mapping/preview/reopened-DMI RGBA SHA-256 comparison
- verification, cancellation and replacement failures preserve the original output hash and remove temporary files
- deterministic batch end-to-end
- overwrite/skip behavior
- stable input/output reporting
- workspace settings repository roundtrip and version validation
- imported state workspace settings validation
- batch manifest validation and artifacts behavior

## v2.3.0 Release Validation

Release-проверка v2.3.0 включает:

- hidden Unicode scan
- locked `dotnet restore`
- `dotnet build` in Release configuration
- `dotnet test` in Release configuration
- 230 unit tests
- 56 integration tests
- NuGet vulnerability audit
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
17. Restore Pixel/Area behavior
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
15. Repeat the editor checks at multiple zoom levels and at Windows display scaling values of 100%, 125%, and 150%.
16. Enable the mirror-axis guide independently of the grid; verify the centered axis and positive/negative offsets on every Source and Editable canvas.
17. Exercise Single, Parallel, and All scopes with Mirror both enabled and disabled, including an offset that produces out-of-bounds projections; verify skipped targets are reported and never clamped to an edge.
18. Draw fast sparse Paint, Erase, and Restore strokes; verify continuity and exactly one Undo step per completed gesture.
19. Verify inclusive Fill bounds, overlapping Move and Select/Move, transparent Erase, and original-pixel Restore.
20. During a gesture, test Escape, tool/state/direction changes, lost mouse capture, and mouse release outside the canvas; verify cancellation or completion at the last valid coordinate as appropriate.
21. Save the same asymmetric synthetic DMI both to a new path and in place, then compare preview pixels with the reopened DMI.
22. Open the saved DMI in a third-party BYOND/DMI tool and verify state order, animation frames, directions, metadata, transparency, and unique corner pixels.
23. After any WPF-UI dependency update, repeat the full toolbar, dialogs, themes, DPI, canvas input, and window-resize smoke before accepting the package.

## Validation Commands

Developer validation:

```powershell
dotnet restore AdaptiveSpritesDMItool.sln --locked-mode -m:1
dotnet build AdaptiveSpritesDMItool.sln -c Release -m:1 -v minimal --no-restore
dotnet test AdaptiveSpritesDMItool.sln -c Release -m:1 -v minimal --no-build
```

Release validation:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File ./eng/build-release.ps1 -Version v2.3.0 -Runtime win-x64
```

Docs-only validation:

```powershell
git diff --check
powershell -NoProfile -ExecutionPolicy Bypass -File ./eng/check-hidden-unicode.ps1
```
