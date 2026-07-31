# ADR 0001: Source-referenced SpriteDocument

- Status: accepted
- Date: 2026-07-31
- Target: v2.4.0

## Context

DMI is a PNG with document metadata, while an ordinary PNG has neither states nor directions. The V2.3 runtime exposes DMI-specific assets, frame reads and imported layers, so treating every raster as a DMI would lose semantics and move codec decisions into WPF.

## Decision

Introduce a format-independent `SpriteDocument` in Domain. A document contains canvas size, ordered states, 1/4/8 direction depth, animation metadata and ordered frame references. A frame reference points to an external source plus crop and explicit transforms; RGBA data is loaded through an Application frame-source contract only when required.

Infrastructure owns signature probing, DMI/PNG decoding, sidecar persistence and exporters. Presentation invokes use cases and never depends on ImageSharp or DMISharp.

The public mapping config and `IDmiWriter` contracts remain unchanged. DMI mutation continues through the verified atomic V2.3 writer; new document exporters are separate contracts.

## Consequences

- one-direction PNG sources are represented without weakening the mapping config invariant;
- large projects avoid retaining every decoded frame in managed memory;
- sidecars can detect missing or modified sources through metadata and SHA-256;
- callers must resolve source changes explicitly before rendering or export;
- document exporters must materialize frames through a bounded, cancellable pipeline.

## Rejected alternatives

- Extension-only routing cannot distinguish DMI from ordinary or renamed PNG.
- Extending `DmiAssetInfo` would preserve DMI assumptions in every caller.
- An always-materialized RGBA document would make memory use proportional to all frames and directions.
