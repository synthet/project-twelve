# /plan — Implementation plan

Use after a spec exists (or for small tasks, a verbal agreement). Prefer **plan mode** or explicit user approval before large edits. Keep the Spec Kit separation of concerns: the spec states what/why, while this plan records how the approved behavior will be built and verified.

## Inputs

- Approved spec or tight task description.
- Relevant files the user pointed at.

## Output (Implementation Plan)

1. **Goal** — What "done" means.
2. **Files / areas to touch** — Paths or components (include Unity `.meta` when assets change).
3. **Approach** — Steps in order; call out risky changes.
4. **Constitution check** — Confirm alignment with `AGENTS.md`, `.agent/SAFETY.md`, `docs/PAID_ASSETS.md`, security rules, package boundaries, and Unity/autotile conventions.
5. **Tests** — Failing test stubs to write *before* touching implementation, derived from spec
   acceptance criteria. List the test file paths and the assertion names.
6. **Task breakdown handoff** — Note whether `/tasks` is required before implementation; use it for multi-step or multi-agent work.
7. **Rollback / flags** — If feature-flagged or migratory.

## Done when

- Another developer could execute the plan without guessing.
- Failing test stubs are identified and ready to write before implementation begins.
- Test plan maps back to `AC-n` IDs and project conventions.
- Any skipped `/tasks` gate is explicitly justified as trivial/single-step work.

## Note

If the user has not approved implementation, **do not** apply code changes until they confirm.
