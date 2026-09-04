# PulseInspector WPF UI Design System

Status: Implemented in `ai/phase-1-mainform-slice`

## Design coverage

| Area | Status | Implementation |
|---|---|---|
| Application theme | Complete | Global WPF resource dictionaries |
| Color palette | Complete | Light/Dark semantic brushes |
| Typography | Complete | `Themes/Typography.xaml`, Segoe UI |
| Iconography | Complete | Windows `Segoe MDL2 Assets` glyphs, no external dependency |
| Button style | Complete | Shared sizing, hierarchy, primary action style |
| Navigation/action style | Complete | Main command bar with grouped workflow actions |
| DataGrid style | Complete | Shared headers, rows, grid lines, padding, alternating rows |
| Chart style | Complete | Shared chart brush tokens; histogram/scatter react to theme |
| Dialog style | Complete | Shared window typography, spacing, primary/cancel hierarchy |
| Status indicator | Complete | Model/result readiness badges plus status text |
| Dark/Light theme | Complete | Runtime switch from Settings |
| Screen layouts | Complete | Main, Settings, Training, About, Waveform, Histogram, Scatter |
| Image/icon assets | Complete | System glyph icon set selected to avoid packaging and licensing overhead |

## Semantic palette

Light and dark palettes expose the same semantic keys:
`AppBackgroundBrush`, `SurfaceBrush`, `SurfaceElevatedBrush`, `BorderBrush`,
`PrimaryBrush`, `PrimaryForegroundBrush`, `TextBrush`, `MutedTextBrush`,
`DefectBrush`, `NormalBrush`, `ChartBrush`, `ChartGridBrush`, `SelectionBrush`.

Views must consume semantic resources rather than hard-coded UI colors.

## Typography

- Font family: Segoe UI
- Application body: 14 px
- Window title: 24 px / SemiBold
- Section title: 15 px / SemiBold
- Caption: 12 px

## Iconography

Toolbar icons use `Segoe MDL2 Assets` glyphs for add, rows, training, train/inspect,
export, settings, about, and clear actions. This keeps the application self-contained
and avoids third-party icon packages.

## Interaction rules

- Primary workflow actions use the primary action style.
- Tooltips explain non-obvious actions and shortcuts.
- F5 invokes Inspect.
- DataGrid sorting preserves stable subgroup identity.
- Empty state provides the next action instead of showing a blank workspace.
- Model/result state is presented as explicit status badges.

## Theme rules

`ThemeManager.Apply()` replaces the active color dictionary at runtime. All controls
and charts consume `DynamicResource` semantic brushes so theme changes propagate
without recreating the main window.

## Asset policy

PulseInspector is an engineering inspection desktop application. It does not require
photographic or decorative image assets. Icons are treated as UI assets and are supplied
by the Windows system icon font to keep the application deterministic, lightweight,
and redistribution-safe.
