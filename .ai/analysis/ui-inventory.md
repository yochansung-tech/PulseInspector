# UI Inventory — Phase 0 Initial Baseline

> This is an initial inventory from the repository structure. Event and dependency analysis must refine it before migration work begins.

| Legacy UI | Initial WPF target | Migration approach | Priority |
|---|---|---|---|
| MainForm | MainWindow / shell | WPF rewrite; host legacy controls initially | P0 |
| SettingsForm | SettingsView | WPF rewrite | P1 |
| TrainingForm | TrainingView | WPF rewrite | P1 |
| AboutForm | AboutView | WPF rewrite | P2 |
| WaveformControl | WaveformView/Control | Initially WindowsFormsHost, then evaluate rewrite | P0 |
| FeatureGrid | Feature DataGrid | WPF DataGrid | P0 |
| FeatureDeviationGrid | Deviation DataGrid | WPF DataGrid | P1 |
| HistogramControl | Histogram view/control | WPF implementation after chart decision | P1 |
| ScatterPlotControl | Scatter view/control | WPF implementation after chart decision | P1 |
| StatusIndicator | StatusIndicator control | WPF custom control | P0 |

## Pilot candidate
MainForm + WaveformControl is the preferred pilot because it exercises the shell, navigation/layout, visualization, status, grid integration and WinForms/WPF interoperability.

## Rules
- Do not infer behavior from class names alone.
- Before migration, map event handlers, service calls, state ownership, persistence and error paths.
- Record every temporary WindowsFormsHost dependency.
