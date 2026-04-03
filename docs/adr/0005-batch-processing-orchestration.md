# ADR 0005: Batch Processing Orchestration

## Status

Accepted

## Context

Legacy batch processing stores UI controls and progress counters in a static processor and starts work through `Task.Run` without a proper awaitable orchestration path.

## Decision

Move batch orchestration into the Application layer and implement DMI/file processing in Infrastructure. Progress and cancellation are passed through explicit contracts.

## Consequences

- batch jobs become deterministic, awaitable, and cancellable
- per-file results can be reported reliably
- UI progress binding is decoupled from the processing engine