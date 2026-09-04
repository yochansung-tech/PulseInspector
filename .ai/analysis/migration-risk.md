# Migration Risk Register — Phase 0

| Area | Risk | Level | Mitigation |
|---|---|---:|---|
| `MainForm` | UI, workflow orchestration and service construction are coupled | High | Extract application-facing adapters/use cases before large-scale screen migration |
| `WaveformControl` | Visualization rewrite may change rendering behavior | High | Host existing control first; compare screenshots and interaction behavior |
| Feature calculations | UI refactor could accidentally alter feature extraction | Critical | Treat feature extraction as protected; use golden-data regression tests |
| Group inspection | Training/inspection aggregation semantics could drift | Critical | Regression-test group and subgroup results before/after migration |
| Feature ordering | Binding or dictionary enumeration could change displayed order | High | Preserve explicit deterministic feature ordering contract |
| CSV | Import/export compatibility could be broken | High | Keep loaders and formats unchanged during UI migration |
| Settings | Confidence/sample interval/decision policy are form-owned state | Medium | Move to ViewModel/application settings without changing semantics |
| WinForms/WPF interop | Threading, handle lifetime and DPI issues | Medium | Isolate `WindowsFormsHost`; verify startup/shutdown/resize/DPI |
| Charts | WPF chart implementation may not reproduce WinForms rendering exactly | Medium | Pilot one visualization and establish visual acceptance criteria |
| Tests | Existing test coverage may not cover every UI workflow | Medium | Expand regression suite before destructive legacy cleanup |

## Phase 0 conclusion
The primary risk is not converting WinForms controls to XAML. The primary risk is accidentally changing behavior because `MainForm` currently coordinates UI state, services and workflows in one class.

Therefore Phase 1 should establish a WPF shell and an application-facing boundary before migrating the complete screen.
