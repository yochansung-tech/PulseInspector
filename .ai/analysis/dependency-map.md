# Dependency Map — Phase 0

## Scope
This document records the dependency boundaries relevant to WinForms → WPF modernization. It is an analysis artifact, not an implementation target.

## MainForm dependency surface
`MainForm` directly owns both UI controls and application services. The current source constructs these objects directly rather than receiving them through dependency injection.

### Direct UI dependencies
- `WaveformControl`
- `FeatureGrid`
- `FeatureDeviationGrid`
- `StatusIndicator`
- `ListBox`
- `ListView`
- `CheckBox`
- WinForms layout/menu/dialog types

### Direct service dependencies
- `FeatureExtractor`
- `CsvWaveformLoader`
- `CsvRowWaveformLoader`
- `GroupInspectionService`
- `SubgroupInspectionService`
- `FeatureDeviationService`
- `InspectionSelectionService`
- `GroupDecisionService`
- `TrainingValidationService`

The MainForm source confirms this direct composition pattern. fileciteturn20file0L1-L2

## Domain/data dependencies
The UI works with:
- `GroupData`
- `FeatureVector`
- `InspectionModel`
- `SubgroupInspectionResult`
- `FeatureDeviation`
- `GroupDecisionPolicy`

## Service dependency observations
- `FeatureExtractor` transforms waveform samples into `FeatureVector` instances.
- `GroupInspectionService` owns group-level training and inspection.
- `SubgroupInspectionService` owns subgroup inspection.
- `InspectionSelectionService` maps displayed subgroup rows back to source records/results.
- `GroupDecisionService` applies the configured group decision policy.
- `TrainingValidationService` validates training feature vectors before model training.

## Migration implication
WPF ViewModels should not duplicate these calculations. They should call application/service APIs through adapters or an application-facing facade.

## Risk boundary
The highest-risk dependency is the current `MainForm` because it combines presentation, object construction, event handling, workflow orchestration and user feedback. It should be migrated after the domain/service contracts are characterized, not by mechanically translating controls into XAML.
