# TASK-0002 — MainForm State & Workflow Extraction

**Status:** IMPLEMENTED — VERIFICATION PENDING
**Phase:** 1
**Branch:** `ai/phase-1-mainform-slice`
**Depends on:** TASK-0001 / ADR-0001

## Objective
Move the first meaningful MainForm workflow behind the WPF application boundary while preserving protected signal/statistical behavior.

## Implemented scope
1. Application-facing inspection workflow facade coordinating existing loaders/services.
2. WPF presentation models and commands for group/subgroup selection and inspection state.
3. Native WPF waveform rendering; the temporary WindowsFormsHost seam has been removed from the WPF project.
4. WPF-native group/subgroup presentation.
5. WPF settings, training validation, about, histogram, scatter, sorting, theme resources, and primary accessibility metadata.

## Explicitly preserved
- Baseline estimation/removal.
- Positive-current charge integration.
- Six feature definitions.
- Deterministic feature ordering.
- Group/subgroup inspection semantics.
- Mahalanobis calculation and chi-square threshold.
- CSV compatibility.

## Acceptance status
- WPF workflow commands do not calculate domain features or inspection statistics: **PASS by source review**.
- Application facade delegates to existing services: **PASS by source review**.
- Group/subgroup selection maps to existing records: **PASS by source review**.
- Protected WinForms/domain logic was not rewritten: **PASS by diff scope review**.
- WPF build/tests in GitHub Actions: **PENDING**.
- Hosted WaveformControl disposal: **N/A** after native WPF waveform migration.
- Broad legacy UI migration: **not completed; intentionally deferred**.

## Verification still required
1. Observe a successful GitHub Actions build for the current head.
2. Run the existing automated regression suite.
3. Add WPF/application-boundary focused tests where practical.
4. Manually verify selection, inspection, defective state, settings invalidation, sorting, chart rendering, keyboard focus, and DPI behavior on Windows.
5. Complete Core extraction and remove the remaining WinForms project only after dependency cleanup.

## Hard stops
Protected service behavior, CSV contracts, feature ordering, and numerical algorithms must not be changed as part of UI migration without a separate architecture/algorithm decision.
