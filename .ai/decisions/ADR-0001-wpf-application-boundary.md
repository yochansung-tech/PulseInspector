# ADR-0001 — WPF Application Boundary

## Status
Accepted for Phase 1 implementation

## Context
`MainForm` currently owns UI construction, mutable workflow state, event handlers, and direct construction/use of multiple services. A direct WinForms-to-WPF rewrite would reproduce this coupling inside WPF and make regression isolation difficult.

The migration must preserve existing signal-processing, feature extraction, inspection, training, CSV, and decision semantics.

## Decision
Introduce an application-facing boundary between WPF ViewModels and the existing service/model layer.

```text
WPF View
   ↓
ViewModel
   ↓
Application Facade / Use Case Boundary
   ↓
Existing Services
   ↓
Models
```

The first implementation will expose only the use cases required by the migrated shell. It will not become a second business-logic layer.

### Responsibilities

**View**
- Rendering
- Binding
- User interaction
- Visual state

**ViewModel**
- Presentation state
- Commands
- Selection state
- Mapping application results to bindable properties

**Application boundary**
- Coordinates existing services
- Owns application use-case orchestration
- Converts UI-facing requests into existing service calls
- Returns application-facing result objects without duplicating calculations

**Existing Services / Models**
- Remain authoritative for domain and inspection behavior

## Initial Use Cases
The foundation boundary should be designed around these workflows:

1. Load waveform/group data
2. Select group/subgroup
3. Extract/display features through existing services
4. Train/validate inspection model
5. Inspect group/subgroup
6. Apply group decision policy
7. Load/save CSV where required by the existing workflow

Only the subset needed by the first WPF screen should be implemented initially.

## WaveformControl Strategy
`WaveformControl` remains a WinForms control during the first migration step and is hosted through `WindowsFormsHost`. A later task may replace it with a native WPF rendering implementation after numerical and visual equivalence criteria are defined.

## Consequences
### Positive
- Existing domain behavior remains isolated and reusable.
- WPF migration can proceed screen-by-screen.
- MainForm coupling is not copied into WPF.
- Hybrid migration is reversible.

### Negative
- Temporary adapter/facade code is required.
- Some state mapping may exist while WinForms and WPF coexist.
- Native WPF control migration is deferred.

## Rejected Alternatives
### Direct MainForm rewrite
Rejected because it moves existing orchestration/coupling into WPF instead of establishing a clean boundary.

### Reimplement domain logic in ViewModels
Rejected because it risks numerical and semantic regressions.

### Rewrite all controls before creating the shell
Rejected because it creates a large, difficult-to-verify change set.

## Verification Requirement
Every application boundary method must be traceable to existing service behavior. Protected numerical outputs and inspection decisions must be covered by existing or newly established regression tests before legacy implementation is retired.
