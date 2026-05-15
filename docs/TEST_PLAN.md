# Test Plan

## Strategy

The repository uses three verification layers:

- unit tests for Domain invariants and Application orchestration
- integration tests for persistence, DMI adapters, preview, settings, and batch behavior
- WPF shell smoke tests for startup and workspace lifecycle

## Regression Matrix

Mandatory scenarios:

1. startup into an empty workspace
2. load a valid `.dmi`
3. reject invalid or empty `.dmi`
4. save and load a JSON config roundtrip
5. import legacy CSV config
6. apply config to `4-dir` DMI
7. apply config to `8-dir` DMI
8. overwrite policy behavior
9. batch processing progress and cancellation
10. undo/redo sequence
11. validation rejects out-of-bounds mappings
12. config incompatibility by resolution
13. config incompatibility by direction depth
14. missing landmark or overlay does not crash preview flow
15. deterministic save/batch behavior when equivalent counts still differ in content
16. workspace settings persist across restart

## Automated Coverage

### Unit

- `SpriteConfig`, `PixelCoordinate`, `SpriteResolution`, `SupportedDirectionSet`
- `EditorSession` empty workspace lifecycle
- grouped editor mutations captured as a single undo step
- `StartEmptyWorkspaceUseCase`, `LoadDmiFileUseCase`, `SaveConfigUseCase`
- preview-selection normalization
- workspace-settings load/save use cases
- WPF shell smoke tests for empty startup and settings persistence

### Integration

- JSON config repository roundtrip
- JSON validation failures
- legacy CSV import success and malformed-input failures
- DMI load and direction detection
- empty DMI rejection
- preview extraction with missing landmark/overlay
- DMI writer apply/save for `4-dir` and `8-dir`
- deterministic batch end-to-end
- overwrite/skip behavior and stable input/output reporting
- workspace settings repository roundtrip and version validation

## Manual Smoke

Run these checks after large presentation changes:

1. Launch the app on a clean workspace with no sample assets.
2. Confirm the shell opens in an empty workspace.
3. Open a `.dmi` file manually.
4. Create a config and edit mappings in source/editable panes.
5. Build preview for base, landmark, and overlay selections.
6. Save JSON, reload it, and verify the same config appears.
7. Import a legacy CSV and save it as JSON.
8. Run batch processing and verify progress, cancellation, and per-file results.

## Validation Commands

- `dotnet build AdaptiveSpritesDMItool.sln -m:1 -v minimal`
- `dotnet test AdaptiveSpritesDMItool.sln -m:1 -v minimal`
- `git diff --check` for docs-only changes
