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
- Automatic baseline and noise estimation
- Peak, Charge, Rise Time, FWHM, Noise, Z-score, Mahalanobis Distance and Threshold features

The project is intentionally organized so the signal-processing and statistical logic can be reused independently of the UI.
