# TASK-0002 — MainForm State & Workflow Extraction

**Status:** READY FOR IMPLEMENTATION
**Phase:** 1
**Branch:** `ai/phase-1-mainform-slice`
**Depends on:** TASK-0001 / ADR-0001

## Objective
Move the first meaningful MainForm workflow behind the WPF application boundary while keeping the existing WinForms application buildable and preserving protected behavior.

## Scope
1. Introduce an application-facing inspection workflow facade that coordinates existing loaders/services.
2. Introduce WPF presentation models and commands for group/subgroup selection and inspection state.
3. Keep `WaveformControl` hosted through `WindowsFormsHost`.
4. Add WPF-native group/subgroup presentation for the pilot slice.
5. Preserve existing WinForms MainForm unchanged except for strictly necessary integration seams.

## Out of scope
- Rewriting waveform rendering.
- Rewriting all WinForms controls.
- Changing signal processing or statistics.
- Changing CSV format/semantics.
- Changing feature ordering.
- Changing training or inspection algorithms.
- Removing MainForm.

## Protected behavior
- Baseline estimation/removal.
- Positive-current charge integration.
- Six feature definitions.
- Deterministic feature ordering.
- Group/subgroup inspection semantics.
- Mahalanobis calculation and chi-square threshold.
- CSV compatibility.

## Acceptance criteria
- WPF workflow commands do not calculate domain features or inspection statistics.
- Application facade delegates to existing services as the source of truth.
- Group and subgroup selection produce the same displayed underlying record/feature/result semantics.
- Existing WinForms project remains untouched in protected logic.
- WPF build and tests are green in GitHub Actions.
- Hosted `WaveformControl` is disposed safely when the WPF window closes.
- No broad control migration is included in this task.

## Verification
1. Compare application facade calls against current MainForm service calls.
2. Run existing automated tests.
3. Add focused tests for command/application state mapping where practical.
4. Manually verify selection, inspection, defective state, settings invalidation, and hybrid waveform lifecycle.

## Hard stops
Stop and return to Architecture if implementation requires changing protected service behavior, duplicating numerical calculations in ViewModels, changing CSV contracts, or replacing the waveform renderer as part of this task.
