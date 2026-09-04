# TASK-0003 — Core Boundary Extraction

**Status:** IN PROGRESS
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
5. Confirmed the current Core assembly has no reference to Windows Forms by project configuration.
6. Confirmed Release build and algorithm regression suite pass in GitHub Actions.

## Transitional implementation
Core currently uses linked compilation of the existing `PulseInspector/Models` and `PulseInspector/Services` source files. This deliberately avoids duplicating protected numerical code while the legacy UI is retired.

## Protected behavior
No algorithm rewrite is permitted in this task:
- baseline estimation/removal
- positive-current trapezoidal charge integration
- feature definitions/order
- subgroup/group semantics
- covariance/Mahalanobis calculation
- chi-square threshold
- CSV parsing/compatibility

## Next work
1. Relocate Models/Services physically under `PulseInspector.Core`.
2. Remove linked compilation and delete the legacy source copies only after all references are migrated.
3. Add an automated dependency-boundary check.
4. Verify WPF/Application runtime path contains no WinForms assembly dependency.
5. Retire legacy Forms/Controls after WPF parity and manual Windows verification.

## Acceptance criteria
- Core builds as `net8.0` without Windows desktop UI references.
- Application references Core directly.
- WPF references Application only.
- Legacy WinForms remains buildable during transition.
- Existing algorithm regression tests remain green.
- No protected numerical behavior changes as part of extraction.
