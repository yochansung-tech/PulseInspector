# Phase 1 — WPF Migration Foundation

## Goal
Create a minimal, buildable WPF shell and application boundary while keeping the existing WinForms application as the behavioral baseline.

## Work Packages

### P1-01 — Solution Baseline
- Verify current target frameworks and project references.
- Establish the baseline build/test command and record results.
- No behavior changes.

### P1-02 — Application Boundary
- Add the smallest application-facing contracts/facade required by the first screen.
- Delegate to existing services.
- Do not duplicate domain calculations.

### P1-03 — WPF Shell
- Add WPF application/project structure.
- Establish MVVM shell.
- Establish centralized theme/resource foundation.

### P1-04 — Hybrid Waveform View
- Host existing `WaveformControl` using `WindowsFormsHost`.
- Establish explicit lifecycle/disposal behavior.
- Keep rendering implementation unchanged.

### P1-05 — MainForm Slice
- Move only the first coherent MainForm presentation slice into WPF.
- Preserve group/subgroup selection and status semantics for that slice.

### P1-06 — Verification
- Build solution.
- Run existing tests.
- Run protected-data regression tests.
- Compare legacy and WPF workflow behavior for the migrated slice.

## Exit Criteria
Phase 1 is complete only when the WPF shell is buildable, the first application boundary is in place, the hybrid waveform path works, and regression evidence shows no protected-behavior change.

## Non-Goals
- Full MainForm conversion.
- Full control rewrite.
- Domain refactoring.
- Statistical algorithm changes.
- CSV format changes.
