# TASK-0002 — MainForm State & Workflow Extraction

**Status:** RETIRED — WPF APPLICATION BOUNDARY ESTABLISHED / LEGACY WINFORMS SHELL REMOVED
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
9. Legacy WinForms UI smoke coverage was retired after the WPF workflow became the supported application UI.
10. The legacy `PulseInspector` WinForms executable/project was removed from the solution and its project reference was removed from tests.

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
- WPF/Application/Core Release build and algorithm regression: **previously PASS** for commit `9e0429d2ff3d2737ea3a4c6ce6116ff0b55674fa`.
- Application export boundary: **IMPLEMENTED**; export regression coverage is present in `PulseInspector.Tests`.
- Hosted WaveformControl disposal: **N/A** after native WPF waveform migration.
- Legacy WinForms smoke dependency: **REMOVED**.
- Legacy WinForms solution project/executable: **REMOVED**.

## Verification state
The latest retirement commits were created directly on the migration branch. GitHub Actions status for the newest head must be treated as **pending until a workflow run is observed**; no CI success is claimed solely from source edits.

## Migration completion gate
The Phase-1 MainForm migration slice is considered source-complete because:
- WPF is the only application UI project in the solution.
- Tests no longer reference the legacy WinForms project.
- Application does not reference the legacy WinForms assembly.
- Core/Application boundaries remain intact.
- Domain algorithms remain outside WPF and were not rewritten during UI retirement.

## Hard stops
Protected service behavior, CSV contracts, feature ordering, and numerical algorithms must not be changed as part of UI migration without a separate architecture/algorithm decision.
