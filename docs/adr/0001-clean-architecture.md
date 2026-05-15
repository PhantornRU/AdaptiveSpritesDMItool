# ADR 0001: Clean Architecture

## Status

Accepted

## Context

The current repository mixes WPF controls, global state, preview rendering, batch processing, and config persistence in static controllers and helper classes.

## Decision

Adopt a layered architecture with `Domain`, `Application`, `Infrastructure`, and `Presentation.Wpf`.

## Consequences

- Domain logic becomes testable without WPF or DMISharp.
- Batch processing and config persistence move out of code-behind and static helpers.
- Legacy controllers may coexist temporarily during migration but are not part of the final runtime architecture.