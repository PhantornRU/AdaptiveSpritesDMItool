# Refactor Plan

## Challenge Block

### Confirmed facts

- Startup currently hard-depends on demo/test DMI files.
- Config persistence currently depends on CSV and stores no schema version or metadata.
- Preview/edit flow currently depends on static controller state and direct WPF control references.
- Batch processing currently uses shared mutable state and UI controls inside the processor.
- The repository currently builds with warnings and no dedicated test projects.

### Material doubts to resolve during implementation

1. Preview interaction parity:
   The legacy editor exposes a dense set of editing modes. The migration must keep the business capability while avoiding a giant code-behind rewrite.

2. DMI save semantics:
   Replacing transformed states in a DMI file must preserve order and metadata correctly. This requires explicit integration coverage.

3. Direction compatibility edge cases:
   Some configs may be authored against four directions but applied to eight-direction files or vice versa. The compatibility rules must be explicit before migration of batch flow.

### Alternatives considered

1. Big-bang rewrite of the entire repository in one pass
   Rejected because it would hide regressions, break batch processing semantics, and make review impossible.

2. Strangler migration with layer-by-layer replacement and temporary coexistence
   Chosen because it keeps verification checkpoints and allows deletion of legacy paths only after the new path stabilizes.

3. Reusing DMISharp enums and types directly in Domain
   Rejected because that would leak an infrastructure library into the business core and lock tests to the DMISharp model.

4. Defining domain-owned direction and resolution types, then mapping DMISharp at the boundary
   Chosen because it preserves clean architecture and keeps 4-dir/8-dir support explicit.

## Workstream Orchestration Summary

### 1. architecture-agent

Mission:
Design the target architecture, migration steps, dependency rules, ADRs, and legacy-to-new mapping.

Scope:
Architecture decisions, project structure, dependency graph, PR roadmap, definition of done, analyzers/common props.

Files owned:

- `docs/ARCHITECTURE.md`
- `docs/REFACTOR_PLAN.md`
- `docs/adr/*`
- `Directory.Build.props`
- `Directory.Packages.props`
- solution and project restructuring

Files read-only initially:

- all legacy code

Deliverables:

- target solution structure
- dependency rules
- architectural violation inventory
- legacy-to-new mapping
- PR roadmap
- ADRs

Risks:

- over-designing contracts before DMI boundaries are validated
- keeping accidental runtime dependency on old startup flow

Validation:

- solution builds
- architecture docs match committed structure

### 2. domain-agent

Mission:
Create a pure Domain layer for sprite config semantics.

Scope:
Config aggregate, value objects, validation, direction model, empty workspace model.

Files owned:

- `src/AdaptiveSpritesDmiTool.Domain/*`
- `tests/AdaptiveSpritesDmiTool.Tests.Unit/Domain/*`

Files read-only:

- legacy code for behavior extraction
- application and infrastructure contracts once stabilized

Deliverables:

- `SpriteConfig`
- `PixelMapping`
- `PixelCoordinate`
- `SpriteResolution`
- `SupportedDirectionSet`
- `SpriteDirection`
- `ConfigMetadata`
- `ConfigValidationResult`
- compatibility and bounds validation

Risks:

- accidentally encoding UI assumptions into the aggregate
- under-modeling transparent/deleted pixels

Validation:

- unit tests for invariants
- no references to WPF, DMISharp, or file IO

### 3. application-agent

Mission:
Create use cases and orchestration over the Domain.

Scope:
Results/errors, session orchestration, batch orchestration, progress/cancellation, undo/redo, overwrite policy.

Files owned:

- `src/AdaptiveSpritesDmiTool.Application/*`
- `tests/AdaptiveSpritesDmiTool.Tests.Unit/Application/*`

Files read-only:

- domain project
- presentation contracts once introduced
- infrastructure abstractions only as interfaces

Deliverables:

- `Result<T>` and error model
- use cases:
  - `StartEmptyWorkspaceUseCase`
  - `CreateConfigUseCase`
  - `LoadConfigUseCase`
  - `SaveConfigUseCase`
  - `ImportLegacyCsvConfigUseCase`
  - `LoadDmiFileUseCase`
  - `BuildPreviewUseCase`
  - `ApplyConfigToDmiBatchUseCase`
  - `UndoChangeUseCase`
  - `RedoChangeUseCase`
- editor session orchestration
- progress/cancellation contracts
- UI-friendly error translation contract

Risks:

- leaking filesystem concerns into use cases
- hiding async work behind fire-and-forget calls

Validation:

- unit tests with fake repositories and fake DMI gateways
- no WPF references

### 4. infrastructure-agent

Mission:
Implement external dependencies behind application contracts.

Scope:
JSON repository, CSV importer, DMISharp adapters, filesystem, preview extraction, settings, workspace persistence, batch processor.

Files owned:

- `src/AdaptiveSpritesDmiTool.Infrastructure/*`
- `tests/AdaptiveSpritesDmiTool.Tests.Integration/*`

Files read-only:

- domain and application contracts
- presentation project

Deliverables:

- DMI adapters
- JSON config repository
- legacy CSV importer
- path/filesystem services
- logging adapters
- preview/image conversion adapters
- deterministic batch engine
- workspace persistence

Risks:

- preserving legacy save bugs during migration
- hidden ordering instability from filesystem enumeration

Validation:

- integration tests for JSON roundtrip, CSV import, DMI load/save, batch processing

### 5. wpf-ui-agent

Mission:
Build a production-grade MVVM presentation layer with empty-workspace startup.

Scope:
Shell, workspace, editor, batch screen, commands, dialogs, minimal pointer bridge, progress/error UI.

Files owned:

- `src/AdaptiveSpritesDmiTool.Presentation.Wpf/*`

Files read-only:

- application contracts
- domain read models

Deliverables:

- empty workspace shell
- open DMI flow
- config explorer
- preview panes
- batch screen
- toolbar commands
- progress/cancellation UI
- undo/redo UI

Risks:

- overloading view models with imaging logic
- reintroducing control registries through convenience shortcuts

Validation:

- build
- startup smoke test
- manual open/load/save/batch flow

### 6. test-agent

Mission:
Build regression coverage and testing discipline for the migration.

Scope:
Test plan, unit tests, integration tests, startup smoke checklist, regression matrix.

Files owned:

- `tests/*`
- `docs/TEST_PLAN.md`

Files read-only:

- source projects

Deliverables:

- regression matrix
- unit/integration tests for critical flows
- startup smoke checks

Risks:

- missing the legacy bugs that motivated the rewrite

Validation:

- `dotnet test`
- documented smoke checklist for WPF startup and manual preview

### 7. migration-doc-agent

Mission:
Keep public and internal docs aligned with the migrated architecture.

Scope:
README, config format docs, migration guide, license/package metadata notes.

Files owned:

- `README.md`
- `docs/CONFIG_FORMAT.md`
- `docs/MIGRATION_GUIDE.md`
- `docs/adr/*`

Files read-only:

- source code for factual verification

Deliverables:

- updated README
- config format reference
- legacy CSV migration guide
- metadata/license consistency notes

Risks:

- documenting target behavior that the code does not yet implement

Validation:

- docs match current code and build configuration
- `git diff --check`

## Ownership Map

```text
architecture-agent -> docs, ADR, solution structure, shared props
domain-agent -> Domain project + domain unit tests
application-agent -> Application project + application unit tests
infrastructure-agent -> Infrastructure project + integration tests
wpf-ui-agent -> Presentation.Wpf project
test-agent -> test matrix and cross-layer test harness
migration-doc-agent -> README/config/migration docs
```

No workstream owns another workstream's runtime implementation files.

## Dependency Graph

```text
architecture-agent
  -> domain-agent
  -> application-agent
  -> infrastructure-agent
  -> wpf-ui-agent
  -> test-agent
  -> migration-doc-agent

domain-agent
  -> application-agent
  -> infrastructure-agent
  -> wpf-ui-agent
  -> test-agent

application-agent
  -> infrastructure-agent
  -> wpf-ui-agent
  -> test-agent

infrastructure-agent
  -> wpf-ui-agent
  -> test-agent
  -> migration-doc-agent

wpf-ui-agent
  -> test-agent
  -> migration-doc-agent
```

## Parallelization Plan

Can start in parallel after architecture/contracts stabilize:

- domain-agent
- migration-doc-agent for ADR drafts and config/migration doc skeletons
- test-agent for regression matrix and scaffolding

Can start after domain contracts stabilize:

- application-agent

Can start after application abstractions stabilize:

- infrastructure-agent

Can start after application contracts and key infrastructure read models stabilize:

- wpf-ui-agent

Must wait until the new path is proven:

- legacy deletion
- README finalization
- old project removal

## PR Roadmap

1. Foundation + docs
2. Domain extraction
3. Config format redesign
4. Application layer
5. Infrastructure layer
6. Batch processing rewrite
7. WPF editor rewrite
8. Startup and workspace rewrite
9. Undo/Redo
10. 4-dir / 8-dir support completion
11. Docs and metadata cleanup
12. Legacy removal and hardening

## Migration Sequence For This Repository

1. Add docs and ADRs.
2. Introduce shared props and new projects without deleting legacy.
3. Establish domain/application contracts.
4. Add JSON config repository and legacy CSV importer.
5. Add DMI gateway and deterministic batch engine.
6. Add new WPF shell and workspace flow.
7. Switch startup to new presentation/app flow.
8. Port editor and batch screens.
9. Remove static controllers and old pages after replacement is stable.

## Risk Matrix

| Risk | Impact | Mitigation |
| --- | --- | --- |
| DMI behavior changes during save/export | corrupt output sprites | integration tests over real DMI samples and temp directories |
| Hidden dependency on demo assets | app fails on clean start | empty-workspace startup test and removal of auto-load path |
| Preview/editor mismatch after refactor | config authoring regression | application preview contracts + smoke validation |
| 4-dir/8-dir incompatibility | incorrect mapping or data loss | explicit direction-set model and compatibility validation |
| Async cancellation/progress regressions | hung UI or partial processing | awaitable batch use case and cancellation-token tests |
| Metadata/license inconsistency remains | packaging/docs drift | final docs audit and csproj cleanup |

## Definition Of Done

1. The app starts without demo assets.
2. The startup state is an empty workspace.
3. A DMI file can be opened manually.
4. Configs save/load as JSON.
5. Legacy CSV configs import into the JSON model.
6. Batch processing is deterministic, awaitable, and cancellable.
7. 4-dir and 8-dir are first-class supported scenarios.
8. Undo and redo work.
9. Domain and application flows are covered by tests.
10. UI code-behind no longer owns business logic.
11. Static legacy controllers are removed from the final runtime path.
12. README, LICENSE, and package metadata no longer contradict one another.