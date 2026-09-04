# AI Migration Task Lifecycle

This directory defines the contract used by the modernization agents.

## Lifecycle

```text
TASK CREATED
    ↓
ANALYSIS
    ↓
ARCHITECTURE APPROVAL
    ↓
DESIGN
    ↓
IMPLEMENTATION
    ↓
CODE REVIEW
    ↓
QA
    ↓
PR
    ↓
MERGE
```

A task may move backward when evidence exposes a missing requirement or regression. No agent may silently skip a gate.

## Task Template

```markdown
# TASK-XXXX — <title>

## Status
PLANNED | ANALYSIS | ARCHITECTURE | DESIGN | IMPLEMENTATION | REVIEW | QA | PR | DONE

## Phase
Phase N

## Objective
<one measurable objective>

## Scope
- <included item>

## Out of Scope
- <excluded item>

## Protected Behavior
- <domain/inspection behavior that must not change>

## Inputs
- <source/artifact>

## Dependencies
- <task or prerequisite>

## Owner Agent
<agent>

## Allowed Files
- <paths>

## Forbidden Files
- <paths>

## Acceptance Criteria
- [ ] <criterion>

## Regression Tests
- [ ] <test/evidence>

## Artifacts
- <design/spec/report>

## Branch
<feature branch>

## Reviewer
<agent/person>

## QA
<verification status>
```

## Gate Rules

### Analysis
Legacy behavior and dependencies are evidenced from the current repository.

### Architecture
Ownership, boundaries, adapter/facade contracts, and migration strategy are explicit.

### Design
Visual hierarchy, states, components, theme tokens, and accessibility expectations are explicit for UI work.

### Implementation
Only approved files/scope are changed. Protected domain behavior remains unchanged unless the task explicitly authorizes a separate behavior change.

### Review
The actual diff is checked for architecture, scope, regression, and maintainability violations.

### QA
Build/tests, golden-data comparisons, workflow checks, and relevant visual/interaction checks are completed.

### PR
The PR describes what changed, why, verification performed, known differences, and rollback considerations.

## Branch Policy

- `main` is the stable baseline.
- Phase planning/analysis may remain on `ai/phase-0`.
- Production implementation tasks should use dedicated branches created from the appropriate approved base.
- Merge only after review and QA gates pass.
