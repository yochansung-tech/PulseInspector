# TASK-0001-B — WPF Foundation Implementation Handoff

## Status
READY

## Preconditions
- Architecture decision ADR-0001 is accepted.
- Phase 0 governance and analysis artifacts are complete.
- Existing WinForms behavior remains the baseline.

## Implementation Order
1. Verify solution/project target frameworks and references.
2. Create dedicated `ai/phase-1-foundation` from the approved Phase 0 head.
3. Add the WPF/application project structure without modifying protected services.
4. Add the minimal application boundary.
5. Add a minimal WPF shell/ViewModel.
6. Integrate `WaveformControl` through `WindowsFormsHost` only if the project references support it cleanly.
7. Build and test.
8. Stop before broad MainForm/control migration.

## Hard Stop Conditions
Stop and return to Architecture Review if:
- a legacy service must be changed to support the shell;
- a domain calculation must be duplicated or changed;
- existing CSV behavior must change;
- FeatureVector ordering must change;
- the WinForms baseline can no longer build;
- WPF/WinForms interop requires invasive changes outside the approved boundary.

## Deliverable
A small, buildable WPF foundation with an explicit application boundary and no protected-behavior changes.
