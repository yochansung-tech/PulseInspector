# TASK-0002 — MainForm State & Workflow Extraction

**Status:** VERIFIED — CORE EXTRACTION IN PROGRESS
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
6. `PulseInspector.Core` dependency boundary introduced for Models/Services.

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
- Application facade delegates to domain services: **PASS by source review**.
- Group/subgroup selection maps to existing records: **PASS by source review**.
- Protected numerical/domain logic was not rewritten: **PASS by diff scope review**.
- WPF/Application/Core Release build in GitHub Actions: **PASS**.
- Existing algorithm regression suite: **PASS**.
- Hosted WaveformControl disposal: **N/A** after native WPF waveform migration.
- Broad legacy UI migration: **not completed; intentionally deferred**.

## Remaining verification
1. Manual Windows verification of selection, inspection, defective state, settings invalidation, sorting, chart rendering, keyboard focus, accessibility, and DPI behavior.
2. Physical relocation of Core-owned source files from legacy paths.
3. Automated dependency-boundary checks.
4. Retirement of remaining WinForms Forms/Controls after WPF parity.

## Hard stops
Protected service behavior, CSV contracts, feature ordering, and numerical algorithms must not be changed as part of UI migration without a separate architecture/algorithm decision.
