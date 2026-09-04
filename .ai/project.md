# PulseInspector AI Modernization Project

## Baseline
- Name: PulseInspector
- Version: Release 1.0
- Language: C#
- Framework: .NET 8
- Current UI: WinForms
- Target UI: WPF
- Pattern: MVVM for new WPF UI
- Platform: Windows
- IDE: Visual Studio 2022+

## Modernization goals
1. Preserve all existing application functionality.
2. Preserve signal-processing and statistical results exactly unless a deliberate functional change is approved.
3. Modernize the desktop UI progressively rather than replacing the application wholesale.
4. Establish a reusable design system for colors, typography, spacing, controls, icons and states.
5. Make WPF UI testable and independent of the legacy WinForms presentation layer.

## Migration strategy
- Incremental migration.
- Keep the existing WinForms project buildable during migration.
- Introduce WPF only after Phase 0 analysis is complete.
- Use WindowsFormsHost when a legacy control must be reused temporarily.
- Migrate high-value screens/controls one at a time.
- Remove legacy controls only after functional and visual regression checks pass.

## Protected domain behavior
- Waveform/signal processing
- Feature extraction
- Feature definitions
- FeatureVector deterministic ordering
- Group and subgroup inspection
- Statistics and covariance calculations
- Mahalanobis distance
- Chi-square threshold behavior
- CSV formats and parsing semantics

## Phase gates
- Phase 0: analysis, rules, baseline and regression evidence. No production UI changes.
- Phase 1: WPF shell and design system.
- Phase 2: pilot screen/control migration.
- Phase 3: systematic screen migration.
- Phase 4: legacy cleanup and final validation.
