# AI Migration Rules

## Mandatory rules
1. Never change domain or analysis behavior as part of a UI-only task.
2. Never introduce business logic into WPF Views or code-behind.
3. New WPF screens use MVVM and dependency injection where practical.
4. Theme values must come from centralized resources/design tokens.
5. Icons must use the centralized icon strategy; do not scatter arbitrary icon assets.
6. Preserve accessibility, keyboard behavior, validation and error semantics.
7. Preserve CSV compatibility.
8. Preserve deterministic feature ordering.
9. Prefer adapters over invasive changes to legacy code.
10. Keep each migration change small enough to review and revert.

## Legacy reuse
- A WinForms UserControl may remain in service through WindowsFormsHost.
- Reuse is preferred when the control contains nontrivial visualization logic and rewriting it adds regression risk.
- A hosted control must have an explicit migration record in `.ai/analysis/control-map.md`.

## Verification
For every migrated screen:
- `dotnet build`
- `dotnet test`
- functional comparison with the legacy screen
- resize/DPI verification
- keyboard/focus verification
- visual review

For domain-sensitive changes, compare golden inputs and expected outputs before and after the change.
