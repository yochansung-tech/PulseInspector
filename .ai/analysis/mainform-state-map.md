# MainForm State Map — Phase 1

## Purpose
Extract the state ownership that must move behind the WPF ViewModel/application boundary without changing existing behavior.

## Mutable application state
| Current MainForm state | Meaning | WPF target |
|---|---|---|
| `_groups` | Loaded inspection groups | `ObservableCollection<GroupSummaryViewModel>` |
| `_subgroupResults` | Results for selected group | `ObservableCollection<SubgroupResultViewModel>` |
| `_model` | Current trained inspection model | Application state, exposed as status/inspection state |
| `_decisionPolicy` | Group defect decision rule | Settings/application state |
| `_confidence` | Training confidence | Settings/application state |
| `_sampleIntervalSeconds` | Import/default sampling interval | Settings/application state |
| `_updatingDefective` | WinForms event recursion guard | ViewModel property update semantics |
| `_subgroupSortColumn` | Current subgroup sort column | ViewModel sort state |
| `_subgroupSortOrder` | Current subgroup sort direction | ViewModel sort state |

## UI state currently derived in MainForm
- selected group index
- selected subgroup index
- waveform shown for selected group/subgroup
- feature vector shown for selected group/subgroup
- feature deviation list
- subgroup result rows
- defective checkbox state
- status text/state

## Ownership target
```text
WPF View
  -> MainWindowViewModel
       -> InspectionApplication facade
            -> existing services/models
```

The ViewModel owns presentation state and command availability. It must not calculate features, Mahalanobis distance, charge, FWHM, noise, peak, rise time, Z-score, covariance, thresholds, or inspection decisions.

## Workflow state transitions
1. Load group -> append group -> select loaded group -> refresh waveform/features.
2. Select group -> show group mean waveform/features -> clear subgroup results/deviations.
3. Toggle defective -> update selected `GroupData.IsDefective` while preserving current behavior.
4. Train -> validate normal groups -> train existing model -> expose training status.
5. Inspect -> validate training groups -> train existing model -> inspect selected group -> populate feature/deviation/subgroup state -> apply existing decision policy.
6. Select subgroup -> use existing selection service -> display record waveform/features/deviations/result.
7. Settings -> validate/apply policy/confidence/sample interval -> invalidate model and subgroup state exactly as legacy behavior.
8. Clear -> remove groups and dependent presentation/model state.

## Migration rule
No MainForm event handler should be copied into WPF code-behind. Each workflow should become a ViewModel command backed by an application-facing use case.
