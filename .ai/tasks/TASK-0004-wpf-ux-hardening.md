# TASK-0004 — WPF Main Window UX Hardening

## Status

IN PROGRESS — command-state and empty-state slice implemented.

## Objective

Complete the MainWindow workflow so that the UI always reflects whether training, inspection, and export are currently valid operations, while keeping domain and numerical logic outside WPF.

## Implemented in this slice

- `RelayCommand` exposes explicit `RaiseCanExecuteChanged()` support.
- Train is enabled only when the required number of normal groups is available.
- Inspect is enabled only when a group is selected and enough other normal groups remain for training.
- Export is enabled only while a current inspection result exists.
- Changing the selected group invalidates the current model/result state.
- Changing a group's defective/normal classification invalidates the model and inspection result.
- Applying settings invalidates the model and inspection result.
- Clear removes groups, model state, inspection state, and waveform state together.
- Training readiness is reported in the status area.
- Model/result state is exposed for UI presentation.
- Main window has an explicit empty-state message when no groups are loaded.
- Status area reports model/result state without moving business logic into the view.

## Invariants

1. WPF remains a presentation/application-shell layer.
2. Numerical algorithms remain in Core/Application services.
3. `FeatureVector` ordering and inspection semantics are not changed by UX work.
4. Export uses the last completed inspection result; it does not retrain or re-inspect.
5. A model is never presented as valid after its training inputs/settings have been invalidated.

## Follow-up

- Add focused automated tests for command-state transitions without requiring a rendered WPF window.
- Review subgroup selection against sorted DataGrid views so selection remains tied to the underlying record identity rather than display index.
- Add visual regression checks for empty/loading/error states.
- After these are complete, move the migration effort from Phase 1 slice work to WPF Release 1.0 stabilization.
