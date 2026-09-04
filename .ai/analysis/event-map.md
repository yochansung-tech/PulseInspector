# Event and State Map — Phase 0

## MainForm event surface
Observed UI events include:
- group selection change
- subgroup selection change
- subgroup column click/sort
- defective checkbox change
- File menu commands
- Training commands
- Settings command
- About command

The current MainForm attaches these handlers directly in its constructor. fileciteturn20file0L1-L2

## Workflow state owned by MainForm
The current form owns mutable application/workflow state including:
- loaded `GroupData` collection
- current `InspectionModel`
- subgroup inspection results
- `GroupDecisionPolicy`
- confidence level
- sample interval
- subgroup sorting state
- UI update guard state

This makes MainForm the primary orchestration/state hub.

## WPF migration rule
This state should move to ViewModels/application state objects incrementally. The WPF View should primarily bind to state and issue commands.

### Target pattern
```text
View event / Command
        ↓
ViewModel command
        ↓
Application use case / adapter
        ↓
Existing service
        ↓
Model/result
        ↓
Observable ViewModel state
        ↓
WPF View
```

## Important constraint
Migration must preserve workflow semantics. Event-to-command conversion is an architectural refactor, not permission to change when training, inspection, selection, or validation occurs.
