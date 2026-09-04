# TASK-0001-A — Foundation Architecture Handoff

## Status
APPROVED FOR IMPLEMENTATION

## Decision
Use the application-facing boundary defined in `ADR-0001-wpf-application-boundary.md`.

## First Implementation Slice
The first production implementation should be deliberately small:

```text
PulseInspector.Wpf
    ├── App
    ├── Views
    └── ViewModels

PulseInspector.Application
    ├── Contracts
    └── Services
```

The exact project/namespace names may be adjusted to match the existing solution conventions, but the dependency direction must remain:

```text
WPF → Application → Existing Services/Models
```

and never:

```text
Existing Services → WPF
```

## First ViewModel Boundary
Create a shell-level ViewModel first. It should expose presentation state and commands, not feature-calculation algorithms.

Initial state categories:
- current group selection
- current subgroup selection
- current waveform presentation state
- feature presentation state
- inspection/training status
- user-facing error/status messages

Initial commands should be limited to the workflow needed by the first screen. Do not expose the entire legacy service surface merely because it exists.

## Waveform Integration
Use `WindowsFormsHost` for `WaveformControl` in the first WPF shell. The hosted control is an integration dependency, not the target architecture.

## Implementation Gate
Before implementation:
- confirm the existing solution builds
- identify exact project target frameworks and references
- identify whether the current solution can add WPF without disrupting WinForms
- define test project references

## Required Handoff to WPF Developer
1. Read `.ai/project.md`.
2. Read `.ai/architecture.md` and ADR-0001.
3. Read `.ai/migration-rules.md`.
4. Read `.ai/analysis/dependency-map.md`, `service-map.md`, `control-map.md`, and `event-map.md`.
5. Implement only the first shell/boundary slice.
6. Keep legacy production behavior unchanged.
7. Provide build/test evidence and changed-file inventory.

## Human Gate
This architecture is the approved plan for implementation. Any change to the dependency direction, protected behavior, or migration strategy requires a new architecture decision/review.
