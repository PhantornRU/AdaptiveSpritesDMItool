# Migration Guide

## Legacy To New Config Migration

The legacy application stored mappings in CSV. The migrated application uses versioned JSON as the primary format.

## Recommended Path

1. Open the new WPF application.
2. Start from the empty workspace.
3. Open the target `.dmi`.
4. Use `Import CSV` to load the legacy config.
5. Review the imported mapping in the editor and preview panes.
6. Save the config as JSON.
7. Use the JSON config for future editing and batch processing.

## What Changed

- CSV is no longer the primary authoring format.
- Metadata is stored with each config.
- Resolution and direction compatibility are validated explicitly.
- Workspace/settings are persisted independently from the config file.
- Startup no longer depends on `default.dmi` or any bundled demo/test asset.

## Legacy CSV Caveats

- no schema version
- no metadata
- rows may be malformed or out of bounds
- old behavior could accept invalid input silently
- new behavior validates strictly and returns explicit errors

## Batch Migration

Legacy batch processing used shared mutable progress and unsafe async orchestration.

The new batch path:

- is awaitable
- accepts cancellation tokens
- reports progress explicitly
- produces per-file results
- uses deterministic file ordering
- honors overwrite policy explicitly

## Settings Migration

Legacy settings were tied to static controller state.

The new application stores repository-backed workspace settings, including:

- last opened DMI/config paths
- last imported legacy CSV path
- last draft config name
- last base/landmark/overlay state selection
- last selected direction
- batch input/output directories
- last overwrite policy
