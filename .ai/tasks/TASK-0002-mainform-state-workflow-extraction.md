# TASK-0002 — MainForm State & Workflow Extraction

**Status:** VERIFIED — WPF WORKFLOW SLICE COMPLETE / LEGACY RETIREMENT NEXT
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
6. `PulseInspector.Core` dependency boundary introduced for Models/Services and physically relocated.
7. CSV inspection-result export exposed through `IInspectionApplication` and consumed by WPF without duplicating exporter logic.
8. WPF file open/save dialogs remain in the WPF view layer as UI infrastructure; parsing, feature extraction, inspection, and export remain behind Application/Core boundaries.

## Explicitly preserved
- Baseline estimation/removal.
- Positive-current charge integration.
- Six feature definitions.
- Deterministic feature ordering.
- Group/subgroup inspection semantics.
- Mahalanobis calculation and chi-square threshold.
- CSV compatibility and stable export column ordering.

## Acceptance status
- WPF workflow commands do not calculate domain features or inspection statistics: **PASS by source review**.
- Application facade delegates to domain services: **PASS by source review**.
- Group/subgroup selection maps to existing records: **PASS by source review**.
- Protected numerical/domain logic was not rewritten: **PASS by diff scope review**.
- WPF/Application/Core Release build in GitHub Actions: **PASS** for commit `9e0429d2ff3d2737ea3a4c6ce6116ff0b55674fa`.
- Existing algorithm regression suite: **PASS** for commit `9e0429d2ff3d2737ea3a4c6ce6116ff0b55674fa`.
- Application export boundary: **IMPLEMENTED**; latest CI verification is pending after the current export/UI changes.
- Hosted WaveformControl disposal: **N/A** after native WPF waveform migration.
- Broad legacy UI migration: **not completed; intentionally deferred until WPF parity is manually verified**.

## Legacy retirement assessment
The legacy WinForms project is still required by the current compatibility smoke test and still contains `MainForm` plus its Forms/Controls UI shell. The application layer does not reference the legacy WinForms assembly. Therefore the safe next step is not to delete the legacy project yet; it is to migrate remaining user-visible workflows, then remove the smoke-test dependency and retire the shell in a separate, reviewable change.

## Remaining verification
1. Manual Windows verification of selection, inspection, defective state, settings invalidation, sorting, chart rendering, keyboard focus, accessibility, and DPI behavior.
2. Final CI verification of the current WPF export slice.
3. Migration of any remaining user-visible WinForms-only workflow that has no WPF equivalent.
4. Remove `WinFormsSmokeTest` dependency after parity is accepted.
5. Retire `PulseInspector` Forms/Controls and its WinForms executable in a separate change.

## Hard stops
Protected service behavior, CSV contracts, feature ordering, and numerical algorithms must not be changed as part of UI migration without a separate architecture/algorithm decision.
