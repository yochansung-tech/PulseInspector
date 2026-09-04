# Service Map — Phase 0

## Feature extraction
### `FeatureExtractor`
Responsibility: convert a waveform and sampling interval into the Release 1.0 feature vector.

Observed contract:
- input: waveform samples + sample interval
- output: `FeatureVector`
- must remain independent of WPF presentation

The repository exposes `FeatureExtractor.Extract(IReadOnlyList<double>, double)`. fileciteturn18file0L2-L8

## Group inspection
### `GroupInspectionService`
Responsibility:
- train the group-level statistical model from normal groups
- inspect a complete group against the model

The repository explicitly assigns group-level training and inspection to this service. fileciteturn19file1L18-L24

### `SubgroupInspectionService`
Responsibility:
- inspect individual subgroup/waveform records using an inspection model

It is a separate service and must remain separate from WPF presentation logic. fileciteturn19file3L42-L54

## Decision and validation
### `GroupDecisionService`
Applies the configured group decision policy to subgroup inspection results.

### `TrainingValidationService`
Validates training feature vectors before model training.

### `FeatureDeviationService`
Calculates feature-level deviation information used by the UI.

### `InspectionSelectionService`
Maps selected UI subgroup rows back to source records and inspection/deviation data.

## CSV services
### `CsvWaveformLoader`
Loads one waveform per CSV file.

### `CsvRowWaveformLoader`
Loads multiple waveform/subgroup records from CSV rows.

CSV compatibility is a protected migration contract.

## Migration target
Phase 1 should introduce an application-facing facade or adapter layer where appropriate, so WPF ViewModels depend on stable use cases rather than directly constructing many concrete services.

Do not move signal-processing/statistical implementations into WPF.
