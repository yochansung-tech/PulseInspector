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

## Release 1.0 feature ordering

The complete display order is deterministic and alphabetic by feature name:

`Charge, FWHM, MahalanobisDistance, Noise, Peak, RiseTime, Threshold, ZScore`

The Mahalanobis calculation intentionally uses only the six statistical features. Mahalanobis Distance and Threshold are derived outputs and are not included in the covariance model.

The project is intentionally organized so the signal-processing and statistical logic can be reused independently of the UI.
