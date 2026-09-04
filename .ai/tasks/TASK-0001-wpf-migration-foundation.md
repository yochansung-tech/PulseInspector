# TASK-0001 — WPF Migration Foundation

## Status
PLANNED

## Phase
Phase 1

## Objective
Establish the first application-facing boundary for WPF without changing existing WinForms behavior or protected inspection semantics.

## Scope
- Add the target WPF/application project structure required for incremental migration.
- Define the boundary between WPF ViewModels and existing services.
- Define the first MainForm migration seam.
- Select `WaveformControl` as the initial hybrid-hosting candidate.
- Establish build/test expectations for the mixed WinForms/WPF solution.

## Out of Scope
- Rewriting the full MainForm in WPF.
- Rewriting WaveformControl rendering.
- Rewriting FeatureGrid, FeatureDeviationGrid, HistogramControl, or ScatterPlotControl.
- Changing signal processing or statistical calculations.
- Changing CSV formats or inspection decisions.
- Changing FeatureVector ordering.

## Protected Behavior
- Feature extraction definitions and numerical results.
- Positive-current charge integration and baseline handling.
- Group and subgroup inspection semantics.
- Mahalanobis calculation and chi-square threshold behavior.
- Deterministic feature ordering.
- CSV compatibility.
- Existing user workflow and selection semantics.

## Inputs
- `.ai/project.md`
- `.ai/architecture.md`
- `.ai/migration-rules.md`
- `.ai/analysis/*`
- Existing WinForms source under `PulseInspector/`

## Dependencies
- Phase 0 analysis and agent operating rules.
- Existing solution must remain buildable before implementation begins.

## Owner Agent
Architecture Agent → WPF Developer Agent

## Allowed Files
- New WPF/application project files required by the approved architecture.
- New adapter/facade and ViewModel files explicitly approved by the implementation task.
- Tests explicitly added for the new boundary.
- Related `.ai/` artifacts.

## Forbidden Files
- Existing feature/statistics implementation unless a separate behavior-preserving refactor is explicitly approved.
- Existing CSV loader semantics.
- Existing group/subgroup inspection semantics.
- Existing WinForms controls during the foundation task except for documented integration points.

## Acceptance Criteria
- [ ] Existing WinForms project remains buildable.
- [ ] WPF target architecture is represented by actual project/boundary structure.
- [ ] WPF ViewModels do not directly duplicate domain calculations.
- [ ] Existing services remain the source of truth for inspection behavior.
- [ ] `WaveformControl` hosting boundary is documented.
- [ ] No protected numerical result changes are introduced.
- [ ] Feature ordering remains deterministic.
- [ ] A build/test command sequence is documented for local and CI execution.

## Regression Tests
- [ ] Existing test suite passes.
- [ ] Golden feature vectors remain unchanged for representative waveform inputs.
- [ ] Group/subgroup inspection results remain unchanged for representative datasets.
- [ ] CSV loading compatibility remains unchanged.

## Artifacts
- Architecture decision for application boundary.
- WPF project structure.
- Adapter/facade contract.
- Initial ViewModel contract.
- Build/test verification report.

## Branch
`ai/phase-1-foundation`

## Reviewer
Code Review Agent

## QA
QA Agent after implementation and review

## Gate
Human approval is required before production implementation begins. Phase 0 remains analysis/governance only.
