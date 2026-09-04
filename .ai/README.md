# AI Modernization Workspace

This directory is the shared memory and contract for the AI-agent modernization workflow.

## Directory map
- `project.md` — project baseline and modernization goals
- `migration-rules.md` — non-negotiable migration rules
- `agents/` — role-specific agent instructions
- `analysis/` — repository, UI, dependency, event and risk analysis
- `design/` — WPF design-system decisions
- `decisions/` — architecture decision records
- `tasks/` — executable modernization tasks

## Agent workflow
1. Project Manager defines a task and acceptance criteria.
2. Legacy Analyst inspects the existing implementation.
3. Architecture Agent identifies boundaries and migration strategy.
4. UI/UX Agent defines screen/component behavior and design tokens.
5. WPF Developer implements the approved scope.
6. Code Reviewer checks architectural and migration-rule compliance.
7. QA validates build, tests, behavior and visual regression.

Agents communicate primarily through repository artifacts. Git history provides the audit trail.

## Phase 0 rule
Phase 0 is analysis-only for production code. The only intended repository changes are AI workspace documentation and analysis artifacts.
