# PulseInspector Release 1.0

PulseInspector is a .NET 8 desktop application for pulse waveform inspection, feature extraction, statistical training, and group-level anomaly detection.

The repository now uses a native WPF/MVVM application shell with reusable Core/Application layers. The migration is being completed without changing the protected signal-processing and statistical behavior.

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
- Native WPF waveform rendering
- Feature/deviation tables
- Subgroup Mahalanobis histogram and scatter views
- Deterministic subgroup sorting and stable record selection
- Centralized theme resources
- Keyboard/focus and accessibility metadata

The legacy WinForms application shell has been retired from the active solution on the migration branch. The current branch is therefore focused on WPF Release 1.0 stabilization rather than a hybrid WindowsFormsHost migration.

## Design principles

- Native WPF/MVVM for the application UI
- Pure C# source
- Deterministic feature ordering
- Group-based inspection
- Baseline estimation that does not depend on the pulse being at the beginning of the record
- Positive-current trapezoidal charge integration
- Five independent statistical features for the Mahalanobis model:
  - Charge
  - FWHM
  - Noise
  - Peak
  - Rise Time
- Diagnostic feature:
  - Z-score
- Derived inspection outputs:
  - Mahalanobis Distance
  - Threshold
- Mahalanobis covariance is calculated from the five independent statistical features. Z-score is derived from Peak and is retained for diagnostics/export; it is not an independent covariance dimension.

## Architecture

```text
WPF View
  ↓
ViewModel
  ↓
Application Facade / Use Case Boundary
  ↓
Core Services
  ↓
Core Models
```

The WPF presentation layer does not duplicate feature extraction, baseline correction, charge integration, covariance, Mahalanobis, or threshold calculations. Core/Application services remain the source of truth.

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

## Release 1.0 feature ordering

The complete display/export order is deterministic and alphabetic by feature name:

`Charge, FWHM, MahalanobisDistance, Noise, Peak, RiseTime, Threshold, ZScore`

The five independent statistical features used by the Mahalanobis model follow:

`Charge, FWHM, Noise, Peak, RiseTime`

Z-score is a diagnostic derived from Peak and is intentionally excluded from the covariance vector.

## Verification

The executable regression suite covers feature ordering, group aggregation, CSV loading, feature extraction, training validation, Mahalanobis inspection, synthetic defect detection, feature deviations, CSV export, subgroup selection, and the Application facade.

The GitHub Actions workflow builds the complete solution on Windows and runs the algorithm regression suite. A successful current-head run is required before declaring the migration release-ready. Manual Windows verification is still required for WPF rendering, DPI, keyboard/focus, CSV import/export, and visual states.
