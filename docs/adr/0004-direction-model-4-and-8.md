# ADR 0004: Direction Model For 4-dir And 8-dir

## Status

Accepted

## Context

Legacy config and preview flow assume four directions in critical code paths.

## Decision

Model directions explicitly in the Domain through a domain-owned direction enum and a supported direction set value object. Map DMISharp direction depth only at infrastructure boundaries.

## Consequences

- 4-dir and 8-dir become first-class scenarios
- compatibility validation becomes explicit
- Domain does not depend on DMISharp enums