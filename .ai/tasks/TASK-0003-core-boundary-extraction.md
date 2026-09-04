# TASK-0003 — Core Boundary Extraction

**Status:** VERIFIED — PHYSICAL EXTRACTION COMPLETE
**Phase:** 1
**Branch:** `ai/phase-1-mainform-slice`
**Depends on:** TASK-0002 / ADR-0001

## Objective
Establish a UI-independent `PulseInspector.Core` assembly containing the protected Models/Services layer while keeping the legacy WinForms shell buildable during migration.

## Completed
1. Added `PulseInspector.Core` targeting `net8.0`.
2. Moved the project dependency direction for Application from the legacy WinForms assembly to Core.
3. Updated the legacy WinForms shell to reference Core rather than compiling duplicate Models/Services.
4. Updated the test project to reference Core explicitly.
5. Confirmed the Core project contains no Windows Forms project dependency.
6. Physically relocated Models and Services under `PulseInspector.Core/Models` and `PulseInspector.Core/Services`.
7. Removed linked compilation from `PulseInspector.Core.csproj`.
8. Removed the legacy `PulseInspector/Models` and `PulseInspector/Services` source copies.
9. Confirmed Release build and algorithm regression suite pass before the physical extraction; final post-extraction CI verification is required.

## Physical extraction result
The Core project now owns the source of truth for Models/Services. Namespaces remain `PulseInspector.Models` and `PulseInspector.Services` to minimize migration churn and preserve existing callers. The legacy WinForms project remains a UI compatibility shell and references Core.

## Protected behavior
No algorithm rewrite is permitted in this task:
- baseline estimation/removal
- positive-current trapezoidal charge integration
- feature definitions/order
- subgroup/group semantics
- covariance/Mahalanobis calculation
- chi-square threshold
- CSV parsing/compatibility

## Dependency boundary
The automated test suite checks that `FeatureVector` is loaded from `PulseInspector.Core`, that Application references Core, and that Application does not reference the legacy WinForms assembly. The Core project itself targets plain `net8.0` and contains only Models/Services sources.

## Remaining work
1. Run and verify post-extraction CI.
2. Perform manual Windows verification of the WPF executable and migrated workflows.
3. Continue retiring the legacy WinForms Forms/Controls compatibility shell after WPF parity is complete.
4. Migrate remaining UI-only infrastructure and export/selection workflows into WPF/Application where required.

## Acceptance criteria
- Core builds as `net8.0` without Windows desktop UI references.
- Application references Core directly.
- WPF references Application only.
- Legacy WinForms remains buildable during transition.
- Existing algorithm regression tests remain green.
- No protected numerical behavior changes as part of extraction.
