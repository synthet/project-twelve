---
type: Task
id: P4-CONTENT-001
title: "[P4-CONTENT-001] Specify crafting, progression, combat, and loot loops."
description: Data-driven crafting recipes, tool/combat properties, loot tables, and a testable dig→craft→fight→progress loop with placeholder balance.
status: open
phase: "Phase P4 — Feature complete and beta"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/44"
github_issue_status: created
resource: wiki/tickets/p4-content-001-specify-crafting-progression-combat-and-loot-loops.md
tags: [docs, wiki, ticket, gameplay, content, p4]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/gameplay-systems.md"
  - "docs/wiki/12-modding.md"
  - "docs/wiki/tickets/p2-inv-001-specify-inventory-backed-placement-and-pickup-rules.md"
  - "docs/wiki/tickets/p2-ai-001-specify-enemy-spawn-and-pathfinding-rules.md"
---

# [P4-CONTENT-001] Specify crafting, progression, combat, and loot loops.

## Open knowledge summary

This ticket specifies the core gameplay loop for feature-complete: **dig → collect → craft →
fight → progress deeper**. All content is registry data (P2-DATA-001 mechanism): crafting recipes
(inputs → output + station requirement), tool properties (strength, range, cooldown, tile damage
— replacing the hard-coded editing values, per `docs/wiki/gameplay-systems.md`), combat stats
(player/enemy health and damage), and loot tables (enemy drops with weighted rolls). Balance uses
placeholder numbers; the deliverable is that the loops are **testable and tunable from data**,
not that they are fun-final.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#44](https://github.com/synthet/project-twelve/issues/44)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a gameplay developer entering P4, I want crafting, combat, and loot specified as registry data
with a closed progression loop so that beta playtests (P4-QA-001) exercise a real game, and
tuning happens by editing definitions rather than code.

## Requirements

### Functional requirements

1. **Crafting:** recipe registry entries `(inputs: [(itemId, count)], output: (itemId, count),
   station: optional tileId)`; crafting validates and atomically exchanges inventory stacks
   (P2-INV-001 rules); handcraft vs station-adjacent recipes both supported.
2. **Tools:** tool-class item properties — mining strength (which tile hardness it can break),
   range, use cooldown, per-hit tile damage; tile defs gain `hardness` and required strength.
   The P2 editing path consumes these instead of constants.
3. **Combat:** melee-first spec — player attack (damage, arc/reach, cooldown, knockback), enemy
   contact damage, health/death for both sides; player death rule documented (respawn at spawn,
   configurable inventory-drop policy); invulnerability windows specified.
4. **Loot:** per-enemy loot tables with weighted entries and count ranges; rolls use seeded PRNG
   so fixtures are deterministic; drops spawn as P2-INV-001 pickup entities.
5. **Progression chain (placeholder content):** wood/stone tier → copper → iron → silver → gold
   (matching existing `SandboxTileIds` ores); each tier's tools unlock breaking the next tier's
   ore; at least one crafting station (e.g. `core:workbench`) gates tier 2+ recipes.
6. **Loop closure check:** from a fresh world, a player can reach the highest tier using only
   specified recipes and world resources — validated by a scripted scenario test, not hope.

### Non-functional requirements

1. No content in code: recipes, tools, stats, and loot are definitions; systems read registries.
2. All combat/loot randomness flows through seeded PRNG (deterministic fixtures).
3. Balance values live in one reviewable data set with a documented tuning workflow for
   P4-QA-001 feedback.

## Acceptance criteria

- Core gameplay loops are testable and balanced with placeholder content, driven entirely from
  registry data.
- EditMode: recipe crafting exchanges stacks atomically (insufficient inputs → no change);
  station gating enforced.
- EditMode: tool-vs-hardness matrix — each tier breaks its tier's tiles and fails on the next.
- EditMode: loot rolls with a fixed seed reproduce exactly; distribution over many rolls matches
  table weights within tolerance.
- EditMode scenario: scripted progression walkthrough reaches the top tier from a fresh golden
  world (loop closure).
- Play-mode checklist: kill an enemy → loot drops → craft at a station → new tool breaks
  previously unbreakable ore; player death/respawn behaves per spec.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, balance notes, and reviewer findings.

## Detailed technical specifications

### Scope

- Recipe/tool/combat/loot data contracts, crafting logic, melee combat, loot rolls, and the
  placeholder progression content set.
- Out of scope: ranged/magic weapons (firearm visuals exist in P2-VISUAL-002; behavior is a
  follow-up), bosses/events, armor/buff systems, fun-final balance (P4-QA-001 feeds tuning),
  crafting UI polish (P4-UX-001).

### Inputs and dependencies

- P2-DATA-001 registries; P2-INV-001 inventory/pickups; P2-AI-001 enemy framework and archetype.
- `docs/wiki/gameplay-systems.md` — tool-properties-not-hardcoded requirement.
- Monster visuals/loot carriers: P2-VISUAL-003 contract.

### Verification plan

- EditMode: crafting atomicity, tool matrix, loot determinism/distribution, progression scenario.
- Play-mode loop checklist with capture; balance review notes recorded for P4-QA-001 input.

## Documentation impact

- `docs/wiki/gameplay-systems.md` — crafting/combat/loot contracts and the progression chart.
- `docs/wiki/12-modding.md` — recipe/loot registries join the registry inventory.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Content contracts documented in `gameplay-systems.md` before implementation.
- [ ] Crafting, tool, loot, and progression-closure tests pass.
- [ ] Play-mode loop capture and balance notes attached.
- [ ] Follow-up tasks created for ranged combat, bosses, and balance tuning rounds.
