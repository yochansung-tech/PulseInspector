# PulseInspector Release 1.0

PulseInspector is a .NET 8 desktop application for pulse waveform inspection, feature extraction, statistical training, and group-level anomaly detection.

The repository currently contains the original Designer-free WinForms application plus an incremental native WPF/MVVM migration. The WPF migration is being completed without changing the protected signal-processing and statistical behavior.

## Requirements

- .NET 8 SDK
- Windows
- Visual Studio 2022 or newer

## WPF migration status

`PulseInspector.Wpf` provides the migrated inspection workspace, including:

- Native WPF/MVVM application shell
- Application workflow facade
- Group and row-based CSV import
- Group/subgroup selection and inspection
- Training validation and settings
- Native waveform rendering
- Feature/deviation tables
- Subgroup Mahalanobis histogram and scatter views
- Deterministic subgroup sorting
- Centralized theme resources
- Primary keyboard/accessibility metadata

The legacy `PulseInspector` WinForms project remains in the solution until the final Core extraction and dependency cleanup are complete. Therefore this branch is a migration stage, not yet the final WinForms-free Release 1.0 packaging.

## Design principles

- WinForms legacy UI without the Designer
- Native WPF/MVVM for the migration target
- Pure C# source
- Deterministic feature ordering
- Group-based inspection
- Baseline estimation that does not depend on the pulse being at the beginning of the record
- Positive-current trapezoidal charge integration
- Six statistical features for the Mahalanobis model:
  - Charge
  - FWHM
  - Noise
  - Peak
  - Rise Time
  - Z-score
- Derived inspection outputs:
  - Mahalanobis Distance
  - Threshold
- Chi-square threshold at configurable confidence; Release 1.0 defaults to 99.9% with df=6 and threshold 22.457744

## Architecture

The migration target is:

```text
WPF View
  ↓
ViewModel
  ↓
Application Facade / Use Case Boundary
  ↓
Existing Services
  ↓
Models
```

The WPF presentation layer does not duplicate feature extraction, baseline correction, charge integration, covariance, Mahalanobis, or threshold calculations. Existing services remain the source of truth during the incremental migration.

## CSV import modes

PulseInspector supports two CSV input modes.

### 1. One waveform per CSV file

Use the WPF **Add Group...** action and select multiple CSV files that belong to one group. Each selected file is loaded as one waveform record.

A file may contain either:

- one numeric sample per line, using the configured sample interval, or
- two numeric columns containing `time,value`.

### 2. One subgroup per CSV row

Use **Add Groups by Rows...** when a CSV contains multiple subgroup measurements, with one complete subgroup on each line.

For example:

```text
s1,s2,s3,...,s64
s1,s2,s3,...,s64
s1,s2,s3,...,s64
...
```

Each numeric row becomes one `WaveformRecord` / subgroup. All rows in the file must contain the same number of samples. The default measurement period is 2.56 µs unless changed in the import options.

The row-based loader does not perform automatic pulse segmentation. Each row is considered a complete subgroup so recorded subgroup boundaries are preserved exactly.

## Group-level inspection

A measurement group represents multiple waveforms belonging to the same physical or logical inspection unit. Each waveform is converted to a `FeatureVector`. The group feature vector is the arithmetic mean of the statistical features across the waveforms in that group.

Training is performed from normal groups, not individual waveforms. The covariance model therefore represents variation between inspection groups. During inspection, the complete group is classified once using its aggregated feature vector.

`GroupData` enforces equal waveform length within a group, keeps waveform and feature data together, and provides mean-waveform and mean-feature aggregation. `GroupInspectionService` owns the group-level training and inspection flow.

## Release 1.0 feature ordering

The complete display order is deterministic and alphabetic by feature name:

`Charge, FWHM, MahalanobisDistance, Noise, Peak, RiseTime, Threshold, ZScore`

The Mahalanobis calculation intentionally uses only the six statistical features. Mahalanobis Distance and Threshold are derived outputs and are not included in the covariance model.

## Verification

The existing executable regression suite covers feature ordering, group aggregation, CSV loading, feature extraction, training validation, Mahalanobis inspection, synthetic defect detection, feature deviations, and the legacy WinForms smoke test. The WPF/application boundary is additionally exercised by an application-facade regression test.

A successful GitHub Actions run on the current migration head is still required before declaring the migration release-ready. Local build verification may be unavailable in environments without network access to restore the .NET SDK dependencies.
