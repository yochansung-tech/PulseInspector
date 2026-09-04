# PulseInspector Modernization Architecture

## 1. Architectural objective

Move the presentation layer from WinForms to WPF without rewriting or destabilizing the existing waveform-processing, feature-extraction, statistical, CSV, and group-inspection domain behavior.

The target architecture separates presentation from reusable application/domain logic and allows a controlled hybrid period during migration.

## 2. Current architecture

```text
PulseInspector
├── Forms
│   ├── MainForm
│   ├── SettingsForm
│   ├── TrainingForm
│   └── AboutForm
├── Controls
│   ├── WaveformControl
│   ├── FeatureGrid
│   ├── FeatureDeviationGrid
│   ├── HistogramControl
│   ├── ScatterPlotControl
│   └── StatusIndicator
├── Models
└── Services
```

The current application is the behavioral baseline. UI modernization must not implicitly change the semantics of Models or Services.

## 3. Target architecture

```text
PulseInspector.sln
├── PulseInspector                 # existing WinForms application during migration
├── PulseInspector.Tests
└── PulseInspector.Wpf             # introduced in a later phase
    ├── Views
    ├── ViewModels
    ├── Controls
    ├── Resources
    ├── Themes
    └── Services / Adapters
```

A later refactoring may extract reusable non-UI code into a dedicated Core project if dependency analysis proves that this reduces coupling. Phase 0 does not perform that extraction.

## 4. Layer responsibilities

### Domain / application logic
Owns waveform processing, feature extraction, statistics, group inspection, models and data semantics.

### Adapter layer
Translates between legacy APIs/data contracts and WPF-facing interfaces. Prefer adapters over invasive changes to stable legacy services.

### ViewModel layer
Owns presentation state, commands, validation state and orchestration of UI interactions. It must not duplicate signal-processing or statistical algorithms.

### View layer
Owns WPF visual structure, bindings, templates, styles, accessibility metadata and visual states. Views must not contain business rules.

### Design system
Owns colors, typography, spacing, dimensions, control states, icons and reusable visual resources through centralized WPF resources.

## 5. Hybrid migration architecture

During migration both UI technologies may coexist:

```text
WPF Shell
   │
   ├── Native WPF View
   │      └── ViewModel
   │
   └── WindowsFormsHost
          └── Legacy WinForms UserControl
```

`WindowsFormsHost` is an intentional migration adapter, not the final target architecture. Each hosted control must have a documented migration decision and regression evidence.

## 6. Candidate migration map

Initial hypotheses to be validated during Phase 0 analysis:

| Current component | Target direction | Initial risk |
|---|---|---:|
| MainForm | WPF shell + MainViewModel | High |
| SettingsForm | Native WPF view | Medium |
| TrainingForm | Native WPF view | High |
| AboutForm | Native WPF view | Low |
| WaveformControl | Initially host, then evaluate native WPF rewrite | High |
| FeatureGrid | WPF DataGrid | Medium |
| FeatureDeviationGrid | WPF DataGrid | Medium |
| HistogramControl | WPF visualization | High |
| ScatterPlotControl | WPF visualization | High |
| StatusIndicator | Native WPF custom control | Low |

This table is a planning hypothesis. The final decision belongs in `.ai/analysis/control-map.md` after source-level dependency and event analysis.

## 7. Protected contracts

The following are architectural invariants for UI modernization:

- `FeatureVector` deterministic ordering
- six-feature statistical model definition
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
Application / Adapter interfaces
   ↓
Existing Services / Domain logic
   ↓
Models
```

The reverse direction is prohibited: domain/service code must not acquire dependencies on WPF controls, windows, resource dictionaries, or view-model types.

## 9. Testing architecture

Regression evidence is required at three levels:

1. Domain regression — existing unit tests plus golden waveform/group cases.
2. Integration regression — loading, training, inspection and export workflows.
3. Visual regression — screenshots/layout/DPI/focus/keyboard behavior for migrated screens.

A UI migration is complete only when the new presentation produces the same protected domain outputs for equivalent inputs.

## 10. Phase 0 architectural decision

Do not add `PulseInspector.Wpf` during Phase 0. First produce the project map, UI inventory, dependency map, service map, control map, event map and migration-risk assessment. Phase 1 may then introduce the WPF shell using the evidence produced by Phase 0.
