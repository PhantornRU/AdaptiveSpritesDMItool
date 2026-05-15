# ADR 0002: JSON Config Format

## Status

Accepted

## Context

Legacy CSV does not support schema versioning, metadata, or strict validation.

## Decision

Use versioned JSON as the primary config format. Keep CSV only as an import path.

## Consequences

- configs can carry metadata and migration provenance
- schema evolution becomes explicit
- strict validation is possible before applying configs