# MainForm Command Map — Phase 1

## Goal
Define the WPF command surface before broad screen migration.

| Legacy action | WPF command | Application boundary | Legacy source of truth |
|---|---|---|---|
| Add Group from CSV | `AddGroupCommand` | `LoadGroupFromCsv` | `CsvWaveformLoader` + `FeatureExtractor` + `GroupData` |
| Add Groups from CSV Rows | `AddGroupsFromCsvRowsCommand` | `LoadGroupsFromCsvRows` | `CsvRowWaveformLoader` + `FeatureExtractor` + `GroupData` |
| Clear Groups | `ClearGroupsCommand` | `ClearGroups` | MainForm collection/state semantics |
| Train Normal Groups | `TrainCommand` | `TrainNormalGroups` | `TrainingValidationService` + `GroupInspectionService` |
| Inspect Selected Group | `InspectSelectedGroupCommand` | `InspectGroup` | validation + `GroupInspectionService` + `SubgroupInspectionService` + `FeatureDeviationService` + `GroupDecisionService` |
| Group selection | `SelectedGroupChangedCommand` | `SelectGroup` | `GroupData.MeanWaveform/MeanFeatures` + presentation updates |
| Subgroup selection | `SelectedSubgroupChangedCommand` | `SelectSubgroup` | `InspectionSelectionService` |
| Defective toggle | `SetSelectedGroupDefectiveCommand` | `SetGroupDefective` | `GroupData.IsDefective` |
| Settings | `ApplySettingsCommand` | `ApplySettings` | `SettingsForm` + `GroupDecisionPolicy.Validate` + model invalidation |

## Boundary rules
- Commands coordinate use cases; they do not contain numerical algorithms.
- CSV parsing remains in the existing loaders.
- Feature extraction remains in `FeatureExtractor`.
- Training/inspection remains in existing services.
- Decision policy remains in `GroupDecisionService`.
- Subgroup row mapping/selection remains in `InspectionSelectionService`.
- ViewModels expose immutable/read-only presentation data where practical.

## Event-to-command migration
WinForms events are translated as follows:
- `SelectedIndexChanged` -> selection command/property change.
- `ColumnClick` -> explicit sort state/command in the ViewModel.
- `CheckedChanged` -> guarded ViewModel property update.
- menu `Click` -> `ICommand`.

## First implementation slice
The first production-facing WPF slice should support selection/presentation state and inspection orchestration through the application boundary, while the legacy waveform renderer remains hosted by `WindowsFormsHost`.
