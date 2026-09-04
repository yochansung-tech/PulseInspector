# PulseInspector Modernization Architecture

## 1. Architectural objective

Move the presentation layer from WinForms to WPF without rewriting or destabilizing the existing waveform-processing, feature-extraction, statistical, CSV, and group-inspection domain behavior.

The migration target is now a native WPF application shell over UI-independent Core/Application layers. The former hybrid WindowsFormsHost path was a migration option only and is no longer part of the active branch architecture.

## 2. Current architecture

```text
PulseInspector.sln
├── PulseInspector.Wpf
│   ├── Views
│   ├── ViewModels
│   └── Themes
├── PulseInspector.Application
│   └── Contracts / application facade
├── PulseInspector.Core
│   ├── Models
│   └── Services
└── PulseInspector.Tests
```

The Core/Application layers own the protected behavioral baseline. WPF owns presentation and UI orchestration only.

## 3. Target architecture

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

## 4. Layer responsibilities

### Core
Owns waveform processing, feature extraction, statistics, group/subgroup inspection, models, CSV parsing/export logic and data semantics.

### Application
Owns composition/orchestration boundaries used by the UI. It delegates numerical and domain work to Core and exposes UI-safe use cases.

### ViewModel
Owns presentation state, commands, readiness/validation state and UI workflow orchestration. It must not duplicate signal-processing or statistical algorithms.

### View
Owns WPF visual structure, bindings, templates, styles, accessibility metadata and visual states. Code-behind is limited to UI infrastructure such as dialogs, window ownership and visual event wiring.

### Design system
Owns colors, typography, spacing, dimensions, control states and reusable visual resources through centralized WPF resources.

## 5. Hybrid migration decision

The original plan allowed `WindowsFormsHost` as a temporary adapter. The active branch no longer uses `WindowsFormsHost`; the migrated waveform, feature tables, histogram and scatter views are native WPF implementations.

A future return to hosted WinForms controls would require an explicit architecture decision and regression evidence.

## 6. Migrated UI map

| Former WinForms component | Active WPF target | Status |
|---|---|---|
| MainForm | MainWindow + MainWindowViewModel | Native WPF |
| SettingsForm | SettingsWindow | Native WPF |
| TrainingForm | TrainingWindow | Native WPF |
| AboutForm | AboutWindow | Native WPF |
| WaveformControl | WaveformView | Native WPF |
| FeatureGrid | WPF DataGrid | Native WPF |
| FeatureDeviationGrid | WPF DataGrid | Native WPF |
| HistogramControl | HistogramView | Native WPF |
| ScatterPlotControl | ScatterPlotView | Native WPF |
| StatusIndicator | MainWindow status presentation | Native WPF |

## 7. Protected contracts

The following are architectural invariants for UI modernization:

- deterministic complete feature ordering
- five independent statistical features used by the Mahalanobis model
- diagnostic Z-score semantics
- Mahalanobis calculation inputs
- threshold calculation semantics
- group/subgroup aggregation semantics
- waveform/sample interpretation
- baseline estimation behavior
- positive-current charge integration
- CSV parsing and export compatibility

UI agents must treat these as external contracts.

## 8. Dependency direction

The intended dependency direction is:

```text
WPF View
   ↓
ViewModel
   ↓
Application interfaces/facade
   ↓
Core services
   ↓
Core models
```

The reverse direction is prohibited: Core and Application must not acquire dependencies on WPF controls, windows, resource dictionaries, or view-model types.

## 9. Testing architecture

Regression evidence is required at three levels:

1. Domain regression — executable Core/Application regression suite and protected golden-data cases.
2. Integration regression — CSV loading, training, inspection, subgroup selection and export workflow.
3. Visual regression — screenshots/layout/DPI/focus/keyboard behavior for migrated screens in a Windows-capable environment.

The current CI workflow builds Core, Application and WPF on Windows and runs the executable algorithm regression suite.

## 10. Release 1.0 stabilization

The migration branch has completed the native WPF MainWindow workflow slice, command-state hardening, stable subgroup selection identity, native visualization views and legacy WinForms source retirement.

Remaining release gates are CI green on the current head and manual Windows verification of rendering, DPI, focus/keyboard behavior and representative import → training → inspection → export workflows.
