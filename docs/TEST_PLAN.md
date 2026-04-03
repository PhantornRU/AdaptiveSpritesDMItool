# Test Plan

## Strategy

The migration uses three test layers:

- unit tests for Domain invariants and Application orchestration
- integration tests for JSON persistence, legacy CSV import, DMI load/save, and batch processing
- smoke validation for WPF startup and primary user workflows

Current automated focus in this branch:

- empty workspace lifecycle in `EditorSession` and `StartEmptyWorkspaceUseCase`
- config compatibility checks for resolution and direction set mismatches
- JSON repository roundtrip plus malformed JSON, unsupported version, and validation failures
- legacy CSV import success paths for 4-dir and 8-dir plus malformed input and cancellation
- overwrite policy propagation where Application contracts already expose it

## Regression Matrix

Mandatory regression scenarios:

1. startup without demo assets
2. create empty workspace
3. load valid DMI
4. reject invalid or empty DMI
5. save/load JSON config roundtrip
6. import legacy CSV config
7. apply config to 4-dir DMI
8. apply config to 8-dir DMI
9. overwrite policy behavior
10. batch processing progress and cancellation
11. undo/redo sequence
12. validation rejects out-of-bounds mappings
13. config incompatibility by resolution
14. config incompatibility by direction depth
15. no crash when landmark or overlay is missing
16. deterministic save behavior when state count is equal but contents differ

## Unit Test Areas

### Domain

- `SpriteConfig` creation and validation
- `PixelCoordinate` bounds rules
- supported direction set rules
- mapping uniqueness and target rules
- transparent/deleted mapping behavior
- compatibility validation by resolution and direction set

### Application

- `StartEmptyWorkspaceUseCase`
- `CreateConfigUseCase`
- `LoadConfigUseCase`
- `SaveConfigUseCase`
- `ImportLegacyCsvConfigUseCase`
- `LoadDmiFileUseCase`
- `BuildPreviewUseCase`
- `ApplyConfigToDmiBatchUseCase`
- `UndoChangeUseCase`
- `RedoChangeUseCase`
- progress and cancellation propagation
- overwrite policy selection
- UI-friendly error translation

Automated today:

- session reset to empty workspace
- load/import rejection when config is incompatible with the loaded asset
- overwrite policy propagation from use case to batch service contract
- missing active-config guardrails for batch execution

## Integration Test Areas

- JSON config repository roundtrip
- legacy CSV importer validation and migration
- DMI file loading and direction detection
- preview extraction with missing overlay/landmark
- deterministic batch apply ordering
- DMI save replacement when state counts are equal but content differs
- workspace/settings persistence

Automated today:

- JSON roundtrip
- JSON malformed/version/out-of-bounds/invalid-metadata failures
- legacy CSV import for 4-dir and 8-dir
- CSV malformed direction/column/coordinate failures
- CSV cancellation handling

Blocked until stronger hooks exist:

- real DMI adapter tests for valid/invalid `.dmi` files
- preview extraction with missing landmark/overlay images
- batch engine integration tests for overwrite behavior and deterministic save semantics
- startup smoke tests against the WPF shell without demo assets
- workspace/settings persistence integration tests through a concrete repository

## Smoke Validation

Manual smoke checks after major presentation milestones:

1. Start app on a workspace with no `Assets/Import/default.dmi`.
2. Confirm empty workspace shell is shown.
3. Open a DMI file manually.
4. Create a config and edit mappings.
5. Save to JSON and reload it.
6. Import a legacy CSV and save it as JSON.
7. Run batch processing with progress and cancellation.

## Validation Commands

- `dotnet build AdaptiveSpritesDMItool.sln`
- `dotnet test AdaptiveSpritesDMItool.sln`

If a stage affects only docs:

- `git diff --check`