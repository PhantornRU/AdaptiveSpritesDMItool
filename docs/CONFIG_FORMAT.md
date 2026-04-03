# Config Format

## Status

Primary format: versioned JSON

Legacy compatibility:

- CSV is supported only as an import path.
- New configs are not written as CSV.

## File Shape

```json
{
  "version": 1,
  "name": "jumpsuit-default",
  "resolution": {
    "width": 32,
    "height": 32
  },
  "directionDepth": 8,
  "supportedDirections": [
    "South",
    "North",
    "East",
    "West",
    "SouthEast",
    "SouthWest",
    "NorthEast",
    "NorthWest"
  ],
  "metadata": {
    "createdUtc": "2026-04-03T00:00:00Z",
    "updatedUtc": "2026-04-03T00:00:00Z",
    "source": "editor",
    "importedFromLegacy": false
  },
  "mappingsByDirection": {
    "South": [
      {
        "source": { "x": 0, "y": 0 },
        "target": { "x": 1, "y": 0 }
      }
    ]
  }
}
```

## Required Fields

- `version`
- `name`
- `resolution.width`
- `resolution.height`
- `directionDepth`
- `supportedDirections`
- `metadata`
- `mappingsByDirection`

## Validation Rules

- `version` must be supported by the repository
- `name` must be non-empty
- `resolution` must be positive
- `supportedDirections` must match `directionDepth`
- mapping coordinates must be in bounds for the config resolution
- mappings cannot duplicate the same source coordinate within a direction
- `target` may be null only when representing transparent/deleted output

## Legacy CSV Import

Legacy CSV rows are interpreted as:

```text
Direction,SourceX,SourceY,TargetX,TargetY
```

Import behavior:

- invalid rows fail validation and return structured errors
- imported configs are marked with `metadata.importedFromLegacy = true`
- imported configs are re-saved as JSON

## Compatibility Rules

- a config can be applied only when resolution matches the target DMI frame size
- a config can be applied only when its direction set is compatible with the target DMI
- compatibility mismatches must be reported as validation errors, not silent skips