# Migration Guide

## Legacy To New Config Migration

The legacy application stores pixel mappings in CSV. The migrated application uses versioned JSON as the primary format.

## Recommended Migration Path

1. Open the migrated application.
2. Use `Import Legacy CSV`.
3. Review validation diagnostics.
4. Save the imported config as JSON.
5. Use the JSON config for future editing and batch processing.

## What Changes

- CSV is no longer the primary authoring format.
- Metadata is now stored with the config.
- Resolution and direction compatibility are validated explicitly.
- Imported legacy configs keep provenance through `metadata.importedFromLegacy`.

## Legacy CSV Caveats

- Legacy CSV has no schema version.
- Legacy CSV has no metadata.
- Legacy CSV may contain out-of-bounds or inconsistent rows that were previously accepted silently.
- The migrated application validates rows strictly and reports errors.

## Workspace Migration

The old app assumed demo assets during startup. The migrated app starts with an empty workspace.

Implications:

- no required `default.dmi`
- no required `testBodyMonkey.dmi`
- no required `testClothingDefaultCoat.dmi`
- users open base DMI, landmark, and overlay explicitly

## Batch Processing Migration

The old processor used shared mutable progress and unsafe async orchestration.

The new processor:

- is awaitable
- accepts cancellation tokens
- reports progress explicitly
- produces per-file results
- uses deterministic file ordering

## Settings Migration

Legacy settings tied directly to static controller state are replaced by repository-backed settings and session models.