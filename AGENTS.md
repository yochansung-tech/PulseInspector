# PulseInspector — AI Agent Instructions

## Project
- Release 1.0
- .NET 8 / C# / WinForms
- Visual Studio 2022+
- WinForms UI is implemented without the Designer.
- Target modernization UI: WPF + MVVM.

## Modernization strategy
- Use incremental migration; do not rewrite the application in one step.
- Preserve existing behavior, analysis results, data formats, and feature definitions.
- WinForms controls may be hosted temporarily in WPF with WindowsFormsHost.
- Keep domain/signal/statistical logic independent from UI technology.

## Protected behavior
Do not change these during UI modernization unless a task explicitly requires it and regression tests prove equivalence:
- signal processing
- feature extraction and definitions
- deterministic FeatureVector ordering
- group/subgroup inspection logic
- statistical calculations
- Mahalanobis calculation
- chi-square threshold behavior
- CSV input/output formats

## AI workspace
Read `.ai/project.md` and `.ai/migration-rules.md` before making modernization changes.
Use `.ai/analysis/` for repository understanding and `.ai/design/` for UI design decisions.

## Change discipline
1. Analyze before editing.
2. Make small, reviewable changes.
3. Never commit directly to `main` for modernization work.
4. Prefer a dedicated `ai/*` branch and pull request.
5. Run build and tests before declaring a task complete.
6. Do not mix UI modernization with unrelated functional changes.
