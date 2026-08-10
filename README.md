# PulseInspector Release 1.0

PulseInspector is a .NET 8 WinForms application for pulse waveform inspection, feature extraction, statistical training, and group-level anomaly detection.

## Requirements

- .NET 8 SDK
- Windows
- Visual Studio 2022 or newer

## Design principles

- WinForms without the Designer
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

## CSV import modes

PulseInspector supports two CSV input modes.

### 1. One waveform per CSV file

Use **File → Add Group from CSV...** and select multiple CSV files that belong to one group. Each selected file is loaded as one waveform record.

A file may contain either:

- one numeric sample per line, using the configured measurement period, or
- two numeric columns containing `time,value`.

### 2. One subgroup per CSV row

Use **File → Add Group from CSV Rows...** when a single CSV contains multiple subgroup measurements, with one complete subgroup on each line.

For example, a 64-sample CSV can be arranged as:

```text
s1,s2,s3,...,s64
s1,s2,s3,...,s64
s1,s2,s3,...,s64
...
```

Each non-empty numeric row becomes one `WaveformRecord` / subgroup inside one `GroupData`. The next line is treated as the next subgroup. All rows in the file must contain the same number of samples. The default measurement period is 2.56 µs unless changed in the import options.

The row-based loader intentionally does not run automatic pulse segmentation. Each row is already considered a complete subgroup. This allows recorded subgroup boundaries to be preserved exactly as provided by the measurement system.

## Group-level inspection

A measurement group represents multiple waveforms that belong to the same physical or logical inspection unit. Each waveform is converted to a FeatureVector. The group feature vector is the arithmetic mean of the statistical features across the waveforms in that group.

Training is performed from normal groups, not individual waveforms. The covariance model therefore represents variation between inspection groups. During inspection, the complete group is classified once using its aggregated feature vector.

`GroupData` enforces equal waveform length within a group, keeps waveform and feature data together, and provides mean-waveform and mean-feature aggregation. `GroupInspectionService` owns the group-level training and inspection flow.

## Release 1.0 feature ordering

The complete display order is deterministic and alphabetic by feature name:

`Charge, FWHM, MahalanobisDistance, Noise, Peak, RiseTime, Threshold, ZScore`

The Mahalanobis calculation intentionally uses only the six statistical features. Mahalanobis Distance and Threshold are derived outputs and are not included in the covariance model.

The project is intentionally organized so the signal-processing and statistical logic can be reused independently of the UI.
