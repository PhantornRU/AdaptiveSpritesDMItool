# ADR 0003: Empty Workspace Startup

## Status

Accepted

## Context

The current app auto-loads demo/test DMI files from `Assets/Import`, which breaks clean startup and makes deployment environment-dependent.

## Decision

Start the application with an empty workspace and require explicit user actions to open base, landmark, and overlay DMI files.

## Consequences

- startup no longer depends on demo assets
- missing optional assets no longer cause startup failures
- the UI must provide explicit open/create flows