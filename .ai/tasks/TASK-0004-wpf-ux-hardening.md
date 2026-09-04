# TASK-0004 — WPF Main Window UX Hardening

## Status

COMPLETED — command-state, empty-state, workflow invalidation, and identity-safe subgroup selection implemented.

## Objective

Complete the MainWindow workflow so that the UI always reflects whether training, inspection, and export are currently valid operations, while keeping domain and numerical logic outside WPF.

## Implemented

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
- Subgroup result rows now retain the underlying `WaveformRecord.Id` as stable identity.
- Selecting a subgroup resolves the waveform by `RecordId`, so DataGrid sorting cannot redirect selection to another record.
- Display `Index` remains a presentation/sort field and is no longer used as the underlying record lookup key.

## Invariants

1. WPF remains a presentation/application-shell layer.
2. Numerical algorithms remain in Core/Application services.
3. `FeatureVector` ordering and inspection semantics are not changed by UX work.
4. Export uses the last completed inspection result; it does not retrain or re-inspect.
5. A model is never presented as valid after its training inputs/settings have been invalidated.
6. DataGrid sorting must not change the waveform associated with an already selected subgroup row.

## Verification

- Source-level inspection confirms subgroup selection is resolved by stable `WaveformRecord.Id`.
- Existing Core/Application regression tests remain unchanged for protected numerical behavior.
- GitHub Actions has not yet produced a workflow run/status for the latest branch head; CI remains pending.
- Manual Windows verification is still required for WPF rendering, sorting/selection interaction, DPI, keyboard/focus, CSV import/export, and visual states.

## Next stabilization scope

- Add focused automated tests for WPF command-state transitions if the test project is intentionally extended to reference WPF.
- Add visual regression checks for empty/loading/error states in a Windows-capable UI test environment.
- Perform manual Windows smoke verification.
- Once CI and manual verification are complete, prepare the WPF Release 1.0 stabilization PR for final review/merge.
