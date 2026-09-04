# Control Map — Phase 0

## Existing custom controls
| WinForms control | Initial migration strategy | Reason |
|---|---|---|
| `WaveformControl` | Hybrid first; WPF rewrite later | Nontrivial visualization; useful pilot for hosted-control strategy |
| `FeatureGrid` | WPF `DataGrid` | Primarily structured feature presentation |
| `FeatureDeviationGrid` | WPF `DataGrid` | Structured tabular deviation presentation |
| `HistogramControl` | WPF chart implementation | Visualization should eventually use WPF-native rendering |
| `ScatterPlotControl` | WPF chart implementation | Visualization should eventually use WPF-native rendering |
| `StatusIndicator` | WPF custom control | Small presentation-only component |

## MainForm composition
`MainForm` directly instantiates `WaveformControl`, `FeatureGrid`, `FeatureDeviationGrid` and `StatusIndicator`, alongside standard WinForms controls. fileciteturn20file0L1-L2

## Pilot recommendation
`WaveformControl` is the preferred first hybrid-host candidate. It allows the WPF shell to be validated without rewriting the waveform rendering algorithm at the same time.

## Rules
- Do not rewrite a visualization control solely because WPF is the target.
- Preserve rendering semantics during the first migration.
- Record every hosted WinForms control in this map.
- Replace a hosted control only after WPF rendering is visually and functionally equivalent.
