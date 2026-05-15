# UI Redesign Brief

## Goal

Completely rethink the current WPF shell so the product feels compact, understandable, and task-oriented instead of behaving like one large operational dashboard.

The redesign must preserve the same business capabilities:

- open DMI manually
- create, load, save, and import configs
- edit pixel mappings
- preview base, landmark, and overlay states
- run deterministic batch processing
- support 4-dir and 8-dir

The redesign should reduce button count, remove duplicated intents, and surface advanced options only when they are needed.

## Current Diagnosis

The current shell mixes four different modes into one screen:

1. onboarding and empty-workspace startup
2. file/config operations
3. editor workflow
4. batch workflow

This is visible in:

- [`MainWindow.xaml`](d:/GitHub/AdaptiveSpritesDMItool/src/AdaptiveSpritesDmiTool.Presentation.Wpf/MainWindow.xaml)
- [`MainWindowViewModel.cs`](d:/GitHub/AdaptiveSpritesDMItool/src/AdaptiveSpritesDmiTool.Presentation.Wpf/MainWindowViewModel.cs)
- [`MainWindowViewModel.State.cs`](d:/GitHub/AdaptiveSpritesDMItool/src/AdaptiveSpritesDmiTool.Presentation.Wpf/MainWindowViewModel.State.cs)
- [`MainWindowViewModel.Commands.cs`](d:/GitHub/AdaptiveSpritesDMItool/src/AdaptiveSpritesDmiTool.Presentation.Wpf/MainWindowViewModel.Commands.cs)
- [`MainWindowViewModel.Commands2.cs`](d:/GitHub/AdaptiveSpritesDMItool/src/AdaptiveSpritesDmiTool.Presentation.Wpf/MainWindowViewModel.Commands2.cs)
- [`MainWindowViewModel.Surface.cs`](d:/GitHub/AdaptiveSpritesDMItool/src/AdaptiveSpritesDmiTool.Presentation.Wpf/MainWindowViewModel.Surface.cs)

### Concrete UX problems

- The top toolbar already acts as a workflow switcher, but the same intents are repeated again in the body.
- File/config forms, editor controls, state explorer, preview, and batch compete for the same attention level.
- Preview occupies a permanent full-height area even when the user is still opening files or configuring batch paths.
- Batch controls are visible all the time, although batch is a separate operational mode.
- Low-level toggles such as grid, overlay, text grid, mirror, and centralization are always visible instead of being contextual.
- `MainWindowViewModel` owns too many modes and too much state at once, so the UI mirrors that overload.

## Design Principles

- one screen, one primary task
- progressive disclosure over always-visible control density
- compact primary actions, advanced options on demand
- direct manipulation in the editor, forms only where forms are natural
- batch as a separate workflow, not a permanent side concern
- preview should support editing, not dominate the shell

## Full Redesign Options

## Option A: Task-Based Tabs

Top-level tabs:

- `Start`
- `Editor`
- `Preview`
- `Batch`

### Strengths

- strongest reduction in cognitive load
- clear answer to “where am I?” and “what can I do here?”
- easiest model to explain and document
- batch stops competing with editor

### Weaknesses

- more tab switching
- expert users may miss simultaneous editor + preview visibility if preview becomes a separate tab

### Best for

- broad user base
- onboarding
- rare or occasional use

## Option B: Editor-First Split Workspace

Main editor layout:

- left: state/config explorer
- center: source/edit canvas
- right: inspector and live preview

Batch moves to its own screen or drawer.

### Strengths

- best fit for heavy manual editing
- maximum space goes to pixels and interaction
- easy to reduce button count sharply
- matches expert workflow well

### Weaknesses

- less friendly for first-time users
- batch-only and import-only scenarios feel secondary
- requires more careful UX design around empty workspace

### Best for

- power users
- frequent pixel editing and preview iteration

## Option C: Progressive Disclosure Single Workspace

Keep one workspace, but only reveal the next relevant section after prerequisites are ready.

Typical step order:

1. open DMI or load config
2. choose states
3. edit mappings
4. preview
5. save or batch

### Strengths

- lowest migration risk
- can reuse more of the current shell
- big UX improvement without full navigation redesign

### Weaknesses

- still one large screen underneath
- high risk of regrowth if discipline fades
- less clean than tabs or editor-first split

### Best for

- short-term UX improvement
- incremental migration

## Option D: Wizard Plus Expert Mode

Startup first asks a single question:

- open DMI
- load JSON config
- import CSV
- run batch

Then the user lands in either an editor workspace or a batch workspace.

Also introduce:

- `Simple` mode
- `Advanced` mode

### Strengths

- friendliest for new users
- significantly reduces first-run intimidation
- naturally supports “fewer buttons, more automation”

### Weaknesses

- can frustrate expert users if overused
- requires careful mode design
- risks feeling too simplified if not balanced well

### Best for

- mixed audiences
- teams where not everyone edits manually

## Recommended Target

Use a hybrid:

- Task-Based Tabs as shell navigation
- Editor-First Split Workspace inside the `Editor` tab
- Start Hub or short wizard for empty-workspace entry
- Progressive Disclosure for advanced toggles and optional controls

## Recommended Information Architecture

## 1. Start Tab

Purpose:

- choose how to begin

Content:

- large cards: `Open DMI`, `Load JSON`, `Import CSV`, `Run Batch`
- compact recent items list
- workspace summary

Must not include:

- editor controls
- preview canvas
- batch result table

## 2. Editor Tab

Purpose:

- manual mapping work

Layout:

- left rail: states and config context
- center: source and editable panes
- right rail: inspector and live preview

Primary controls:

- direction
- tool
- undo
- redo
- save

Advanced controls:

- mirror
- centralize
- grid options
- overlay visibility
- text grid

These should live in an `Editor Options` flyout, inspector panel, or expander instead of staying always visible.

## 3. Batch Tab

Purpose:

- folder-based application of the current config

Content:

- input folder
- output folder
- overwrite policy
- run button
- result table
- progress summary

Batch should not share vertical space with editor panels.

## 4. Optional Preview Tab

Use only if the editor-side preview becomes too constrained.

Alternative:

- keep preview primarily in the editor right rail
- allow expanding it into a dedicated preview tab or detached surface

## Specific Simplifications

- remove duplicated open/load/save intents between toolbar and body forms
- replace always-visible path text boxes with dialog-driven actions plus compact summaries
- keep only 4-6 primary toolbar actions visible at all times
- make preview rebuild automatic on relevant changes, with a manual refresh fallback
- treat batch as a separate mode, not part of the editor scroll stack
- keep status and progress in a thin bottom bar
- move state assignment actions closer to preview and explorer context
- favor presets and view modes over clusters of independent checkboxes

## Recommended ViewModel Split

The current `MainWindowViewModel` should become a shell and composition root, not the product brain.

Suggested split:

- `WorkspaceShellViewModel`
- `StartHubViewModel`
- `EditorWorkspaceViewModel`
- `StateExplorerViewModel`
- `PreviewPanelViewModel`
- `BatchWorkspaceViewModel`
- `StatusBarViewModel`

Potential views:

- `StartHubView.xaml`
- `EditorWorkspaceView.xaml`
- `PreviewPanelView.xaml`
- `BatchWorkspaceView.xaml`
- `EditorOptionsView.xaml`

The low-level pointer adapter can stay in a thin host view, but product logic should remain in the editor-facing view model set.

## Mapping From Current UI To Target UI

- current top toolbar -> compact shell toolbar plus tab navigation
- current `Files And Configs` block -> `Start` tab and compact workspace header
- current `Editor` block -> `Editor` center workspace
- current `State Explorer` block -> left rail of `Editor`
- current preview panel -> right rail of `Editor` or optional `Preview` tab
- current `Batch Processing` block -> dedicated `Batch` tab
- current footer progress area -> slim shared status bar

## Phased Rollout

## Phase 1

- introduce shell tabs
- split `Batch` out of the editor screen
- collapse duplicate file/config actions into a single `Start` surface

## Phase 2

- extract `EditorWorkspaceViewModel`
- move preview into an editor-side inspector
- convert always-visible toggles into advanced options

## Phase 3

- add start hub cards and recent items
- enable automatic preview refresh where safe
- refine simple versus advanced presentation

## Phase 4

- polish visual hierarchy, spacing, typography, and empty states
- add hotkeys and contextual actions for expert flows

## Recommendation Summary

If only one direction is chosen, use:

- `Tabs + Start Hub + Editor Split Workspace`

This gives the strongest reduction in perceived clutter while still matching the tool’s actual workflows and keeping the migration technically manageable.
