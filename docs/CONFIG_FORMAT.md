# Config Format

## Status

Primary format:

- versioned JSON

Compatibility:

- legacy CSV is supported only as an import path
- new configs are not written as CSV

## JSON Shape

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
    "createdUtc": "2026-04-05T00:00:00Z",
    "updatedUtc": "2026-04-05T00:00:00Z",
    "source": "ImportedLegacyCsv",
    "sourceIdentifier": "legacy-config.csv",
    "importedFromLegacy": "legacy-config.csv"
  },
  "mappingsByDirection": {
    "South": [
      {
        "source": { "x": 0, "y": 0 },
        "target": { "x": 1, "y": 0 }
      },
      {
        "source": { "x": 2, "y": 0 },
        "target": null
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
- every mapping source and target must stay within bounds
- `target: null` means transparent output
- config resolution must match the target DMI resolution
- config direction set must match the target DMI direction depth

## Legacy CSV Import

Legacy CSV rows are interpreted as:

```text
Direction,SourceX,SourceY,TargetX,TargetY
```

Behavior:

- invalid rows fail fast with structured validation errors
- imported configs keep provenance in `metadata`
- imported configs should be saved as JSON for future editing and batch processing
